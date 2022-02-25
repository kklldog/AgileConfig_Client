using AgileConfig.Client.RegisterCenter.Heartbeats;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter
{
    public class RegiserHostedService : IHostedService
    {
        private IRegisterService _registerService;
        private HeartbeatService _heartbeatService;
        private ILoggerFactory _loggerFactory;
        private IDiscoveryService _discoveryService;
        public RegiserHostedService(IRegisterService registerServicer,
            IDiscoveryService discoveryService,
            ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _registerService = registerServicer;
            _discoveryService = discoveryService;
            _heartbeatService = new HeartbeatService(ConfigClient.Instance, _loggerFactory);
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var logger = _loggerFactory.CreateLogger<RegiserHostedService>();
            logger.LogInformation("RegiserHostedService starting ...");
            logger.LogInformation("try to register serviceinfo to server .");

            await _registerService.RegisterAsync();
            //客户端心跳
            _heartbeatService.Start(() =>
            {
                return _registerService.UniqueId;
            },
            (str) =>
            {
                if (string.IsNullOrEmpty(str))
                {
                    return;
                }

                var result = JsonConvert.DeserializeObject<HeartbeatResult>(str);
                if (result != null)
                {
                    if (!result.DataVersion.Equals(_discoveryService.DataVersion, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        //如果服务端跟客户端的版本不一样直接刷新
                        _ = _discoveryService.RefreshAsync();
                    }
                }
            }
            );
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var logger = _loggerFactory.CreateLogger<RegiserHostedService>();

            logger.LogInformation("RegiserHostedService stoping ...");
            logger.LogInformation("try to unregister serviceinfo to server .");

            await _registerService.UnRegisterAsync();
        }

        class HeartbeatResult
        {
            public bool Success { get; set; }

            public string DataVersion { get; set; }
        }
    }
}
