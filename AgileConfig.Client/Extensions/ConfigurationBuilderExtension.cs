using AgileConfig.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Hosting
{
    public static class ConfigurationBuilderExtension
    {
        public static IConfigurationBuilder AddAgileConfig(
           this IConfigurationBuilder builder,
           IConfigClient client, Action<ConfigReloadedArgs> evt = null)
        {
            if (evt != null)
            {
                client.ReLoaded += evt;
            }

            return builder.Add(new AgileConfigSource(client));
        }

        public static IConfigurationBuilder AddAgileConfig(
          this IConfigurationBuilder builder, Action<ConfigReloadedArgs> evt = null)
        {
            return builder.AddAgileConfig(new ConfigClient(), evt);
        }

        [Obsolete("ConfigChanged event will be obsolete.")]
        public static IConfigurationBuilder AddAgileConfig(
            this IConfigurationBuilder builder,
            IConfigClient client, Action<ConfigChangedArg> e)
        {
            if (e != null)
            {
                client.ConfigChanged += e;
            }

            return builder.Add(new AgileConfigSource(client));
        }

        [Obsolete("ConfigChanged event will be obsolete.")]
        public static IConfigurationBuilder AddAgileConfig(
          this IConfigurationBuilder builder, Action<ConfigChangedArg> e)
        {
            return builder.AddAgileConfig(new ConfigClient(), e);
        }

        public static IConfigurationBuilder AddAgileConfig(
          this IConfigurationBuilder builder, ConfigClientOptions options)
        {
            return builder.AddAgileConfig(new ConfigClient(options), evt: null);
        }

        public static IConfigurationBuilder AddAgileConfig(
          this IConfigurationBuilder builder, Func<IConfigurationBuilder, ConfigClientOptions> getOp)
        {
            ConfigClientOptions defaultOp = null;
            if (getOp != null)
            {
                defaultOp = getOp(builder);
            }

            return builder.AddAgileConfig(new ConfigClient(defaultOp), evt: null);
        }

        /// <summary>
        /// 先尝试从本地的配置文件读取参数，然后合并 setOp Action的赋值
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="setOp"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddAgileConfig(
         this IConfigurationBuilder builder, Action<ConfigClientOptions> setOp)
        {
            var defaultOp = ConfigClientOptions.FromLocalAppsettingsOrEmpty();
            if (defaultOp == null)
            {
                defaultOp = new ConfigClientOptions();
            }

            if (setOp != null)
            {
                setOp(defaultOp);
            }

            return builder.AddAgileConfig(defaultOp);
        }
    }
}
