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
using System.Threading;
using System.Threading.Tasks;

namespace HuaweiCloud.SDK.LTS
{
    
    public class Producer
    {
        private Config _config;
        private LogAccumulator _logAccumulator;
        private Mover _mover;
        private IO_ThreadPool _threadPool;
        private long _moverWaitGroup;
        private long _ioWorkerWaitGroup;
        private long _ioThreadPoolWaitGroup;
        private int _buckets;
        private long _producerLogGroupSize;
        
        public long ProducerLogGroupSize
        {
            get => _producerLogGroupSize;
            set => _producerLogGroupSize = value;
        }

        public long MoverWaitGroup
        {
            get => _moverWaitGroup;
            set => _moverWaitGroup = value;
        }

        public long IoWorkerWaitGroup
        {
            get => _ioWorkerWaitGroup;
            set => _ioWorkerWaitGroup = value;
        }

        public long IoThreadPoolWaitGroup
        {
            get => _ioThreadPoolWaitGroup;
            set => _ioThreadPoolWaitGroup = value;
        }

        public Config ProducerConfig
        {
            get => _config;
            set => _config = value;
        }

        public LogAccumulator LogAccumulator
        {
            get => _logAccumulator;
            set => _logAccumulator = value;
        }

        public Mover Mover
        {
            get => _mover;
            set => _mover = value;
        }

        public IO_ThreadPool ThreadPool
        {
            get => _threadPool;
            set => _threadPool = value;
        }

        public int Buckets
        {
            get => _buckets;
            set => _buckets = value;
        }
        
        public void AddProducerLogGroupSize(long delta)
        {
            Interlocked.Add(ref _producerLogGroupSize, delta);
        }
        
        public void AddMoverWaitGroup(long delta)
        {
            Interlocked.Add(ref _moverWaitGroup, delta);
        }
        
        public void AddIoWorkerWaitGroup(long delta)
        {
            Interlocked.Add(ref _ioWorkerWaitGroup, delta);
        }
        
        public void AddIoThreadPoolWaitGroup(long delta)
        {
            Interlocked.Add(ref _ioThreadPoolWaitGroup, delta);
        }

        public long AtomicLoadLogGroupSize()
        {
            return Interlocked.Read(ref _producerLogGroupSize);
        }

        public Producer(Config config)
        {
            var client = HuaweiCloud.SDK.LTS.HTTP.LTSHttpClient.CreateIHttpClient(config);
            var finalProducerConfig = ValidateProducerConfig(config);
            var retryQueue = new RetryQueue();
            var errorStatusMap = GetErrorCodeMap(finalProducerConfig);
            _config = finalProducerConfig;
            Buckets = finalProducerConfig.Buckets;

            var ioWorker = new IO_Worker(client, retryQueue, (int)finalProducerConfig.MaxIoWorkers, errorStatusMap, this);
            var ioThreadPool = new IO_ThreadPool(ioWorker);
            var logAccumulator = new LogAccumulator(finalProducerConfig, ioWorker, ioThreadPool, this);
            var mover = new Mover(retryQueue, ioWorker, logAccumulator, ioThreadPool);

            _logAccumulator = logAccumulator;
            _mover = mover;
            _threadPool = ioThreadPool;
            _moverWaitGroup = 0;
            _ioWorkerWaitGroup = 0;
            _ioThreadPoolWaitGroup = 0;
        }

        private static Dictionary<int, string> GetErrorCodeMap(Config finalProducerConfig)
        {
            var errorCodeMap = new Dictionary<int, string>();
            if (finalProducerConfig.NoRetryCodeList == null)
            {
                return errorCodeMap;
            }

            foreach (var noRetryCode in finalProducerConfig.NoRetryCodeList)
            {
                errorCodeMap.Add(noRetryCode, "");
            }

            return errorCodeMap;
        }

        private Config ValidateProducerConfig(Config producerConfig)
        {
            if (producerConfig.MaxReservedAttempts <= 0)
            {
                producerConfig.MaxReservedAttempts = 11;
            }
            if (producerConfig.MaxBatchCount  > 40960 || producerConfig.MaxBatchCount <= 0)
            {
                producerConfig.MaxBatchCount = 40960;
            }
            if (producerConfig.MaxBatchSize > 512*1024 || producerConfig.MaxBatchSize <= 0)
            {
                producerConfig.MaxBatchSize = 512 * 1024;
            }
            if (producerConfig.MaxIoWorkers <= 0)
            {
                producerConfig.MaxIoWorkers = 10;
            }
            if (producerConfig.BaseRetryBackoffMs <= 0)
            {
                producerConfig.BaseRetryBackoffMs = 100;
            }
            if (producerConfig.TotalSizeLnBytes <= 0)
            {
                producerConfig.TotalSizeLnBytes = 100 * 1024 * 1024;
            }
            if (producerConfig.LingerMs < 100)
            {
                producerConfig.LingerMs = 2000;
            }

            return producerConfig;
        }

        public void Start()
        {
            Interlocked.Increment(ref _moverWaitGroup);
            Task.Run(() => Mover.run(ref _moverWaitGroup, ProducerConfig));
            Interlocked.Increment(ref _ioThreadPoolWaitGroup);
            Task.Run(() => ThreadPool.Start(this));
        }

        public void Close(long timeoutMs)
        {
            var startCloseTime = Common.GetTimeMs();
            SendCloseProdcerSignal();
            while (Interlocked.Read(ref _moverWaitGroup) != 0)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(5));
            }
            ThreadPool.ThreadPoolShutDownFlag = true;

            while (true)
            {
                if (Mover.IoWorker.AtomicLoadTaskCount() == 0 && !ThreadPool.hasTask())
                {
                    return;
                }

                if (Common.GetTimeMs() - startCloseTime > timeoutMs)
                {
                    throw new TimeoutException();
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }
            
        }

        private void SendCloseProdcerSignal()
        {
            Mover.MoverShutDownFlag = true;
            LogAccumulator.ShutDownFlag = true;
            Mover.IoWorker.RetryQueueShutDownFlag = true;
        }

        public Error SendLog(string groupId, string streamId, Log log)
        {
            return LogAccumulator.AddLogToProducerBatch(groupId, streamId, log, null);
        }
        
        public Error SendLogWithCallBack(string groupId, string streamId, Log log, CallBack callBack)
        {
            return LogAccumulator.AddLogToProducerBatch(groupId, streamId, log, callBack);
        }
    }
}
