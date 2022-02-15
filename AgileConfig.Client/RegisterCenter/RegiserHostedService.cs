using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter
{
    public class RegiserHostedService : IHostedService
    {
        private ILogger _logger;
        public RegiserHostedService(ILogger<RegiserHostedService> logger)
        {
            _logger = logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RegiserHostedService starting ...");
            _logger.LogInformation("try to register serviceinfo to server .");

            RegisterService.Do();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RegiserHostedService stoping ...");
            _logger.LogInformation("try to unregister serviceinfo to server .");

            return Task.CompletedTask;
        }
    }
}
