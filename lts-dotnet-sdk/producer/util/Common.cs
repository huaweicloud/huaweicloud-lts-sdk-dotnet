using HuaweiCloud.SDK.LTS.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace HuaweiCloud.SDK.LTS
{
    public class Common
    {
        public static string Delimiter = "|";

        public static long GetTimeMs()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public static long GetTimeNs()
        {
            var now = DateTimeOffset.Now;
            return now.ToUnixTimeMilliseconds();
        }

        // 计算Log中所有 logConent中日志的字节大小
        public static int GetLogSizeCalculate(Log lg)
        {
            int sizeInBytes = 4;
            if (lg == null)
            {
                return sizeInBytes;
            }

            var logContents = lg.Contents;
            if (logContents == null)
            {
                return sizeInBytes;
            }

            sizeInBytes += logContents.Select(logContent => logContent.Log == null ? string.Empty : logContent.Log)
                .Select(log => log.Length).Sum();

            return sizeInBytes;
        }

        public static int GetLogListSize(List<Log> logList)
        {
            int sizeInBytes = 0;
            if (logList == null || logList.Count == 0)
            {
                return sizeInBytes;
            }

            return logList.Sum(log => GetLogSizeCalculate(log));
        }

        public static int SovLog(long x)
        {
            int n = 0;
            while (true)
            {
                n++;
                x >>= 7;
                if (x == 0)
                {
                    break;
                }
            }

            return n;
        }

        public static bool IsNotBlank(string s)
        {
            return (s != null && s.Length > 0);
        }

        public static bool IsBlank(string s)
        {
            return (s == null || s.Length == 0);
        }

        public static RestClient DefaultHttpClient(Config config)
        {
            var options = new RestClientOptions
            {
                MaxTimeout = config.HttpMaxTimeout * 1000,
                ConfigureMessageHandler = handler => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = delegate { return true; }
                }
            };

            var client = new RestClient(options);
            return client;
        }
    }
}