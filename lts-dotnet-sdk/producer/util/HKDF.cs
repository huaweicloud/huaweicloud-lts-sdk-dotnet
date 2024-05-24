using HuaweiCloud.SDK.LTS.HTTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HuaweiCloud.SDK.LTS
{
    class HKDF
    {
        public static byte[] DeriveKey(HashAlgorithmName hashAlgorithmName, byte[] ikm, int outputLength, byte[] salt = null, byte[] info = null)
        {
            int EXPAND_CEIL = (int)Math.Ceiling((double)outputLength / (double)HashLength(hashAlgorithmName));
            byte[] extractKey = Extract(ikm, salt, hashAlgorithmName);
            byte[] derSecretKey = Expand(extractKey, info, hashAlgorithmName,
                outputLength, EXPAND_CEIL);
            return derSecretKey;
        }

        private static byte[] Extract(byte[] ikm, byte[] salt, HashAlgorithmName hashAlgorithmName)
        {
            if (salt == null || salt.Length == 0)
            {
                salt = new byte[HKDF.HashLength(hashAlgorithmName)];
            }

            HMAC mac = GetMAC(hashAlgorithmName, salt);
            return mac.ComputeHash(ikm);
        }

        private static byte[] Expand(byte[] pkr, byte[] info, HashAlgorithmName hashAlgorithmName, int okmlength, int ceil)
        {
            HMAC mac = GetMAC(hashAlgorithmName, pkr);
            byte[] rawResult;
            if (ceil == 1)
            {
                rawResult = ExpandFirst(info, mac);
            } 
            else
            {
                rawResult = new byte[0];
                byte[] temp = new byte[0];
                for(int i = 1; i<= ceil; i++)
                {
                    temp = ExpandOnce(info, mac, temp, i);
                    MemoryStream combineBytes = new MemoryStream();
                    combineBytes.Write(rawResult, 0, rawResult.Length);
                    combineBytes.Write(temp, 0, temp.Length);
                    rawResult = combineBytes.ToArray();

                }
            }

            if (okmlength == rawResult.Length)
            {
                return rawResult;
            }
            else if (okmlength < rawResult.Length)
            {
                byte[] copy = new byte[okmlength];
                Array.Copy(rawResult, copy, okmlength);
                return copy;
            }
            else
            {
                return null;
            }
        }

        private static byte[] ExpandFirst(byte[] info, HMAC mac)
        {
            byte[] result = new byte[info.Length + 1];
            Array.Copy(info, 0, result, 0, info.Length);
            result[info.Length] = 0x01;
            return mac.ComputeHash(result);
        }

        private static byte[] ExpandOnce(byte[] info, HMAC mac, byte[] preTemp, int i)
        {
            MemoryStream memory = new MemoryStream();
            memory.Write(preTemp, 0, preTemp.Length);
            memory.Write(info, 0, info.Length);
            memory.WriteByte((byte)i);
            return mac.ComputeHash(memory.ToArray());
        } 

        private static int HashLength(HashAlgorithmName hashAlgorithmName)
        {
            if (hashAlgorithmName == HashAlgorithmName.SHA1)
                return 20;
            if (hashAlgorithmName == HashAlgorithmName.SHA256)
                return 32;
            if (hashAlgorithmName == HashAlgorithmName.SHA384)
                return 48;
            if (hashAlgorithmName == HashAlgorithmName.SHA512)
                return 64;
            if (hashAlgorithmName == HashAlgorithmName.MD5)
                return 16;
            throw new ArgumentOutOfRangeException(nameof(hashAlgorithmName));
        }

        private static HMAC GetMAC(HashAlgorithmName hashAlgorithmName, byte[] salt)
        {
            if (hashAlgorithmName == HashAlgorithmName.SHA1)
                return new HMACSHA1(salt);
            if (hashAlgorithmName == HashAlgorithmName.SHA256)
                return new HMACSHA256(salt);
            if (hashAlgorithmName == HashAlgorithmName.SHA384)
                return new HMACSHA384(salt);
            if (hashAlgorithmName == HashAlgorithmName.SHA512)
                return new HMACSHA512(salt);
            if (hashAlgorithmName == HashAlgorithmName.MD5)
                return new HMACMD5(salt);
            throw new NotSupportedException(nameof(hashAlgorithmName));
        }
    }
}
