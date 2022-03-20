using AgileConfig.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AgileConfig.Client.MessageHandlers
{
    /// <summary>
    /// 服务端回复配置client的action消息的处理类
    /// </summary>
    class ConfigCenterActionMessageHandler
    {
        public static bool Hit(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            var action = JsonConvert.DeserializeObject<WebsocketAction>(message);
            if (action == null)
            {
                return false;
            }

            return action.Module == ActionModule.ConfigCenter;
        }

        public static async Task Handle(string message, ConfigClient client)
        {
            var action = JsonConvert.DeserializeObject<WebsocketAction>(message);
            if (action != null)
            {
                await client.TryHandleAction(action);
            }
        }
    }
}
