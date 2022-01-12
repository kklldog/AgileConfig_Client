using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace AgileConfig.Client
{
    class HttpUtil
    {
        public static HttpWebResponse Get(string url, Dictionary<string, string> headers, int? timeout) 
        {
            var request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }

            if (headers != null)
            {
                foreach (var keyValuePair in headers)
                {
                    request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            var response = request.GetResponse() as HttpWebResponse;

            return response;
        }

        public static string GetResponseContent(HttpWebResponse response)
        {
            using (var responseStream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }
    }
}
