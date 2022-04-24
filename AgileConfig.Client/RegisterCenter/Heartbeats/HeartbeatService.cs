using AgileConfig.Client.MessageHandlers;
using AgileConfig.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter.Heartbeats
{
    class HeartbeatService
    {
        IConfigClient _client;
        HeartbeatChannelPicker _picker;
        ILogger _logger;
        CancellationTokenSource _cancellationTokenSource;

        private int Interval
        {
            get
            {
                if (_client.Options == null || _client.Options.RegisterInfo == null || _client.Options.RegisterInfo.Interval < 5)
                {
                    return 30;
                }

                return _client.Options.RegisterInfo.Interval;
            }
        }

        public HeartbeatService(IConfigClient client, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HeartbeatService>();
            _client = client;
            _picker = new HeartbeatChannelPicker(client, loggerFactory);
        }

        public void Start(Func<string> getId)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var uniqueId = getId();
                    if (!string.IsNullOrEmpty(uniqueId))
                    {
                        var channel = _picker.Pick();
                        await channel.SendAsync(uniqueId);
                    }

                    await Task.Delay(1000 * Interval);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
