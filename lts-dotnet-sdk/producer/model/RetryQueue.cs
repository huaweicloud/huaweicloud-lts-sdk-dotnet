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
using System.Collections.Generic;
using System.Threading;

namespace HuaweiCloud.SDK.LTS
{
    public class RetryQueue
    {
        public Queue<Batch> BatchQueue { get; set; } = new Queue<Batch>();
        public Mutex Mutex { get; set; } = new Mutex();


        public void SendToRetryQueue(Batch producerBatch)
        {
            try
            {
                Mutex.WaitOne();
                if (producerBatch != null)
                {
                    BatchQueue.Enqueue(producerBatch);
                }
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }

        public List<Batch> GetRetryBatch(bool moverShutDownFlag)
        {
            try
            {
                Mutex.WaitOne();
                List<Batch> retryBatchList = new List<Batch>();
                if (moverShutDownFlag)
                {
                    while (BatchQueue.Count > 0)
                    {
                        Batch retryBatch = BatchQueue.Dequeue();
                        retryBatchList.Add(retryBatch);
                    }
                }
                else
                {
                    while (BatchQueue.Count > 0)
                    {
                        Batch retryBatch = BatchQueue.Dequeue();
                        if (retryBatch.NextRetryMs < Common.GetTimeMs())
                        {
                            retryBatchList.Add(retryBatch);
                        }
                        else
                        {
                            BatchQueue.Enqueue(retryBatch);
                        }
                    }
                }

                return retryBatchList;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }

        public int Length()
        {
            if (BatchQueue == null)
            {
                return 0;
            }

            return BatchQueue.Count;
        }
    }
}