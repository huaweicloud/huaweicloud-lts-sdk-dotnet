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

using HuaweiCloud.SDK.LTS.HTTP;
using HuaweiCloud.SDK.LTS.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuaweiCloud.SDK.LTS
{
    public class LTS_Store
    {
        
        public LogProject LogProject { get; set; }
        public CompressType PutLogCompressType { get; set; }
        public string GroupId { set; get; }
        public string StreamId { set; get; }
        
        public IHttpClient LtsHttpClient { set; get; }

        public static Log GenerateLog(List<string> logList, Dictionary<string, string> labels)
        {
            var content = logList.Select(value => new LogContent { LogTimeNs = Common.GetTimeNs(), Log = value }).ToList();

            string labelStr = @"{}";
            if (labels != null)
            {
                labelStr = JsonConvert.SerializeObject(labels);
            }

            return new Log { Contents = content, Labels = labelStr };
        }

        public Error putLogs(LogGroup logGroup)
        {
            if (logGroup == null || logGroup.IsEmptyLogs())
            {
                return null;
            }
            string body = JsonConvert.SerializeObject(logGroup);

            string path = string.Format("/v2/internal/{0}/lts/groups/{1}/streams/{2}/tenant/batch-contents",
                LogProject.ProjectId, GroupId, StreamId);
            var httpResult = LtsHttpClient.HttpPost(LogProject.GetBaseUrl(), path, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), body, LogProject.Region);
            return httpResult.ConvertError();
        }
    }
}

