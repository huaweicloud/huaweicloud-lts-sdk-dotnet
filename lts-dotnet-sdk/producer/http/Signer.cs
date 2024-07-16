using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HuaweiCloud.SDK.LTS.Model;
using RestSharp;

// HWS API Gateway Signature

namespace HuaweiCloud.SDK.LTS.HTTP
{
    public class HttpRequest
    {
        public Method Method { get; set; }
        public string Host { get; set; } 
        public string URL { get; set; } = "/"; 
        public Dictionary<string, List<string>> Query { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        public string Region { get; set; }
        public string Body { get; set; } = "";

        public HttpRequest(Method method = Method.Get, Uri url = null, Dictionary<string, string> headers = null,
            string body = null)
        {
            if (method != null)
            {
                this.Method = method;
            }

            if (url != null)
            {
                Host = url.Scheme + "://" + url.Host + ":" + url.Port;
                URL = url.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
                Query = new Dictionary<string, List<string>>();
                if (url.Query.Length > 1)
                {
                    foreach (var kv in url.Query.Substring(1).Split('&'))
                    {
                        string[] spl = kv.Split(new char[] { '=' }, 2);
                        string key = Uri.UnescapeDataString(spl[0]);
                        string value = "";
                        if (spl.Length > 1)
                        {
                            value = Uri.UnescapeDataString(spl[1]);
                        }

                        if (Query.ContainsKey(key))
                        {
                            Query[key].Add(value);
                        }
                        else
                        {
                            Query[key] = new List<string> { value };
                        }
                    }
                }
            }

            if (headers != null)
            {
                this.Headers = headers;
            }

            if (body != null)
            {
                this.Body = body;
            }
        }
    }

    public class Signer
    {
        const string BasicDateFormat = "yyyyMMddTHHmmssZ";

        private const string DerivedDateFormat = "yyyyMMdd";
        private const string shortTimeFormat   = "yyyyMMdd";
        const string Algorithm = "SDK-HMAC-SHA256";
        const string HeaderXDate = "X-Sdk-Date";
        const string HeaderHost = "host";
        const string HeaderAuthorization = "Authorization";
        const string HeaderContentSha256 = "X-Sdk-Content-Sha256";
        private const string DerivationAlgorithm = "V11-HMAC-SHA256";
        readonly HashSet<string> unsignedHeaders = new HashSet<string> { "content-type" };

        private string key;
        private string secret;

        public string AppKey
        {
            get => key;
            set => key = value;
        }

        public string AppSecret
        {
            get => secret;
            set => secret = value;
        }

        public string Key
        {
            get => key;
            set => key = value;
        }

        public string Secret
        {
            get => secret;
            set => secret = value;
        }

        byte[] hmacsha256(byte[] keyByte, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                return hmacsha256.ComputeHash(messageBytes);
            }
        }

