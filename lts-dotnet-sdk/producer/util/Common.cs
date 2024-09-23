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

using HuaweiCloud.SDK.LTS.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace HuaweiCloud.SDK.LTS
{
    public class Common
    {
        public static string Delimiter = "|";

        public static long GetTimeMs()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public static long GetTimeNs()
        {
            var now = DateTimeOffset.Now;
            return now.ToUnixTimeMilliseconds();
        }

        // 计算Log中所有 logConent中日志的字节大小
        public static int GetLogSizeCalculate(Log lg)
        {
            int sizeInBytes = 4;
            if (lg == null)
            {
                return sizeInBytes;
            }

            var logContents = lg.Contents;
            if (logContents == null)
            {
                return sizeInBytes;
            }

            sizeInBytes += logContents.Select(logContent => logContent.Log == null ? string.Empty : logContent.Log)
                .Select(log => log.Length).Sum();

            return sizeInBytes;
        }

        public static int GetLogListSize(List<Log> logList)
        {
            int sizeInBytes = 0;
            if (logList == null || logList.Count == 0)
            {
                return sizeInBytes;
            }

            return logList.Sum(log => GetLogSizeCalculate(log));
        }

        public static int SovLog(long x)
        {
            int n = 0;
            while (true)
            {
                n++;
                x >>= 7;
                if (x == 0)
                {
                    break;
                }
            }

            return n;
        }

        public static bool IsNotBlank(string s)
        {
            return (s != null && s.Length > 0);
        }

        public static bool IsBlank(string s)
        {
            return (s == null || s.Length == 0);
        }

        public static RestClient DefaultHttpClient(Config config)
        {
            var options = new RestClientOptions
            {
                MaxTimeout = config.HttpMaxTimeout * 1000,
                ConfigureMessageHandler = handler => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = delegate { return true; }
                }
            };

            var client = new RestClient(options);
            return client;
        }
    }
}