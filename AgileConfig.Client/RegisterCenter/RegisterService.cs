using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter
{
    public interface IRegisterService
    {
        Task RegisterAsync();
        Task UnRegisterAsync();
    }

    public class RegisterService : IRegisterService
    {
        private ConfigClientOptions _options;
        private ILogger _logger;
        private string _uniqueId = "";

        public RegisterService(ConfigClientOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        //是否注册成功
        public bool Registered { get; set; }

        public async Task RegisterAsync()
        {
            var regInfo = _options?.RegisterInfo;
            if (regInfo == null)
            {
                _logger?.LogInformation("NO ServiceRegisterInfo STOP register .");

                return;
            }

            //post registerinfo to server
            await TryRegisterAsync();
        }

        public async Task UnRegisterAsync()
        {
            if (!Registered || string.IsNullOrEmpty(_uniqueId))
            {
                _logger.LogInformation("no successful registerinfo so do nothing .");

                return;
            }

            var json = JsonConvert.SerializeObject(new
            {
                serviceId = _options.RegisterInfo.ServiceId,
                serviceName = _options.RegisterInfo.ServiceName
            });
            var data = Encoding.UTF8.GetBytes(json);
            var random = new RandomServers(_options.Nodes);
            while (!random.IsComplete)
            {   //随机一个节点尝试移除
                var host = random.Next();
                var postUrl = host + (host.EndsWith("/") ? "" : "/") + $"api/registercenter/{_uniqueId}";
                try
                {
                    var resp = await HttpUtil.DeleteAsync(postUrl, null, data, _options.HttpTimeout, "application/json");
                    var content = await HttpUtil.GetResponseContentAsync(resp);
                    _logger.LogInformation($"unregister service info from server:{host} then server response result:{content} status:{resp.StatusCode}");

                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Registered = false;
                        _uniqueId = JsonConvert.DeserializeObject<RegisterResult>(content).uniqueId;

                        _logger.LogInformation($"unregister service info to server {host} success , uniqueId:{_uniqueId} serviceId:{_options.RegisterInfo.ServiceId} serviceName:{_options.RegisterInfo.ServiceName}");
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    _logger?.LogError(ex, $"try to unregister service info failed . uniqueId:{_uniqueId} url:{postUrl} , serviceId:{_options.RegisterInfo.ServiceId} serviceName:{_options.RegisterInfo.ServiceName}");
                }
            }
        }

        private async Task TryRegisterAsync()
        {
            var json = JsonConvert.SerializeObject(_options.RegisterInfo);
            var data = Encoding.UTF8.GetBytes(json);

            var random = new RandomServers(_options.Nodes);
            while (!random.IsComplete)
            {   //随机一个节点尝试注册
                var host = random.Next();
                var postUrl = host + (host.EndsWith("/") ? "" : "/") + $"api/registercenter";
                try
                {
                    var resp = await HttpUtil.PostAsync(postUrl, null, data, _options.HttpTimeout, "application/json");
                    var content = await HttpUtil.GetResponseContentAsync(resp);
                    _logger.LogInformation($"register service info to server:{host} then server response result:{content} status:{resp.StatusCode}");

                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Registered = true;
                        _uniqueId = JsonConvert.DeserializeObject<RegisterResult>(content).uniqueId;

                        _logger.LogInformation($"register service info to server {host} success , uniqueId:{_uniqueId} serviceId:{_options.RegisterInfo.ServiceId} serviceName:{_options.RegisterInfo.ServiceName}");
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    _logger?.LogError(ex, $"try to register service info failed . uniqueId:{_uniqueId} url:{postUrl} , serviceId:{_options.RegisterInfo.ServiceId} serviceName:{_options.RegisterInfo.ServiceName}");
                }
            }
        }

        class RegisterResult{
            public string uniqueId { get; set; }
        }
    }
}
