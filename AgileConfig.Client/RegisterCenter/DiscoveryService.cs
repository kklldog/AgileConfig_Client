using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter
{
    public interface IDiscoveryService
    {
        string DataVersion { get; }
        List<ServiceInfo> OfflineServices { get; }
        List<ServiceInfo> OnlineServices { get; }
        List<ServiceInfo> Services { get; }

        Task RefreshAsync();
    }

    public class DiscoveryService : IDiscoveryService
    {
        private List<ServiceInfo> _services;
        private IConfigClient _configClient;
        private ILogger _logger;

        public DiscoveryService(IConfigClient client, ILoggerFactory loggerFactory)
        {
            _services = new List<ServiceInfo>();
            _configClient = client;
            _logger = loggerFactory.CreateLogger<DiscoveryService>();
            RefreshAsync().GetAwaiter().GetResult();
        }

        public string DataVersion { get; private set; }


        public List<ServiceInfo> Services
        {
            get
            {
                return _services;
            }
        }

        public List<ServiceInfo> OnlineServices
        {
            get
            {
                return _services.Where(x => x.Status == ServiceStatus.Online).ToList();
            }
        }

        public List<ServiceInfo> OfflineServices
        {
            get
            {
                return _services.Where(x => x.Status == ServiceStatus.Offline).ToList();
            }
        }

        public async Task RefreshAsync()
        {
            var random = new RandomServers(_configClient.Options.Nodes);
            while (!random.IsComplete)
            {   //随机一个节点尝试移除
                var host = random.Next();
                var getUrl = host + (host.EndsWith("/") ? "" : "/") + $"api/registercenter/services";
                try
                {
                    var resp = await HttpUtil.GetAsync(getUrl, null, null);

                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var content = await HttpUtil.GetResponseContentAsync(resp);
                        if (!string.IsNullOrEmpty(content))
                        {
                            var result = JsonConvert.DeserializeObject<ServiceListResult>(content);
                            if (result != null)
                            {
                                this.DataVersion = result.DataVersion;
                                this._services = result.Data;
                            }
                        }
                    }
                    else
                    {
                        _logger.LogTrace($"DiscoveryService refresh all services fail , url {getUrl} , status code {resp.StatusCode} .");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DiscoveryService refresh all services error .");
                }
            }
        }

        class ServiceListResult
        {
            public string DataVersion { get; set; }

            public List<ServiceInfo> Data { get; set; }
        }
    }
}
