using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter.Heartbeats
{
    class HeartbeatService
    {
        IConfigClient _client;
        HeartbeatChannelPicker _picker;
        ILogger _logger;
        public HeartbeatService(IConfigClient client, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HeartbeatService>();
            _client = client;
            _picker = new HeartbeatChannelPicker(client, loggerFactory);
        }

        public void Start(Func<string> getId, Action<string> callback)
        {
            Task.Factory.StartNew(async ()=> {
                while (true)
                {
                    var uniqueId = getId();
                    if (!string.IsNullOrEmpty(uniqueId))
                    {
                        var channel = _picker.Pick();
                        await channel.SendAsync(uniqueId, (str) => {
                            _logger.LogTrace($"service {uniqueId} heartbeat result : {str}");
                            callback?.Invoke(str);
                        });
                    }
                   
                    await Task.Delay(1000 * 5);
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
