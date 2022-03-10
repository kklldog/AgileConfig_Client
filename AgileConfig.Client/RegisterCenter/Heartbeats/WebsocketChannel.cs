using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter.Heartbeats
{
    class WebsocketChannel : IChannel
    {
        private IConfigClient _client;
        private ILogger _logger;
        private Action<string> _messageReceiver;

        public WebsocketChannel(IConfigClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
            MessageCenter.Subscribe += (str) =>
            {
                Task.Run(()=> { 
                    _messageReceiver?.Invoke(str);
                });
            };
        }

        private ClientWebSocket Websocket
        {
            get
            {
                return _client.WebSocket;
            }
        }

        public async Task SendAsync(string id, Action<string> receiver)
        {
            _messageReceiver = receiver;
            if (Websocket.State == WebSocketState.Open)
            {
                try
                {
                    var msg = $"S:{id}";
                    var data = Encoding.UTF8.GetBytes(msg);
                    await Websocket.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
                            CancellationToken.None).ConfigureAwait(false);
                    _logger.LogTrace($"WebsocketChannel send a heartbeat to server success .");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"WebsocketChannel send a heartbeat to server error .");
                }
            }
        }

    }
}
