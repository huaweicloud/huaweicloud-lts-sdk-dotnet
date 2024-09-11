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

using RestSharp;
using System;
using System.Collections.Generic;

namespace HuaweiCloud.SDK.LTS.HTTP
{
    public class LTSRequest
    {
        private const int GlobalDebugLevel = 0;
        private const bool RetryOnServerErrorEnabled = true;


        public string Endpoint { get; set; }
        public string Path { get; set; }
        public Method Method { get; set; }

        public Dictionary<string, Object> QueryParams { get; set; }
        public Dictionary<string, string> PathParams { get; set; }
        public Dictionary<string, string> Header { get; set; }
        public string Body { get; set; }
        
        public string Region { get; set; }

        public string ContentType { get; set; } = "application/json";
    }
}