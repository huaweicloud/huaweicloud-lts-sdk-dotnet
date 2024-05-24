namespace HuaweiCloud.SDK.LTS.Model
{
    public class Constant
    {
        public const long DefaultRetryTimeout = 90;

        public const string HttpScheme = "http://";
        public const string HttpsScheme = "https://";
        public const string IpRegexStr = @"^((2((5[0-5])|([0-4]\d)))|([0-1]?\d{1,2}))(\.((2((5[0-5])|([0-4]\d)))|([0-1]?\d{1,2}))){3}$";
        public const string RequestIDHeader = "X-Request-Id";
        public const string AuthServiceName = "lts";

        public const int MaxRetryAttempts = 5;
        public const int PauseBetweenFailuresSecond = 5;
    }
}