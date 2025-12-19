using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgileConfig.Client.Extensions
{
    public static class HostApplicationBuilderExtension
    {
        /// <summary>
        /// 为 Host.CreateApplicationBuilder 提供 AgileConfig 支持
        /// </summary>
        public static IHostApplicationBuilder UseAgileConfig(
            this IHostApplicationBuilder builder,
            Action<ConfigReloadedArgs> evt = null)
        {
            // 1️⃣ 注册 IConfigurationSource（等价于 ConfigureAppConfiguration）
            builder.Configuration.AddAgileConfig(evt);

            // 2️⃣ 注册 DI（等价于 ConfigureServices）
            builder.Services.AddAgileConfig();

            return builder;
        }

        public static IHostApplicationBuilder UseAgileConfig(
            this IHostApplicationBuilder builder,
            string appsettingsFileName,
            Action<ConfigReloadedArgs> evt = null)
        {
            if (string.IsNullOrEmpty(appsettingsFileName))
            {
                builder.Configuration.AddAgileConfig(evt);
            }
            else
            {
                builder.Configuration.AddAgileConfig(
                    new ConfigClient(appsettingsFileName),
                    evt);
            }

            builder.Services.AddAgileConfig();
            return builder;
        }

        public static IHostApplicationBuilder UseAgileConfig(
            this IHostApplicationBuilder builder,
            IConfigClient client,
            Action<ConfigReloadedArgs> evt = null)
        {
            builder.Configuration.AddAgileConfig(client, evt);
            builder.Services.AddAgileConfig();
            return builder;
        }

        public static IHostApplicationBuilder UseAgileConfig(
            this IHostApplicationBuilder builder,
            ConfigClientOptions options)
        {
            builder.Configuration.AddAgileConfig(options);
            builder.Services.AddAgileConfig();
            return builder;
        }

        public static IHostApplicationBuilder UseAgileConfig(
            this IHostApplicationBuilder builder,
            Func<IConfigurationBuilder, ConfigClientOptions> getOp)
        {
            builder.Configuration.AddAgileConfig(getOp);
            builder.Services.AddAgileConfig();
            return builder;
        }

        public static IHostApplicationBuilder UseAgileConfig(
            this IHostApplicationBuilder builder,
            Action<ConfigClientOptions> setOp)
        {
            builder.Configuration.AddAgileConfig(setOp);
            builder.Services.AddAgileConfig();
            return builder;
        }
    }
}