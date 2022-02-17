using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter
{
    public class RegiserHostedService : IHostedService
    {
        private IRegisterService _registerService;
        private ILoggerFactory _loggerFactory;
        public RegiserHostedService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _registerService = new RegisterService(ConfigClient.Instance.Options, _loggerFactory.CreateLogger<RegisterService>());
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var logger = _loggerFactory.CreateLogger<RegiserHostedService>();
            logger.LogInformation("RegiserHostedService starting ...");
            logger.LogInformation("try to register serviceinfo to server .");

            await _registerService.RegisterAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var logger = _loggerFactory.CreateLogger<RegiserHostedService>();

            logger.LogInformation("RegiserHostedService stoping ...");
            logger.LogInformation("try to unregister serviceinfo to server .");

            await _registerService.UnRegisterAsync();
        }
    }
}
