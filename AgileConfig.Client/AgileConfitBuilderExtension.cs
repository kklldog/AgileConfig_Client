using AgileConfig.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.AspNetCore.Hosting
{
    public static class AgileConfitBuilderExt
    {
        public static IConfigurationBuilder AddAgileConfig(
            this IConfigurationBuilder builder,
            IConfigClient client, Action<ConfigChangedArg> e = null)
        {
            ConfigClient.Instance = client;

            if (e != null)
            {
                client.ConfigChanged += e;
            }

            if (client.Logger  == null)
            {
                using (var loggerFactory = LoggerFactory.Create(lb =>
                {
                    lb.SetMinimumLevel(LogLevel.Trace);
                    lb.AddConsole();
                }))
                {
                    var logger = loggerFactory.CreateLogger<ConfigClient>();
                    client.Logger = logger;
                    client.Logger.LogInformation("agileConfig client set a default console logger .");
                }
            }

            return builder.Add(new AgileConfigSource(client));
        }

        public static IConfigurationBuilder AddAgileConfig(
          this IConfigurationBuilder builder, Action<ConfigChangedArg> e = null)
        {
            return builder.AddAgileConfig(new ConfigClient(), e);
        }

        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, Action<ConfigChangedArg> e = null)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(e);
            });

            return builder;
        }
        public static IHostBuilder UseAgileConfig(this IHostBuilder builder, IConfigClient client, Action<ConfigChangedArg> e = null)
        {
            builder.ConfigureAppConfiguration((_, cfb) =>
            {
                cfb.AddAgileConfig(client, e);
            });

            return builder;
        }
    }
}
