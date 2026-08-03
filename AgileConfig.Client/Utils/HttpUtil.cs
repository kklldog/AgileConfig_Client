using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Client.Utils
{
    internal sealed class HttpResult
    {
        private readonly Dictionary<string, string> _headers;

        public HttpResult(HttpStatusCode statusCode, string content, Dictionary<string, string> headers)
        {
            StatusCode = statusCode;
            Content = content;
            _headers = headers;
        }

        public HttpStatusCode StatusCode { get; }

        public string Content { get; }

        public string GetHeader(string name)
        {
            return _headers.TryGetValue(name, out var value) ? value : null;
        }
    }

    internal interface IHttpTransport
    {
        Task<HttpResult> SendAsync(
            HttpMethod method,
            string url,
            Dictionary<string, string> headers,
            byte[] body,
            int? timeout,
            string contentType,
            CancellationToken cancellationToken);
    }

    internal sealed class HttpClientTransport : IHttpTransport
    {
        private const int DefaultTimeoutMilliseconds = 100 * 1000;
        private static readonly HttpClient SharedHttpClient = CreateSharedHttpClient();
        private readonly HttpClient _httpClient;

        public static IHttpTransport Shared { get; } = new HttpClientTransport(SharedHttpClient);

        public HttpClientTransport(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<HttpResult> SendAsync(
            HttpMethod method,
            string url,
            Dictionary<string, string> headers,
            byte[] body,
            int? timeout,
            string contentType,
            CancellationToken cancellationToken)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            var timeoutMilliseconds = timeout ?? DefaultTimeoutMilliseconds;
            if (timeoutMilliseconds != Timeout.Infinite && timeoutMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            using (var request = new HttpRequestMessage(method, url))
            {
                if (body != null)
                {
                    request.Content = new ByteArrayContent(body);
                    if (!string.IsNullOrWhiteSpace(contentType))
                    {
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    }
                }

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            throw new InvalidOperationException($"Cannot add request header '{header.Key}'.");
                        }
                    }
                }

                using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    if (timeoutMilliseconds != Timeout.Infinite)
                    {
                        timeoutSource.CancelAfter(timeoutMilliseconds);
                    }

                    using (var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead,
                        timeoutSource.Token).ConfigureAwait(false))
                    {
                        var content = response.Content == null
                            ? string.Empty
                            : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var header in response.Headers)
                        {
                            responseHeaders[header.Key] = string.Join(",", header.Value);
                        }

                        if (response.Content != null)
                        {
                            foreach (var header in response.Content.Headers)
                            {
                                responseHeaders[header.Key] = string.Join(",", header.Value);
                            }
                        }

                        return new HttpResult(response.StatusCode, content, responseHeaders);
                    }
                }
            }
        }

        private static HttpClient CreateSharedHttpClient()
        {
            return new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }
    }

    internal static class HttpUtil
    {
        public static Task<HttpResult> GetAsync(string url, Dictionary<string, string> headers, int? timeout)
        {
            return HttpClientTransport.Shared.SendAsync(
                HttpMethod.Get,
                url,
                headers,
                null,
                timeout,
                null,
                CancellationToken.None);
        }

        public static Task<HttpResult> PostAsync(string url, Dictionary<string, string> headers, byte[] body, int? timeout, string contentType)
        {
            return HttpClientTransport.Shared.SendAsync(
                HttpMethod.Post,
                url,
                headers,
                body,
                timeout,
                contentType,
                CancellationToken.None);
        }

        public static Task<HttpResult> DeleteAsync(string url, Dictionary<string, string> headers, byte[] body, int? timeout, string contentType)
        {
            return HttpClientTransport.Shared.SendAsync(
                HttpMethod.Delete,
                url,
                headers,
                body,
                timeout,
                contentType,
                CancellationToken.None);
        }
    }
}
