using AgileConfig.Client;
using AgileConfig.Client.RegisterCenter;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        public static void AddAgileConfig(this IServiceCollection services)
        {
            // Register IConfigClient as a lazy singleton so that the logger is resolved from
            // the fully-built DI container (which already includes every logging provider the
            // host has registered, e.g. NLog / Serilog) instead of from a temporary container
            // built at service-registration time.
            services.AddSingleton<IConfigClient>(sp =>
            {
                var instance = ConfigClient.Instance;
                var loggerFactory = sp.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    instance.Logger = loggerFactory.CreateLogger<ConfigClient>();
                }
                return instance;
            });
            services.AddHostedService<ClientShutdownHostService>();
            if (ConfigClient.Instance.Options.RegisterInfo != null)
            {
                AddAgileRegisterCenterDiscovery(services);
            }
        }

        public static void AddAgileRegisterCenterDiscovery(this IServiceCollection services)
        {
            services.AddSingleton<IRegisterService, RegisterService>();
            services.AddSingleton<IDiscoveryService, DiscoveryService>();
            services.AddHostedService<RegisterHostedService>();
        }
    }
}
