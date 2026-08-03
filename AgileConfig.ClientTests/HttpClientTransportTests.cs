using AgileConfig.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Client.Tests
{
    [TestClass]
    public class HttpClientTransportTests
    {
        [TestMethod]
        public async Task SendAsync_Get_ReturnsContentStatusAndHeaders()
        {
            HttpMethod actualMethod = null;
            Uri actualUri = null;
            string actualAppId = null;
            var handler = new StubHttpMessageHandler((request, _) =>
            {
                actualMethod = request.Method;
                actualUri = request.RequestUri;
                actualAppId = request.Headers.GetValues("appid").Single();

                var response = new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent("response-content", Encoding.UTF8, "text/plain")
                };
                response.Headers.TryAddWithoutValidation("X-Publish-Timeline-Id", "timeline-1");
                return Task.FromResult(response);
            });

            using (handler)
            using (var httpClient = CreateHttpClient(handler))
            {
                var transport = new HttpClientTransport(httpClient);
                var result = await transport.SendAsync(
                    HttpMethod.Get,
                    "http://localhost/api/config",
                    new Dictionary<string, string> { { "appid", "app-1" } },
                    null,
                    1000,
                    null,
                    CancellationToken.None);

                Assert.AreEqual(HttpMethod.Get, actualMethod);
                Assert.AreEqual("http://localhost/api/config", actualUri.ToString());
                Assert.AreEqual("app-1", actualAppId);
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
                Assert.AreEqual("response-content", result.Content);
                Assert.AreEqual("timeline-1", result.GetHeader("x-publish-timeline-id"));
                Assert.AreEqual("text/plain; charset=utf-8", result.GetHeader("Content-Type"));
            }
        }

        [DataTestMethod]
        [DataRow("POST")]
        [DataRow("DELETE")]
        public async Task SendAsync_WithBody_SendsJsonContent(string methodName)
        {
            var actualBody = string.Empty;
            var actualContentType = string.Empty;
            HttpMethod actualMethod = null;
            var handler = new StubHttpMessageHandler(async (request, _) =>
            {
                actualMethod = request.Method;
                actualBody = await request.Content.ReadAsStringAsync();
                actualContentType = request.Content.Headers.ContentType.MediaType;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("ok")
                };
            });

            using (handler)
            using (var httpClient = CreateHttpClient(handler))
            {
                var transport = new HttpClientTransport(httpClient);
                await transport.SendAsync(
                    new HttpMethod(methodName),
                    "http://localhost/api/registercenter",
                    null,
                    Encoding.UTF8.GetBytes("{\"id\":1}"),
                    1000,
                    "application/json",
                    CancellationToken.None);

                Assert.AreEqual(methodName, actualMethod.Method);
                Assert.AreEqual("{\"id\":1}", actualBody);
                Assert.AreEqual("application/json", actualContentType);
            }
        }

        [TestMethod]
        public async Task SendAsync_WhenTimeoutExpires_CancelsRequest()
        {
            var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (handler)
            using (var httpClient = CreateHttpClient(handler))
            {
                var transport = new HttpClientTransport(httpClient);

                await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => transport.SendAsync(
                    HttpMethod.Get,
                    "http://localhost/slow",
                    null,
                    null,
                    50,
                    null,
                    CancellationToken.None));
            }
        }

        [TestMethod]
        public async Task SendAsync_WhenResponseIsNotSuccessful_ReturnsResponseDetails()
        {
            var handler = new StubHttpMessageHandler((_, __) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("temporarily unavailable")
                }));

            using (handler)
            using (var httpClient = CreateHttpClient(handler))
            {
                var transport = new HttpClientTransport(httpClient);
                var result = await transport.SendAsync(
                    HttpMethod.Get,
                    "http://localhost/unavailable",
                    null,
                    null,
                    1000,
                    null,
                    CancellationToken.None);

                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, result.StatusCode);
                Assert.AreEqual("temporarily unavailable", result.Content);
            }
        }

        [TestMethod]
        public async Task SendAsync_DisposesResponseAfterBufferingContent()
        {
            var content = new TrackingContent("buffered-content");
            var handler = new StubHttpMessageHandler((_, __) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = content
                }));

            using (handler)
            using (var httpClient = CreateHttpClient(handler))
            {
                var transport = new HttpClientTransport(httpClient);
                var result = await transport.SendAsync(
                    HttpMethod.Get,
                    "http://localhost/content",
                    null,
                    null,
                    1000,
                    null,
                    CancellationToken.None);

                Assert.AreEqual("buffered-content", result.Content);
                Assert.IsTrue(content.IsDisposed);
            }
        }

        private static HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

            public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
            {
                _sendAsync = sendAsync;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _sendAsync(request, cancellationToken);
            }
        }

        private sealed class TrackingContent : HttpContent
        {
            private readonly byte[] _content;

            public TrackingContent(string content)
            {
                _content = Encoding.UTF8.GetBytes(content);
            }

            public bool IsDisposed { get; private set; }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return stream.WriteAsync(_content, 0, _content.Length);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _content.Length;
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
