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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using HuaweiCloud.SDK.LTS.Model;
using Newtonsoft.Json;

namespace HuaweiCloud.SDK.LTS.HTTP
{
    public class Result
    {
        public List<Attempt> AttemptList { get; set; } = new List<Attempt>();
        public bool Success { get; set; } = false;

        public string GetErrorCode()
        {
            if (AttemptList == null || AttemptList.Count == 0)
            {
                return "";
            }

            var latsAttempt = AttemptList.Last();
            if (latsAttempt == null)
            {
                return "";
            }

            return latsAttempt.ErrorCode;
        }

        public string GetErrorMessage()
        {
            if (AttemptList == null || AttemptList.Count == 0)
            {
                return "";
            }

            var latsAttempt = AttemptList.Last();
            if (latsAttempt == null)
            {
                return "";
            }

            return latsAttempt.ErrorMessage;
        }

        public string GetRequestId()
        {
            if (AttemptList == null || AttemptList.Count == 0)
            {
                return "";
            }

            var latsAttempt = AttemptList.Last();
            if (latsAttempt == null)
            {
                return "";
            }

            return latsAttempt.RequestId;
        }

        public long GetTimeStampMs()
        {
            if (AttemptList == null || AttemptList.Count == 0)
            {
                return 0;
            }

            var latsAttempt = AttemptList.Last();
            if (latsAttempt == null)
            {
                return 0;
            }

            return latsAttempt.TimeStampMs;
        }

        public int GetHttpCode()
        {
            if (AttemptList == null || AttemptList.Count == 0)
            {
                return 0;
            }

            var latsAttempt = AttemptList.Last();
            if (latsAttempt == null)
            {
                return 0;
            }

            return latsAttempt.HttpCode;
        }
    }

    public class Attempt
    {
        public bool Success { get; set; }
        public int HttpCode { get; set; }
        public string RequestId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public long TimeStampMs { get; set; }
        public long LastAttemptCostMs { get; set; }

        public Attempt(bool success, string requestId, string errorCode, string errorMessage, long timeStampMs,
            long lastAttemptCostMs, int httpCode)
        {
            Success = success;
            RequestId = requestId;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            TimeStampMs = timeStampMs;
            LastAttemptCostMs = lastAttemptCostMs;
            HttpCode = httpCode;
        }
    }

    public class BadResponseError
    {
        public string RespBody { get; set; }
        public Dictionary<string, List<string>> RespHeader { get; set; }
        public int HTTPCode { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string Error()
        {
            return this.ToString();
        }

        public BadResponseError(string respBody, Dictionary<string, List<string>> respHeader, int httpCode)
        {
            RespBody = respBody;
            RespHeader = respHeader;
            HTTPCode = httpCode;
        }
    }

    public class HttpResult
    {
        public bool Success { get; set; }
        public HttpStatusCode HTTPCode { get; set; }
        public string RespBody { get; set; }

        public Error ConvertError()
        {
            if (Success)
            {
                return null;
            }

            return new Error { HTTPCode = (int)HTTPCode, ErrorCode = RespBody ?? "", Message = RespBody ?? "" };
        }
    }
}