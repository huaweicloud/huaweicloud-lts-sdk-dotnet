## 使用前提
- 要使用LTS .Net SDK ，您需要拥有云账号以及该账号对应的 Access Key（AK）和 Secret Access Key（SK）。 请在华为云控制台“我的凭证-访问密钥”页面上创建和查看您的 AK&SK 。更多信息请查看 [访问密钥](https://support.huaweicloud.com/usermanual-ca/zh-cn_topic_0046606340.html)  。
- 要使用LTS .Net SDK 上报日志 ，您需要确认已在 [华为云控制台](https://console.huaweicloud.com/console/?locale=zh-cn&region=cn-north-4#/home) 开通LTS服务
- LTS .Net SDK 适用于
  - .NET and .NET Core 6.0 及以上版本
  - .NET Framework 4.6.2 及以上版本

更多版本维护信息请参考 [.NET Core](https://learn.microsoft.com/zh-cn/lifecycle/faq/dotnet-core)、[.NET Framework](https://learn.microsoft.com/zh-cn/lifecycle/faq/dotnet-framework) 生命周期常见问题解答。


## 三方库
| 三方库                                  | License信息        | 链接                                       | 版本      |
|--------------------------------------|--------------------|--------------------------------------------|---------|
| Microsoft.Extensions.Http.Polly      | MIT license        | https://github.com/dotnet/aspnetcore       | 8.0.2   |
| RestSharp                            | Apache-2.0 license | https://github.com/restsharp/RestSharp     | 110.2.0 |
| Newtonsoft.Json                      | MIT license        | https://github.com/JamesNK/Newtonsoft.Json | 13.0.3  |
| Microsoft.Extensions.Logging         | MIT license        | https://github.com/dotnet/runtime          | 8.0.0   |
| Microsoft.Extensions.Logging.Console | MIT license        | https://github.com/dotnet/runtime          | 8.0.0   |


使用 .NET CLI 工具
```
dotnet add package Microsoft.Extensions.Http.Polly --version 8.0.2
dotnet add package RestSharp --version 110.2.0
dotnet add package Newtonsoft.Json --version 13.0.3
dotnet add package Microsoft.Extensions.Logging --version 8.0.0
dotnet add package Microsoft.Extensions.Logging.Console --version 8.0.0
```
使用 Package Manager
```
Install-Package Microsoft.Extensions.Http.Polly -version 8.0.2
Install-Package RestSharp -version 110.2.0
Install-Package Newtonsoft.Json -version 13.0.3
Install-Package Microsoft.Extensions.Logging -Version 8.0.0
Install-Package Microsoft.Extensions.Logging.Console -Version 8.0.0
```

## 参数配置
使用.net-sdk时，有以下配置参数：

| 参数名称               | 描述                                                                                              | 类型   | 是否必填 | 默认值                     |
|--------------------|-------------------------------------------------------------------------------------------------|--------|--------|-------------------------|
| ProjectId          | 华为云帐号的项目ID（project id）。                                                                         | String | 必填     |                         |
| AccessKeyId        | 华为云帐号的AK。                                                                                       | String | 必填     |                         |
| AccessKeySecret    | 华为云帐号的SK。                                                                                       | String | 必填     |                         |
| RegionName         | 云日志服务的区域。                                                                                       | String | 必填     |                         |
| Endpoint           | 日志上报的目的地址。                                                                                      | String | 必填     |                         |
| MaxBlockSec        | 如果 producer 可用空间不足，调用者在 send 方法上的最大阻塞时间，默认为 60 秒。建议为0秒。                                         | int    | 选填     | 0                       |
| MaxIoWorkers       | 执行日志发送任务的任务池大小。                                                                                 | int    | 选填     | 10                      |
| MaxBatchSize       | 当一个 ProducerBatch 中缓存的日志大小大于等于 batchSizeThresholdInBytes 时，该 batch 将被发送。                        | int    | 选填     | 0.5M（512 * 1024）        |
| MaxBatchCount      | 当一个 ProducerBatch 中缓存的日志条数大于等于 batchCountThreshold 时，该 batch 将被发送。                              | int    | 选填     | 4096                    |
| LingerMs           | 一个 ProducerBatch 从创建到可发送的逗留时间。                                                                  | int    | 选填     | 2S                      |
| Retries            | 如果某个 ProducerBatch 首次发送失败，能够对其重试的次数，建议为 3 次。如果 retries 小于等于 0，该 ProducerBatch 首次发送失败后将直接进入失败队列。 | int    | 选填     | 3                       |
| BaseRetryBackoffMs | 首次重试的退避时间。                                                                                      | long   | 选填     | 100ms                   |
| MaxRetryBackoffMs  | 重试的最大退避时间。                                                                                      | long   | 选填     | 50000ms                 |
| HttpMaxTimeout     | HTTP请求的超时时间                                                                                     |   int    |   选填   | 60s                     |



## 代码示例
``` csharp
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
            // 多线程
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
                    // var err = producer.SendLog("6909219c-8ed0-436c-9631-4a3f783bdb83", "596ae7a6-7e6c-4ab1-a1f0-338912cfb490", log);
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
```