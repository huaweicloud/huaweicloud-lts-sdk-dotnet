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