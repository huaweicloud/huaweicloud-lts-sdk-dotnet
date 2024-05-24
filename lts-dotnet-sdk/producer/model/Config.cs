using System.Collections.Generic;

namespace HuaweiCloud.SDK.LTS.Model
{
    public class Config
    {
        public long TotalSizeLnBytes { get; set; }
        public long MaxIoWorkers { get; set; }
        public int MaxBlockSec { get; set; }
        public long MaxBatchSize { get; set; }

        public int MaxBatchCount { get; set; }

        // 日志在batch中最多存放的时间，createTime + LingerMs - now < 0时，需要发送日志
        public long LingerMs { get; set; }
        public int Retries { get; set; }
        public int MaxReservedAttempts { get; set; }
        public long BaseRetryBackoffMs { get; set; }
        public long MaxRetryBackoffMs { get; set; }
        public int Buckets { get; set; }
        public string Endpoint { get; set; }
        public string AccessKeyID { get; set; }
        public string AccessKeySecret { get; set; }
        public string ProjectId { get; set; }
        public string RegionId { get; set; }
        public List<int> NoRetryCodeList { get; set; }
        public int HttpMaxTimeout { get; set; }

        public static Config GetConfig()
        {
            return new Config
            {
                TotalSizeLnBytes = 100 * 1024 * 1024,
                MaxIoWorkers = 10,
                MaxBlockSec = 60,
                MaxBatchSize = 512 * 1024,
                LingerMs = 2000,
                Retries = 3,
                MaxReservedAttempts = 11,
                BaseRetryBackoffMs = 100,
                MaxRetryBackoffMs = 50 * 1000,
                Buckets = 64,
                MaxBatchCount = 4096,
                NoRetryCodeList = new List<int> { 413, 429 },
                HttpMaxTimeout = 60
            };
        }
    }
}