        // Build a CanonicalRequest from a regular request string
        string CanonicalRequest(HttpRequest r, List<string> signedHeaders)
        {
            string hexencode;
            if (r.Headers.ContainsKey(HeaderContentSha256))
            {
                hexencode = r.Headers[HeaderContentSha256];
            }
            else
            {
                var data = Encoding.UTF8.GetBytes(r.Body);
                hexencode = HexEncodeSHA256Hash(data);
            }

            return string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}", r.Method.ToString().ToUpper(), CanonicalURI(r), CanonicalQueryString(r),
                CanonicalHeaders(r, signedHeaders), string.Join(";", signedHeaders), hexencode);
        }

        string CanonicalURI(HttpRequest r)
        {
            var pattens = r.URL.Split('/');
            List<string> uri = new List<string>();
            foreach (var v in pattens)
            {
                uri.Add(UrlEncode(v));
            }

            var urlpath = string.Join("/", uri);
            if (urlpath[urlpath.Length - 1] != '/')
            {
                urlpath = urlpath + "/"; // always end with /
            }
            
            return urlpath;
        }

        string CanonicalQueryString(HttpRequest r)
        {
            List<string> keys = new List<string>();
            foreach (var pair in r.Query)
            {
                keys.Add(pair.Key);
            }

            keys.Sort(String.CompareOrdinal);
            List<string> a = new List<string>();
            foreach (var key in keys)
            {
                string k = UrlEncode(key);
                List<string> values = r.Query[key];
                values.Sort(String.CompareOrdinal);
                foreach (var value in values)
                {
                    string kv = k + "=" + UrlEncode(value);
                    a.Add(kv);
                }
            }

            return string.Join("&", a);
        }

        string CanonicalHeaders(HttpRequest request, List<string> signedHeaders)
        {
            StringBuilder buffer = new StringBuilder();


            foreach (string key in signedHeaders)
            {
                buffer.Append(key).Append(":");
                if (request.Headers.ContainsKey(key))
                {
                    var value = request.Headers[key];
                    if (value != null)
                    {
                        buffer.Append(value.Trim());
                    }
                }
                buffer.Append('\n');
            }

            return buffer.ToString();
        }

        List<string> SignedHeaders(HttpRequest r)
        {
            List<string> a = new List<string>();
            foreach (var keyValue in r.Headers)
            {
                string keyLower = keyValue.Key.ToLower();
                a.Add(keyLower);
            }
            a.Sort(String.CompareOrdinal);
            return a;
        }

        static char GetHexValue(int i)
        {
            if (i < 10)
            {
                return (char)(i + '0');
            }

            return (char)(i - 10 + 'a');
        }

        public static string toHexString(byte[] value)
        {
            int num = value.Length * 2;
            char[] array = new char[num];
            int num2 = 0;
            for (int i = 0; i < num; i += 2)
            {
                byte b = value[num2++];
                array[i] = GetHexValue(b / 16);
                array[i + 1] = GetHexValue(b % 16);
            }

            return new string(array, 0, num);
        }

        // Create a "String to Sign".
        string StringToSign(string canonicalRequest, DateTime t)
        {
            SHA256 sha256 = new SHA256Managed();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalRequest));
            sha256.Clear();
            return string.Format("{0}\n{1}\n{2}", Algorithm, t.ToUniversalTime().ToString(BasicDateFormat),
                toHexString(bytes));
        }


        // Create the HWS Signature.
        string SignStringToSign(string stringToSign, byte[] signingKey)
        {
            byte[] hm = hmacsha256(signingKey, stringToSign);
            return toHexString(hm);
        }

        // HexEncodeSHA256Hash returns hexcode of sha256
        public static string HexEncodeSHA256Hash(byte[] body)
        {
            SHA256 sha256 = new SHA256Managed();
            var bytes = sha256.ComputeHash(body);
            sha256.Clear();
            return toHexString(bytes);
        }

        public static string HexEncodeSHA256HashFile(string fname)
        {
            SHA256 sha256 = new SHA256Managed();
            using (var fs = new FileStream(fname, FileMode.Open))
            {
                var bytes = sha256.ComputeHash(fs);
                sha256.Clear();
                return toHexString(bytes);
            }
        }

        // Get the finalized value for the "Authorization" header. The signature parameter is the output from SignStringToSign
        string AuthHeaderValue(string signature, List<string> signedHeaders)
        {
            return string.Format("{0} Access={1}, SignedHeaders={2}, Signature={3}", Algorithm, key,
                string.Join(";", signedHeaders), signature);
        }
        
        // SignRequest set Authorization header
        public RestRequest Sign(HttpRequest r)
        {
            if (r.Method != Method.Post && r.Method != Method.Patch && r.Method != Method.Put)
            {
                r.Body = "";
            }
            
            DateTime t;
            if (!r.Headers.ContainsKey(HeaderXDate))
            {
                t = DateTime.Now;
                r.Headers.Add(HeaderXDate, t.ToUniversalTime().ToString(BasicDateFormat));
            }
            else
            {
                var time = r.Headers[HeaderXDate];
                t = DateTime.ParseExact(time, BasicDateFormat, CultureInfo.CurrentCulture);
            }

            var queryString = CanonicalQueryString(r);
            if (queryString != "")
            {
                queryString = "?" + queryString;
            }
            
            RestRequest req = new RestRequest(r.Host + r.URL);

            var uri = new Uri(r.Host);
            var uriHost = uri.Host;
            r.Headers.Add("Content-Type", "application/json");
            var signedHeaders = SignedHeaders(r);
            var canonicalRequest = CanonicalRequest(r, signedHeaders);

            var info = t.ToUniversalTime().ToString(DerivedDateFormat) + "/" + r.Region + "/" +
                       Constant.AuthServiceName;

            var stringToSign = StringToSignDerived(canonicalRequest, info, t);
            var derivationKey = GetDerivationKey(key, secret, info);
            var signature = SignStringToSign(stringToSign, Encoding.UTF8.GetBytes(derivationKey));
            var authValue = DerivationAuthHeaderValue(signature, key, info, signedHeaders);
            r.Headers.Add(HeaderAuthorization, authValue);
            req.Method = r.Method;
            req =  req.AddHeaders(r.Headers);
            return req.AddBody(r.Body);
        }

        private static string DerivationAuthHeaderValue(string signature, string accessKey, string info,
            List<string> signedHeaders)
        {
            return string.Format("{0} Credential={1}/{2}, SignedHeaders={3}, Signature={4}", DerivationAlgorithm, accessKey, info,
                string.Join(";", signedHeaders), signature);
        }
        

        private static string GetDerivationKey(string accessKey, string secretKey, string info)
        {
            var deriveKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(secretKey), 32,
                Encoding.UTF8.GetBytes(accessKey), Encoding.UTF8.GetBytes(info));
            return toHexString(deriveKey);
        }

        private static string StringToSignDerived(string canonicalRequest, string info, DateTime t)
        {
            var data = Encoding.UTF8.GetBytes(canonicalRequest);
            var hexencode = HexEncodeSHA256Hash(data);
            return string.Format("{0}\n{1}\n{2}\n{3}", DerivationAlgorithm, t.ToUniversalTime().ToString(shortTimeFormat), info, hexencode);
        }

        private static char IntToHex(int n)
        {
            if (n <= 9)
                return (char)(n + (int)'0');
            else
                return (char)(n - 10 + (int)'A');
        }

        private static bool IsUrlSafeChar(char urlChar)
        {
            if (Char.IsLetterOrDigit(urlChar))
            {
                return true;
            }

            if (urlChar == '-' || urlChar == '_' || urlChar == '.' || urlChar == '~')
            {
                return true;
            }
            return false;
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            int cUnsafe = 0;

            // count them first
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];

                if (!IsUrlSafeChar(ch))
                    cUnsafe++;
            }
            
            if (cUnsafe == 0)
            {
                // DevDiv 912606: respect "offset" and "count"
                if (0 == offset && bytes.Length == count)
                {
                    return bytes;
                }
                else
                {
                    var subarray = new byte[count];
                    Buffer.BlockCopy(bytes, offset, subarray, 0, count);
                    return subarray;
                }
            }

            var byteSize = count + cUnsafe * 2;
            byte[] exBytes = new byte[byteSize];
            int pos = 0;

            for (int i = 0; i < count; i++)
            {
                var index = offset + i;
                byte b = bytes[index];
                char ch = (char)b;

                if (IsUrlSafeChar(ch))
                {
                    exBytes[pos++] = b;
                }
                else
                {
                    exBytes[pos++] = (byte)'%';
                    exBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    exBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
            }

            return exBytes;
        }

        private static string UrlEncode(string value)
        {
            if (value == null)
                return null;

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Encoding.UTF8.GetString(UrlEncode(bytes, 0, bytes.Length));
        }
    }
}