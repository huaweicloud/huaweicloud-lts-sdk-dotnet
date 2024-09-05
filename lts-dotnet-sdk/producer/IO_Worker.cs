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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using HuaweiCloud.SDK.LTS.HTTP;
using HuaweiCloud.SDK.LTS.Model;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace HuaweiCloud.SDK.LTS
{
    public class IO_Worker
    {
        
        private static readonly ILogger logger;

        static IO_Worker()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            logger = loggerFactory.CreateLogger<IO_Worker>();
        }
        
        private long _taskCount;

        public static long logNum = 0;

        public long TaskCount
        {
            get => _taskCount;
            set => _taskCount = value;
        }
        public IHttpClient LtsHttpClient { set; get; }
        public RetryQueue RetryQueue { set; get; }
        private bool _retryQueueShutDownFlag;

        public bool RetryQueueShutDownFlag
        {
            get
            {
                lock (this)
                {
                    return _retryQueueShutDownFlag;
                }
            }
            set
            {
                lock (this)
                {
                    _retryQueueShutDownFlag = value;
                }
            }
        }

        public BlockingCollection<long> MaxIoWorker { set; get; }

        public Dictionary<int, string> NoRetryStatusCodeMap { set; get; }

        public Producer Producer { set; get; }

        public IO_Worker(IHttpClient ltsHttpClient, RetryQueue retryQueue, int maxIoWorkerCount,
            Dictionary<int, string> errorStatusMap, Producer producer)
        {
            LtsHttpClient = ltsHttpClient;
            RetryQueue = retryQueue;
            TaskCount = 0;
            _retryQueueShutDownFlag = false;
            MaxIoWorker = new BlockingCollection<long>(maxIoWorkerCount);
            NoRetryStatusCodeMap = errorStatusMap;
            Producer = producer;
        }

        public long AtomicLoadTaskCount()
        {
            return Interlocked.Read(ref _taskCount);
        }

        public void StartSendTask(Producer producer)
        {
            Interlocked.Increment(ref _taskCount);
            MaxIoWorker.Add(1);
            producer.AddIoWorkerWaitGroup(1);
        }

        public void EndThisSendTask(Producer producer)
        {
            MaxIoWorker.Take();
            Interlocked.Decrement(ref _taskCount);
            producer.AddIoWorkerWaitGroup(-1);
        }

        public void SendToServer(Batch producerBatch)
        {
            var beginMs = Common.GetTimeMs();
            var error = LtsHttpClient.putLogs(producerBatch.GroupId, producerBatch.StreamId, producerBatch.LogGroup);
            if (error == null)
            {
                Interlocked.Add(ref logNum, producerBatch.LogGroup.Logs.Count);
                long num = Interlocked.Read(ref logNum);
                logger.LogInformation("success send log num :" + num);
                Console.WriteLine("success send log num :" + num);
                if (producerBatch.AttemptCount < producerBatch.MaxReservedAttempts)
                {
                    var nowMs = Common.GetTimeMs();
                    var attempt = new Attempt(true, string.Empty, string.Empty, string.Empty, nowMs, nowMs - beginMs,
                        200);
                    producerBatch.Result.AttemptList.Add(attempt);
                }

                producerBatch.Result.Success = true;
                // After successful delivery, producer removes the batch size sent out
                Producer.AddProducerLogGroupSize(-producerBatch.TotalDataSize);
                if (producerBatch.CallBackList != null && producerBatch.CallBackList.Count > 0)
                {
                    foreach (var callBack in producerBatch.CallBackList)
                    {
                        callBack.Success(producerBatch.Result);
                    }
                }
            }
            else
            {
                if (RetryQueueShutDownFlag)
                {
                    if (producerBatch.CallBackList != null && producerBatch.CallBackList.Count > 0)
                    {
                        foreach (var callBack in producerBatch.CallBackList)
                        {
                            AddErrorMessageToBatchAttempt(producerBatch, error, false, beginMs);
                            callBack.Fail(producerBatch.Result);
                        }
                    }

                    return;
                }

                if (NoRetryStatusCodeMap.ContainsKey(error.HTTPCode))
                {
                    AddErrorMessageToBatchAttempt(producerBatch, error, false, beginMs);
                    ExcuteFailedCallback(producerBatch);
                    return;
                }

                if (producerBatch.AttemptCount < producerBatch.MaxRetryTimes)
                {
                    AddErrorMessageToBatchAttempt(producerBatch, error, true, beginMs);

                    var retryWaitTime = producerBatch.BaseRetryBackoffMs *
                                        (long)Math.Pow(2, (float)producerBatch.AttemptCount - 1);
                    if (retryWaitTime < producerBatch.MaxRetryIntervalInMs)
                    {
                        producerBatch.NextRetryMs = Common.GetTimeMs() + retryWaitTime;
                    }
                    else
                    {
                        producerBatch.NextRetryMs = Common.GetTimeMs() + producerBatch.MaxRetryIntervalInMs;
                    }

                    RetryQueue.SendToRetryQueue(producerBatch);
                }
                else
                {
                    ExcuteFailedCallback(producerBatch);
                }
            }
        }

        private void AddErrorMessageToBatchAttempt(Batch producerBatch, Error error, bool retryInfo, long beginMs)
        {
            if (producerBatch.AttemptCount < producerBatch.MaxReservedAttempts)
            {
                if (retryInfo)
                {
                }

                var nowMs = Common.GetTimeMs();
                var attempt = new Attempt(false, error.RequestID, error.ErrorCode, error.Message, nowMs,
                    nowMs - beginMs, error.HTTPCode);
                producerBatch.Result.AttemptList.Add(attempt);
            }

            producerBatch.Result.Success = false;
            producerBatch.AttemptCount += 1;
        }

        private void ExcuteFailedCallback(Batch producerBatch)
        {
            Producer.AddProducerLogGroupSize(-producerBatch.TotalDataSize);
            if (producerBatch.CallBackList != null && producerBatch.CallBackList.Count > 0)
            {
                foreach (var callBack in producerBatch.CallBackList)
                {
                    callBack.Fail(producerBatch.Result);
                }
            }
        }
    }
}