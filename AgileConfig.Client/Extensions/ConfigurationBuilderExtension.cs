using AgileConfig.Client;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.AspNetCore.Hosting
{
    public static class ConfigurationBuilderExtension
    {
        public static IConfigurationBuilder AddAgileConfig(
            this IConfigurationBuilder builder,
            IConfigClient client, Action<ConfigChangedArg> e = null)
        {
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

        public static IConfigurationBuilder AddAgileConfig(
          this IConfigurationBuilder builder, ConfigClientOptions options)
        {
            return builder.AddAgileConfig(new ConfigClient(options));
        }

        public static IConfigurationBuilder AddAgileConfig(
          this IConfigurationBuilder builder, Func<IConfigurationBuilder, ConfigClientOptions> getOp)
        {
            ConfigClientOptions defaultOp = null;
            if (getOp != null)
            {
                defaultOp = getOp(builder);
            }

            return builder.AddAgileConfig(new ConfigClient(defaultOp));
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
