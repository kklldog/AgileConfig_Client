using AgileConfig.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Extensions.Hosting
{
    public static class HostBuilderExtension
    {
        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, Action<ConfigChangedArg> e = null)
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
        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, IConfigClient client, Action<ConfigChangedArg> e = null)
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
