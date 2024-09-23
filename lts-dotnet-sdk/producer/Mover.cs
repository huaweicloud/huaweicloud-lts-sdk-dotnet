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
using System;
using System.Collections.Generic;
using System.Threading;

namespace HuaweiCloud.SDK.LTS
{
    public class Mover
    {
        private bool _moverShutDownFlag;

        public bool MoverShutDownFlag
        {
            get
            {
                lock (this)
                {
                    return _moverShutDownFlag;
                }
            }
            set
            {
                lock (this)
                {
                    _moverShutDownFlag = value;
                }
            }
        }

        public RetryQueue RetryQueue { get; set; }
        public IO_Worker IoWorker { get; set; }
        public LogAccumulator LogAccumulator { get; set; }
        public IO_ThreadPool IoThreadPool { get; set; }

        public Mover(RetryQueue retryQueue, IO_Worker ioWorker, LogAccumulator logAccumulator,
            IO_ThreadPool ioThreadPool)
        {
            MoverShutDownFlag = false;
            RetryQueue = retryQueue;
            IoWorker = ioWorker;
            LogAccumulator = logAccumulator;
            IoThreadPool = ioThreadPool;
        }

        public void run(ref long moverWaitGroup, Config config)
        {
            try
            {
                while (!MoverShutDownFlag)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(config.LingerMs));
                    var sleepMs = config.LingerMs;
                    var nowTimeMs = Common.GetTimeMs();
                    int mapCount = 0;
                    try
                    {
                        LogAccumulator.RwLock.EnterWriteLock();
                        if (LogAccumulator.LogGroupData == null)
                        {
                            mapCount = 0;
                        }
                        else
                        {
                            mapCount = LogAccumulator.LogGroupData.Count;
                        }

                        List<string> needSendToServerBatchKeys = new List<string>();
                        foreach (var keyBatchPair in LogAccumulator.LogGroupData)
                        {
                            var key = keyBatchPair.Key;
                            var batch = keyBatchPair.Value;
                            var timeInterval = batch.CreateTimeMs + config.LingerMs - nowTimeMs;
                            if (timeInterval <= 0)
                            {
                                needSendToServerBatchKeys.Add(key);
                            }
                            else
                            {
                                if (sleepMs > timeInterval)
                                {
                                    sleepMs = timeInterval;
                                }
                            }
                        }

                        foreach (var key in needSendToServerBatchKeys)
                        {
                            if (LogAccumulator.LogGroupData.ContainsKey(key))
                            {
                                var batch = LogAccumulator.LogGroupData[key];
                                SendToServer(key, batch, config);
                            }
                        }
                    }
                    finally
                    {
                        LogAccumulator.RwLock.ExitWriteLock();
                    }

                    if (mapCount == 0)
                    {
                        sleepMs = config.LingerMs;
                    }

                    var retryProducerBatchList = RetryQueue.GetRetryBatch(MoverShutDownFlag);
                    if (retryProducerBatchList == null)
                    {
                        // If there is nothing to send in the retry queue, just wait for the minimum time that was given to me last time.
                        Thread.Sleep(TimeSpan.FromMilliseconds(sleepMs));
                    }
                    else
                    {
                        if (retryProducerBatchList != null)
                        {
                            foreach (var retryBatch in retryProducerBatchList)
                            {
                                IoThreadPool.AddTask(retryBatch);
                            }
                        }
                    }

                    if (MoverShutDownFlag)
                    {
                        break;
                    }
                }

                // when mover shutdown, deal all batch in LogAccumulator and retyr batch
                try
                {
                    LogAccumulator.RwLock.EnterWriteLock();
                    if (LogAccumulator.LogGroupData != null)
                    {
                        foreach (var keyValuePair in LogAccumulator.LogGroupData)
                        {
                            var batch = keyValuePair.Value;
                            IoThreadPool.AddTask(batch);
                        }
                    }

                    LogAccumulator.LogGroupData = new Dictionary<string, Batch>();
                }
                finally
                {
                    LogAccumulator.RwLock.ExitWriteLock();
                }

                var producerBatchList = RetryQueue.GetRetryBatch(MoverShutDownFlag);
                if (producerBatchList != null)
                {
                    foreach (var producerBatch in producerBatchList)
                    {
                        IoThreadPool.AddTask(producerBatch);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref moverWaitGroup);
            }
        }

        private void SendToServer(string key, Batch batch, Config config)
        {
            if (!LogAccumulator.LogGroupData.ContainsKey(key))
            {
                return;
            }
            // double check，只有 createTime + lingerMs  < now 时才会发送
            else if (Common.GetTimeMs() - LogAccumulator.LogGroupData[key].CreateTimeMs < config.LingerMs)
            {
                return;
            }

            IoThreadPool.AddTask(batch);
            LogAccumulator.LogGroupData.Remove(key);
        }
    }
}