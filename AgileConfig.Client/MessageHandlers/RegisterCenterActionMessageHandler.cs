using AgileConfig.Protocol;
using System.Text.Json;

namespace AgileConfig.Client.MessageHandlers
{
    class RegisterCenterActionMessageHandler
    {
        public static bool Hit(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            var action = JsonSerializer.Deserialize<ActionMessage>(message);
            if (action == null)
            {
                return false;
            }

            return action.Module == ActionModule.RegisterCenter;
        }
     
    }
}
