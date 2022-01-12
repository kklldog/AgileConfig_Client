using AgileConfig.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        public static void AddAgileConfig(this IServiceCollection services)
        {
            services.AddSingleton<IConfigClient>(sp =>
            {
                var client = ConfigClient.Instance as ConfigClient;
                client.Logger = sp.GetService<ILoggerFactory>().CreateLogger<ConfigClient>();
                return client;
            });
        }
    }
}
