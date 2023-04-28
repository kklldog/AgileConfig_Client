using AgileConfig.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Extensions.Hosting
{
    public static class HostBuilderExtension
    {
        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, Action<ConfigReloadedArgs> evt = null)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(evt);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }

        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, string appsettingsFileName, Action<ConfigReloadedArgs> evt = null)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                if (String.IsNullOrEmpty(appsettingsFileName))
                {
                    cfb.AddAgileConfig(evt);
                }
                else
                {
                    cfb.AddAgileConfig(new ConfigClient(appsettingsFileName), evt);
                }
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }

        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, IConfigClient client, Action<ConfigReloadedArgs> e = null)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(client, e);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }

        [Obsolete]
        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, Action<ConfigChangedArg> e)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(e);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }

        [Obsolete]
        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, string appsettingsFileName, Action<ConfigChangedArg> e)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                if (String.IsNullOrEmpty(appsettingsFileName))
                {
                    cfb.AddAgileConfig(e);
                }
                else
                {
                    cfb.AddAgileConfig(new ConfigClient(appsettingsFileName), e);
                }
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }

        [Obsolete]
        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, IConfigClient client, Action<ConfigChangedArg> e)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(client, e);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }

        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, ConfigClientOptions options)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(options);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }

        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, Func<IConfigurationBuilder, ConfigClientOptions> getOp)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(getOp);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }

        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, Action<ConfigClientOptions> setOp)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(setOp);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAgileConfig();
            });

            return builder;
        }
    }
}
