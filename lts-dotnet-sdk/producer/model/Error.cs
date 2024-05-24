using Newtonsoft.Json;

namespace HuaweiCloud.SDK.LTS.Model
{
    public class Error
    {
        [JsonProperty(PropertyName = "httpCode")]
        public int HTTPCode { get; set; }

        [JsonProperty(PropertyName = "errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty(PropertyName = "errorMessage")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "requestID")]
        public string RequestID { get; set; }
    }
}