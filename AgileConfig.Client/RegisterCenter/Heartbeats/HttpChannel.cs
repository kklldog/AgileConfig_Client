using AgileConfig.Client.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter.Heartbeats
{
    class HttpChannel : IChannel
    {
        private ConfigClientOptions _options;
        private ILogger _logger;

        public HttpChannel(ConfigClientOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }
        public async Task SendAsync(string serviceUniqueId)
        {
            var random = new RandomServers(_options.Nodes);
            var param = new
            {
                uniqueId = serviceUniqueId
            };
            var json = JsonSerializer.Serialize(param);
            var data = Encoding.UTF8.GetBytes(json);
            while (!random.IsComplete)
            {   // Try removing one random node at a time.
                var host = random.Next();
                var postUrl = host + (host.EndsWith("/") ? "" : "/") + $"api/registercenter/heartbeat";
                try
                {
                    var resp = await HttpUtil.PostAsync(postUrl, null, data, null, "application/json").ConfigureAwait(false);

                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogTrace($"HttpChannel send a heartbeat to {postUrl} success .");

                        var content = resp.Content;
                        MessageCenter.Receive(content); // Forward the server response.
                    }
                    else
                    {
                        _logger.LogTrace($"HttpChannel send a heartbeat to {postUrl} fail , status code {resp.StatusCode} .");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"HttpChannel send a heartbeat to {postUrl} error .");
                }
            }
        }
    }
}
