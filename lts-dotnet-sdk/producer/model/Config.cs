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

namespace HuaweiCloud.SDK.LTS.Model
{
    public class Config
    {
        public long TotalSizeLnBytes { get; set; }
        public long MaxIoWorkers { get; set; }
        public int MaxBlockSec { get; set; }
        public long MaxBatchSize { get; set; }

        public int MaxBatchCount { get; set; }

        // 日志在batch中最多存放的时间，createTime + LingerMs - now < 0时，需要发送日志
        public long LingerMs { get; set; }
        public int Retries { get; set; }
        public int MaxReservedAttempts { get; set; }
        public long BaseRetryBackoffMs { get; set; }
        public long MaxRetryBackoffMs { get; set; }
        public int Buckets { get; set; }
        public string Endpoint { get; set; }
        public string AccessKeyID { get; set; }
        public string AccessKeySecret { get; set; }
        public string ProjectId { get; set; }
        public string RegionId { get; set; }
        public List<int> NoRetryCodeList { get; set; }
        public int HttpMaxTimeout { get; set; }

        public static Config GetConfig()
        {
            return new Config
            {
                TotalSizeLnBytes = 100 * 1024 * 1024,
                MaxIoWorkers = 10,
                MaxBlockSec = 60,
                MaxBatchSize = 512 * 1024,
                LingerMs = 2000,
                Retries = 3,
                MaxReservedAttempts = 11,
                BaseRetryBackoffMs = 100,
                MaxRetryBackoffMs = 50 * 1000,
                Buckets = 64,
                MaxBatchCount = 4096,
                NoRetryCodeList = new List<int> { 413, 429 },
                HttpMaxTimeout = 60
            };
        }
    }
}