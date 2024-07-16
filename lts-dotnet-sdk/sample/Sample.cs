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