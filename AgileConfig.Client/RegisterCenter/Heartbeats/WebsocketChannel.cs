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

        public WebsocketChannel(IConfigClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
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
            if (Websocket.State == WebSocketState.Open)
            {
                try
                {
                    var data = Encoding.UTF8.GetBytes(id);
                    await Websocket.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
                            CancellationToken.None).ConfigureAwait(false);

                    ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[1024 * 2]);
                    var result = await Websocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);

                    using (var ms = new MemoryStream())
                    {
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            using (var reader = new StreamReader(ms, Encoding.UTF8))
                            {
                                var content = await reader.ReadToEndAsync().ConfigureAwait(false);

                                receiver?.Invoke(content);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                }
            }
        }

    }
}
