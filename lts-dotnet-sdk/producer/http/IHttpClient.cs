using HuaweiCloud.SDK.LTS.Model;
using System.Collections.Generic;

namespace HuaweiCloud.SDK.LTS.HTTP
{
    public interface IHttpClient
    {
        Error putLogs(string GroupId, string streamId, LogGroup logGroup);
        
        HttpResult HttpGet(string url, string path, Dictionary<string, string> header, string region);
        
        HttpResult HttpPost(string url, string path, Dictionary<string, string> header, string body, string region);
    }
}