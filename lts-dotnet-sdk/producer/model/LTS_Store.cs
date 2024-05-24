using HuaweiCloud.SDK.LTS.HTTP;
using HuaweiCloud.SDK.LTS.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuaweiCloud.SDK.LTS
{
    public class LTS_Store
    {
        
        public LogProject LogProject { get; set; }
        public CompressType PutLogCompressType { get; set; }
        public string GroupId { set; get; }
        public string StreamId { set; get; }
        
        public IHttpClient LtsHttpClient { set; get; }

        public static Log GenerateLog(List<string> logList, Dictionary<string, string> labels)
        {
            var content = logList.Select(value => new LogContent { LogTimeNs = Common.GetTimeNs(), Log = value }).ToList();

            string labelStr = @"{}";
            if (labels != null)
            {
                labelStr = JsonConvert.SerializeObject(labels);
            }

            return new Log { Contents = content, Labels = labelStr };
        }

        public Error putLogs(LogGroup logGroup)
        {
            if (logGroup == null || logGroup.IsEmptyLogs())
            {
                return null;
            }
            string body = JsonConvert.SerializeObject(logGroup);

            string path = string.Format("/v2/internal/{0}/lts/groups/{1}/streams/{2}/tenant/batch-contents",
                LogProject.ProjectId, GroupId, StreamId);
            var httpResult = LtsHttpClient.HttpPost(LogProject.GetBaseUrl(), path, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), body, LogProject.Region);
            return httpResult.ConvertError();
        }
    }
}

