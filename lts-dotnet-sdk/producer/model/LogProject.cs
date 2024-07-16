using System.Text.RegularExpressions;
using HuaweiCloud.SDK.LTS.HTTP;
using HuaweiCloud.SDK.LTS.Model;
using RestSharp;

namespace HuaweiCloud.SDK.LTS
{
    public class LogProject
    {
        public string Region { get; set; } // region id
        public string Endpoint { get; set; }  // IP or hostname
        public string AccessKeyID { get; set; }
        public string AccessKeySecret { get; set; }
        public string BaseURL { get; set; }
        public long RetryTimeout { get; set; }
        public RestClient RestHttpClient { get; set; }
        public string ProjectId { get; set; }

        public void Init()
        {
            if (RetryTimeout == 0)
            {
                if (RestHttpClient == null)
                {
                    RestHttpClient = new RestClient();
                }

                RetryTimeout = Constant.DefaultRetryTimeout;
                ParseEndpoint();
            }
        }

        public void ParseEndpoint()
        {
            string scheme = Constant.HttpScheme;
            string host = Endpoint;
            if (Endpoint.StartsWith(scheme))
            {
                scheme = Constant.HttpScheme;
                host = Endpoint.Substring(scheme.Length);
            } else if (Endpoint.StartsWith(Constant.HttpsScheme))
            {
                scheme = Constant.HttpsScheme;
                host = Endpoint.Substring(scheme.Length);
            }

            if (Regex.IsMatch(host, Constant.IpRegexStr))
            {
                if (RestHttpClient == null)
                {
                    RestHttpClient = new RestClient();
                }

                RetryTimeout = Constant.DefaultRetryTimeout;
            }

            BaseURL = scheme + host;
        }

        public string GetBaseUrl()
        {
            if (Common.IsNotBlank(BaseURL))
            {
                return BaseURL;
            }
            ParseEndpoint();
            return BaseURL;
        }
    }
}

