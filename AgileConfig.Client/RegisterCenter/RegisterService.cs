using Microsoft.Extensions.Logging;

namespace AgileConfig.Client.RegisterCenter
{
    public class RegisterService
    {
        public static void Do()
        {
            var regInfo = ConfigClient.Instance?.Options?.RegisterInfo;

            if (regInfo == null)
            {
                var logger = ConfigClient.Instance?.Options?.Logger;

                if (logger != null)
                {
                    logger.LogInformation("NO ServiceRegisterInfo STOP register .");
                }

                return;
            }
        }
    }
}
