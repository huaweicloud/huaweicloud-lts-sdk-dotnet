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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace HuaweiCloud.SDK.LTS.Model
{
    public class LogTag
    {
        [JsonProperty(PropertyName = "Key", NullValueHandling = NullValueHandling.Ignore)]
        public string Key { get; set; }


        [JsonProperty(PropertyName = "Value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        public int Size()
        {
            int l;
            int n = 0;
            if (Key != null)
            {
                l = Key.Length;
                n += 1 + l + Common.SovLog(l);
            }

            if (Value != null)
            {
                l = Value.Length;
                n += 1 + l + Common.SovLog(l);
            }

            return n;
        }
    }

    public class LogContent
    {
        [JsonProperty(PropertyName = "log_time_ns")]
        public long LogTimeNs { get; set; }

        [JsonProperty(PropertyName = "log")] public string Log { get; set; }

        public int Size()
        {
            int logLength = 0;
            if (Log != null)
            {
                logLength = Log.Length;
            }

            return logLength + Common.SovLog(LogTimeNs);
        }
    }

    public class Log
    {
        [JsonProperty(PropertyName = "time", NullValueHandling = NullValueHandling.Ignore)]
        public long Time { get; set; }

        [JsonProperty(PropertyName = "contents", NullValueHandling = NullValueHandling.Ignore)]
        public List<LogContent> Contents { get; set; }

        [JsonProperty(PropertyName = "labels")]
        public string Labels { get; set; }

        [JsonProperty(PropertyName = "tenant_project_id", NullValueHandling = NullValueHandling.Ignore)]
        public string ProjectId { get; set; }

        public int Size()
        {
            int l;
            int n = 0;
            n += 1 + Common.SovLog(Time);

            if (Contents == null || Contents.Count <= 0) return n;
            foreach (var content in Contents)
            {
                l = content == null ? 0 : content.Size();
                n += 1 + l + Common.SovLog(l);
            }

            return n;
        }
    }

    public class LogGroup
    {
        [JsonProperty(PropertyName = "log_items", NullValueHandling = NullValueHandling.Ignore)]
        public List<Log> Logs { get; set; }

        [JsonProperty(PropertyName = "Category", NullValueHandling = NullValueHandling.Ignore)]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "LogTags", NullValueHandling = NullValueHandling.Ignore)]
        public List<LogTag> LogTag { get; set; }

        public bool IsEmptyLogs()
        {
            return (Logs == null || Logs.Count == 0);
        }

        public int Size()
        {
            int l;
            int n = 0;
            if (Logs != null && Logs.Count > 0)
            {
                foreach (var log in Logs)
                {
                    l = log.Size();
                    n += 1 + l + Common.SovLog(l);
                }
            }

            if (Category != null)
            {
                l = Category.Length;
                n += 1 + l + Common.SovLog(l);
            }

            if (LogTag != null && LogTag.Count > 0)
            {
                foreach (var logTag in LogTag)
                {
                    l = logTag.Size();
                    n += 1 + l + Common.SovLog(l);
                }
            }

            return n;
        }
    }

    public class LogItem
    {
        [JsonProperty(PropertyName = "contents")]
        public List<LogContent> Contents { get; set; }

        [JsonProperty(PropertyName = "labels")]
        public string Labels { get; set; }

        [JsonProperty(PropertyName = "tenant_project_id")]
        public string TenantProjectId { get; set; }
    }

    public class LogItems
    {
        [JsonProperty(PropertyName = "log_items")]
        public List<LogItem> LogItemList { get; set; }
    }
}