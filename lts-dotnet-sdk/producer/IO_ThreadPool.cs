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
using System.Threading.Tasks;
using System.Text.Json;

namespace HuaweiCloud.SDK.LTS
{
    public class IO_ThreadPool
    {
        private bool _threadPoolShutDownFlag;

        public bool ThreadPoolShutDownFlag
        {
            get
            {
                lock (this)
                {
                    return _threadPoolShutDownFlag;
                }
            }
            set
            {
                lock (this)
                {
                    _threadPoolShutDownFlag = value;
                }
            }
        }

        public Queue<Batch> DealBatchQueue { get; set; }

        public ReaderWriterLockSlim RwLock { get; set; }

        public IO_Worker IoWorker { get; set; }

        public IO_ThreadPool(IO_Worker ioWorker)
        {
            _threadPoolShutDownFlag = false;
            DealBatchQueue = new Queue<Batch>();
            IoWorker = ioWorker;
            RwLock = new ReaderWriterLockSlim();
        }

        public void AddTask(Batch batch)
        {
            try
            {
                RwLock.EnterWriteLock();
                DealBatchQueue.Enqueue(batch);
            }
            finally
            {
                RwLock.ExitWriteLock();
            }
        }

        public Batch popTask()
        {
            try
            {
                RwLock.EnterWriteLock();
                if (DealBatchQueue == null || DealBatchQueue.Count == 0)
                {
                    return null;
                }

                var batch = DealBatchQueue.Dequeue();

                return batch;
            }
            finally
            {
                RwLock.ExitWriteLock();
            }
        }

        public bool hasTask()
        {
            try
            {
                RwLock.EnterReadLock();
                if (DealBatchQueue == null)
                {
                    return false;
                }

                return DealBatchQueue.Count > 0;
            }
            finally
            {
                RwLock.ExitReadLock();
            }
        }

        
    public class Handler
    {
        public static void handle(IO_Worker ioWorker, Batch batch, Producer producer)
        {
            try
            {
                ioWorker.SendToServer(batch);
            }
            finally
            {
                ioWorker.EndThisSendTask(producer);
            }
        }
    }


        public void Start(Producer producer)
        {
            try
            {
                while (true)
                {
                    var task = popTask();
                    if (task != null)
                    {
                        IoWorker.StartSendTask(producer);
                        Task.Run(() => { Handler.handle(IoWorker, task, producer); });
                        Thread.Sleep(TimeSpan.FromMilliseconds(1));
                    }
                    else
                    {
                        if (!ThreadPoolShutDownFlag)
                        {
                            Thread.Sleep(TimeSpan.FromMilliseconds(100));
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                producer.AddIoThreadPoolWaitGroup(-1);
            }
        }
    }
}