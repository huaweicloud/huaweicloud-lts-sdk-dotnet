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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HuaweiCloud.SDK.LTS.Sample
{
    public class Sample
    {
        public static void SendLogToLts(string endpoint, string accessKey, string accessSecret, string region, string projectId, string groupId, string streamId,
            int threadCount, int logItemNum, int HttpTimeout, int LingerMs, string logContent, long maxIO)
        {
            Console.WriteLine("start time is: {0}", DateTimeOffset.Now.ToUnixTimeMilliseconds());
            var producerConfig = Config.GetConfig();
            producerConfig.LingerMs = LingerMs;
            producerConfig.Endpoint = endpoint;
            producerConfig.AccessKeyID = accessKey;
            producerConfig.AccessKeySecret = accessSecret;
            producerConfig.RegionId = region;
            producerConfig.ProjectId = projectId;
            producerConfig.HttpMaxTimeout = HttpTimeout;
            producerConfig.MaxIoWorkers = maxIO;
            var producerInstance = new Producer(producerConfig);
            
            producerInstance.Start();
            var wg = new WaitGroup();
            for (int i = 0; i < threadCount; i++)
            {
                wg.Add(1);
                Task.Run(() => Handler.handle(producerInstance, wg, groupId, streamId, logItemNum, logContent));
            }

            wg.wait();
            Console.WriteLine("send all complate ...");
            Thread.Sleep(TimeSpan.FromSeconds(3000));
        }

        public class WaitGroup
        {
            private long _wg = 0;

            public long Wg
            {
                get => _wg;
                set => _wg = value;
            }

            public void Add(int delta)
            {
                Interlocked.Add(ref _wg, delta);
            }

            public void Done()
            {
                Interlocked.Decrement(ref _wg);
            }

            public void wait()
            {
                while (Interlocked.Read(ref _wg) != 0)
                {
                    Thread.Sleep(200);
                }
            }
        }

        public class Handler
        {
            public static void handle(Producer producer, WaitGroup wg, string groupId, string streamId, int logItemNums, string logContent)
            {
                for (int j = 0; j < logItemNums; j++)
                {
                    var labels = new Dictionary<string, string>();
                    labels["keyA"] = "valueA";
                    labels["keyB"] = "valueB";
                    labels["keyC"] = "valueC";
                    var log = LTS_Store.GenerateLog(new List<string> { logContent }, labels);
                    var err = producer.SendLog(groupId, streamId, log);
                    if (err != null)
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(err));
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(0.1));
                }

                wg.Done();
            }
        }
    }
}