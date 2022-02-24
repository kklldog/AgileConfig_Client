using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
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
        public async Task SendAsync(string serviceUniqueId, Action<string> receiver)
        {
            var random = new RandomServers(_options.Nodes);
            while (!random.IsComplete)
            {   //随机一个节点尝试移除
                var host = random.Next();
                var postUrl = host + (host.EndsWith("/") ? "" : "/") + $"api/registercenter/heartbeat/{serviceUniqueId}";
                try
                {
                    var resp = await HttpUtil.PostAsync(postUrl, null, null, null, "application/json");
                    var content = await HttpUtil.GetResponseContentAsync(resp);

                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        receiver?.Invoke(content);
                    }
                }
                catch (System.Exception ex)
                {
                }
            }
        }
    }
}
