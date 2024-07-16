## 使用前提
- 要使用LTS .Net SDK ，您需要拥有云账号以及该账号对应的 Access Key（AK）和 Secret Access Key（SK）。 请在华为云控制台“我的凭证-访问密钥”页面上创建和查看您的 AK&SK 。更多信息请查看 [访问密钥](https://support.huaweicloud.com/usermanual-ca/zh-cn_topic_0046606340.html)  。
- 要使用LTS .Net SDK 上报日志 ，您需要确认已在 [华为云控制台](https://console.huaweicloud.com/console/?locale=zh-cn&region=cn-north-4#/home) 开通LTS服务
- LTS .Net SDK 适用于
    - .NET and .NET Core 6.0 及以上版本
    - .NET Framework 4.6.2 及以上版本

更多版本维护信息请参考 [.NET Core](https://learn.microsoft.com/zh-cn/lifecycle/faq/dotnet-core)、[.NET Framework](https://learn.microsoft.com/zh-cn/lifecycle/faq/dotnet-framework) 生命周期常见问题解答。


## 三方库
| 三方库                          | License信息        | 链接                                       | 版本    |
|---------------------------------|--------------------|--------------------------------------------|---------|
| Microsoft.Extensions.Http.Polly | MIT license        | https://github.com/dotnet/aspnetcore       | 8.0.2   |
| RestSharp                       | Apache-2.0 license | https://github.com/restsharp/RestSharp     | 110.2.0 |
| Newtonsoft.Json                 | MIT license        | https://github.com/JamesNK/Newtonsoft.Json | 13.0.3  |


使用 .NET CLI 工具
```
dotnet add package Microsoft.Extensions.Http.Polly --version 8.0.2
dotnet add package RestSharp --version 110.2.0
dotnet add package Newtonsoft.Json --version 13.0.3
```
使用 Package Manager
```
Install-Package Microsoft.Extensions.Http.Polly -version 8.0.2
Install-Package RestSharp -version 110.2.0
Install-Package Newtonsoft.Json -version 13.0.3
```

## 代码示例
```csharp
using HuaweiCloud.SDK.LTS;
using HuaweiCloud.SDK.LTS.Model;
using Newtonsoft.Json;

namespace HuaweiCloud.SDK.LTS.Sample
{
    public class Sample
    {
        public static void Main(string[] args)
        {
            testOneProducer();
        }

        private static void testOneProducer()
        {
            Console.WriteLine("start time is: {0}", DateTimeOffset.Now.ToUnixTimeMilliseconds());


            var producerConfig = Config.GetConfig();
            producerConfig.Endpoint = "endpoint";
            producerConfig.AccessKeyID = "ak";
            producerConfig.AccessKeySecret = "sk";
            producerConfig.RegionId = "region";
            producerConfig.ProjectId = "pid";
            var producerInstance = new Producer(producerConfig);
            producerInstance.Start();
            var wg = new WaitGroup();
            int threadCount = 100;
            for (int i = 0; i < threadCount; i++)
            {
                wg.Add(1);
                Task.Run(() => Handler.handle(producerInstance, wg));
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
            public static void handle(Producer producer, WaitGroup wg)
            {
                for (int j = 0; j < 1000; j++)
                {
                    var labels = new Dictionary<string, string>();
                    labels["keyA"] = "valueA";
                    labels["keyB"] = "valueB";
                    labels["keyC"] = "valueC";
                    string logContent = "content for this test " + j;
                    var log = LTS_Store.GenerateLog(new List<string> { logContent }, labels);
                    var err = producer.SendLog("groupId", "streamId1", log);
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