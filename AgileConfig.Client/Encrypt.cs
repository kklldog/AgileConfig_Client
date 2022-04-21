using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AgileConfig.Client
{
    internal class Encrypt
    {
        public static string Md5(string txt)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(txt);
                var hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                var sb = new StringBuilder();
                for (var i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// Aes ECB PKCS7Padding(PKCS5Padding) 加密 （与Java程序对接可使用）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static string AesEncryptECB(string key, string rawData)
        {
            var bs = Encoding.UTF8.GetBytes(rawData);
            byte[] keyArray = null;
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(key));
                var rd = sha1.ComputeHash(hash);
                keyArray = rd.Take(16).ToArray();
            }
            RijndaelManaged rm = new RijndaelManaged
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            ICryptoTransform cTransform = rm.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(bs, 0, bs.Length);
            return Convert.ToBase64String(resultArray);
        }
        /// <summary>
        /// Aes ECB PKCS7Padding(PKCS5Padding) 解密 （与Java程序对接可使用）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="base64Data"></param>
        /// <returns></returns>
        public static string AesDecryptECB(string key, string base64Data)
        {
            var bs = Convert.FromBase64String(base64Data);
            byte[] keyArray = null;
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(key));
                var rd = sha1.ComputeHash(hash);
                keyArray = rd.Take(16).ToArray();
            }
            RijndaelManaged rm = new RijndaelManaged
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            ICryptoTransform cTransform = rm.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(bs, 0, bs.Length);
            var result = Encoding.UTF8.GetString(resultArray);
            return result;
        }
    }
}
