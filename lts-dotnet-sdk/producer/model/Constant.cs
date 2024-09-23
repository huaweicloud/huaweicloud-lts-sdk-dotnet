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

namespace HuaweiCloud.SDK.LTS.Model
{
    public class Constant
    {
        public const long DefaultRetryTimeout = 90;

        public const string HttpScheme = "http://";
        public const string HttpsScheme = "https://";
        public const string IpRegexStr = @"^((2((5[0-5])|([0-4]\d)))|([0-1]?\d{1,2}))(\.((2((5[0-5])|([0-4]\d)))|([0-1]?\d{1,2}))){3}$";
        public const string RequestIDHeader = "X-Request-Id";
        public const string AuthServiceName = "lts";

        public const int MaxRetryAttempts = 5;
        public const int PauseBetweenFailuresSecond = 5;
    }
}