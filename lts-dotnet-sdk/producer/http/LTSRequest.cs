using RestSharp;
using System;
using System.Collections.Generic;

namespace HuaweiCloud.SDK.LTS.HTTP
{
    public class LTSRequest
    {
        private const int GlobalDebugLevel = 0;
        private const bool RetryOnServerErrorEnabled = true;


        public string Endpoint { get; set; }
        public string Path { get; set; }
        public Method Method { get; set; }

        public Dictionary<string, Object> QueryParams { get; set; }
        public Dictionary<string, string> PathParams { get; set; }
        public Dictionary<string, string> Header { get; set; }
        public string Body { get; set; }
        
        public string Region { get; set; }

        public string ContentType { get; set; } = "application/json";
    }
}