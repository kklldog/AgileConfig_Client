using AgileConfig.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        public static void AddAgileConfig(this IServiceCollection services)
        {
            ConfigClient.Instance.Logger = GetLogger(services);
            services.AddSingleton<IConfigClient>(sp =>
            {
                ConfigClient.Instance.Logger = GetLogger(services);
                return ConfigClient.Instance;
            });
        }

        private static ILogger GetLogger(IServiceCollection services)
        {
            var logger = services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger<ConfigClient>();
            return logger;
        }
    }
}
