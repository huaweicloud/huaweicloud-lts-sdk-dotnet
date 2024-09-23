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

using System.Text.RegularExpressions;
using HuaweiCloud.SDK.LTS.HTTP;
using HuaweiCloud.SDK.LTS.Model;
using RestSharp;

namespace HuaweiCloud.SDK.LTS
{
    public class LogProject
    {
        public string Region { get; set; } // region id
        public string Endpoint { get; set; }  // IP or hostname
        public string AccessKeyID { get; set; }
        public string AccessKeySecret { get; set; }
        public string BaseURL { get; set; }
        public long RetryTimeout { get; set; }
        public RestClient RestHttpClient { get; set; }
        public string ProjectId { get; set; }

        public void Init()
        {
            if (RetryTimeout == 0)
            {
                if (RestHttpClient == null)
                {
                    RestHttpClient = new RestClient();
                }

                RetryTimeout = Constant.DefaultRetryTimeout;
                ParseEndpoint();
            }
        }

        public void ParseEndpoint()
        {
            string scheme = Constant.HttpScheme;
            string host = Endpoint;
            if (Endpoint.StartsWith(scheme))
            {
                scheme = Constant.HttpScheme;
                host = Endpoint.Substring(scheme.Length);
            } else if (Endpoint.StartsWith(Constant.HttpsScheme))
            {
                scheme = Constant.HttpsScheme;
                host = Endpoint.Substring(scheme.Length);
            }

            if (Regex.IsMatch(host, Constant.IpRegexStr))
            {
                if (RestHttpClient == null)
                {
                    RestHttpClient = new RestClient();
                }

                RetryTimeout = Constant.DefaultRetryTimeout;
            }

            BaseURL = scheme + host;
        }

        public string GetBaseUrl()
        {
            if (Common.IsNotBlank(BaseURL))
            {
                return BaseURL;
            }
            ParseEndpoint();
            return BaseURL;
        }
    }
}

