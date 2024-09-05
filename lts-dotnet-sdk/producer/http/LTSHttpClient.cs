/*
 Copyright 2024 Huawei Technologies Co.,Ltd.

 Licensed to the Apache Software Foundation (ASF) under one
 or more contributor license agreements.  See the NOTICE file
 distributed with this work for additional information
 regarding copyright ownership.  The ASF licenses this file
 to you under the Apache LICENSE, Version 2.0 (the
 "LICENSE"); you may not use this file except in compliance
 with the LICENSE.  You may obtain a copy of the LICENSE at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing,
 software distributed under the LICENSE is distributed on an
 "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 KIND, either express or implied.  See the LICENSE for the
 specific language governing permissions and limitations
 under the LICENSE.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using HuaweiCloud.SDK.LTS.Model;
using Newtonsoft.Json;
using Polly;
using RestSharp;
using Microsoft.Extensions.Logging;

namespace HuaweiCloud.SDK.LTS.HTTP
{
    public class LTSHttpClient : IHttpClient
    {
        private static readonly ILogger logger;
        
        static LTSHttpClient()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            logger = loggerFactory.CreateLogger<LTSHttpClient>();
        }

        
        public string Endpoint { get; set; } // IP or hostname of LTS endpoint
        public string AccessKeyID { get; set; }
        public string AccessKeySecret { get; set; }
        public long RequestTimeOut { get; set; }
        public long RetryTimeOut { get; set; }
        public RestClient RestHTTPClient { get; set; }
        public string Region { get; set; }
        public string ProjectId { get; set; }
        public ReaderWriterLockSlim accessKeyLock { get; set; }
        
        public Config Config { get; set; }

        public static IHttpClient CreateIHttpClient(Config config)
        {
            LTSHttpClient client = new LTSHttpClient
            {
                Endpoint = config.Endpoint, AccessKeyID = config.AccessKeyID, AccessKeySecret = config.AccessKeySecret,
                Region = config.RegionId, ProjectId = config.ProjectId, accessKeyLock = new ReaderWriterLockSlim(),
                RestHTTPClient = Common.DefaultHttpClient(config),
                Config = config
            };
            return client;
        }

        private LTS_Store ConvertLTSstore(LTSHttpClient client, string groupId, string streamId)
        {
            client.accessKeyLock.EnterReadLock();
            LogProject proj;
            try
            {
                proj = ConvertLocked(client);
            }
            finally
            {
                client.accessKeyLock.ExitReadLock();
            }

            return new LTS_Store
            {
                LogProject = proj, PutLogCompressType = CompressType.CompressNone, GroupId = groupId,
                StreamId = streamId, LtsHttpClient = client
            };
        }

        private LogProject ConvertLocked(LTSHttpClient client)
        {
            var logProject = NewLogProject(client);
            logProject.Region = client.Region;
            if (client.RestHTTPClient != null)
            {
                logProject.RestHttpClient = client.RestHTTPClient;
            }

            return logProject;
        }

        private LogProject NewLogProject(LTSHttpClient client)
        {
            var logProject = new LogProject
            {
                Endpoint = client.Endpoint, AccessKeyID = client.AccessKeyID, AccessKeySecret = client.AccessKeySecret,
                RestHttpClient = client.RestHTTPClient, RetryTimeout = Constant.DefaultRetryTimeout,
                Region = client.Region,
                ProjectId = client.ProjectId
            };
            logProject.ParseEndpoint();
            return logProject;
        }

        public Error putLogs(string groupId, string streamId, LogGroup logGroup)
        {
            var ltsStore = ConvertLTSstore(this, groupId, streamId);
            return ltsStore.putLogs(logGroup);
        }

        public HttpResult HttpGet(string url, string path, Dictionary<string, string> header, string region)
        {
            return Execute(new LTSRequest
            {
                Endpoint = url,
                Path = path,
                Method = Method.Get,
                Header = header,
                Region = region
            });
        }

        public HttpResult HttpPost(string url, string path, Dictionary<string, string> header, string body, string region)
        {
            return Execute(new LTSRequest
            {
                Endpoint = url,
                Path = path,
                Method = Method.Post,
                Header = header,
                Body = body,
                Region = region
            });
        }

        private HttpResult Execute(LTSRequest ltsRequest)
        {
            try
            {
                if (string.IsNullOrEmpty(ltsRequest.Endpoint))
                {
                    return new HttpResult
                        { HTTPCode = HttpStatusCode.BadRequest, Success = false, RespBody = "Url is Empty" };
                }

                var url = new Uri(ltsRequest.Endpoint);
                var signer = new Signer() { AppKey = AccessKeyID, AppSecret = AccessKeySecret };
                var request = signer.Sign(GetHttpRequest(ltsRequest));
                if (ltsRequest.Body != null)
                {
                    request.AddStringBody(ltsRequest.Body, ContentType.Json);
                }
                
                var response = RestResponseWithPolicy(request, this.Config);
                
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new HttpResult { HTTPCode = HttpStatusCode.OK, Success = true, RespBody = response.Content };
                }

                if (response.ErrorException != null)
                {
                    return new HttpResult
                        { HTTPCode = response.StatusCode, Success = false, RespBody = response.ErrorException.Message };
                }

                return new HttpResult
                    { HTTPCode = response.StatusCode, Success = false, RespBody = response.ErrorMessage };
            }
            catch (Exception ex)
            {
                return new HttpResult
                    { HTTPCode = HttpStatusCode.InternalServerError, Success = false, RespBody = ex.Message };
            }
        }

        private TimeSpan ExponentialBackoffWithJitter(int retryCount, Config config)
        {
            Random jitterer = new Random();
            var fromMilliseconds = TimeSpan.FromSeconds(Math.Pow(2, retryCount))
                                   + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000));
            if (fromMilliseconds.Milliseconds > config.MaxRetryBackoffMs)
            {
                return TimeSpan.FromMilliseconds(config.MaxRetryBackoffMs);
            }

            return fromMilliseconds;
        }

        private RestResponse RestResponseWithPolicy(RestRequest restRequest, Config config)
        {
            var retryPolicy = Policy
                .HandleResult<RestResponse>(x => !x.IsSuccessful)
                .WaitAndRetry(config.Retries, retryCount => ExponentialBackoffWithJitter(retryCount, config), (restResponse, timeSpan, retryCount, context) =>
                    {
                        logger.LogError($"[retry]The request failed. HttpStatusCode= {restResponse.Result.StatusCode}, Waiting {timeSpan.TotalSeconds} seconds before retry. Number attempt {retryCount}, RequestResponse={restResponse.Result.Content}, ErrorMessage={restResponse.Result.ErrorMessage}");
                        Console.WriteLine($"[retry-console]The request failed. HttpStatusCode= {restResponse.Result.StatusCode}, Waiting {timeSpan.TotalSeconds} seconds before retry. Number attempt {retryCount}, RequestResponse={restResponse.Result.Content}, ErrorMessage={restResponse.Result.ErrorMessage}");
                    });
            return retryPolicy.Execute(() =>
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                return RestHTTPClient.Execute(restRequest);
            });
        }

        private HttpRequest GetHttpRequest(LTSRequest ltsRequest)
        {
            return new HttpRequest
            {
                Body = ltsRequest.Body, Headers = ltsRequest.Header, Host = ltsRequest.Endpoint,
                Method = ltsRequest.Method,
                URL = ltsRequest.Path, Region = ltsRequest.Region
            };
        }
    }
}