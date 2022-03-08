﻿using AgileConfig.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    }
}
