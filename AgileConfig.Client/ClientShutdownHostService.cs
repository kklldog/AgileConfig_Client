using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Client
{
    class ClientShutdownHostService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                // close websocket
                await ConfigClient.Instance?.DisconnectAsync();
            }
            catch
            {
            }
        }
    }
}
