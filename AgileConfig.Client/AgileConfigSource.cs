using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Agile.Config.Client
{
    public static class AgileConfitBuilderExt
    {
        public static IConfigurationBuilder AddAgileConfig(
            this IConfigurationBuilder builder,
            IConfigClient client, Action<ConfigChangedArg> e = null)
        {
            if (e != null)
                client.ConfigChanged += e;
            ConfigClient.Instance = client;
            return builder.Add(new AgileConfigSource(client));
        }

        public static IConfigurationBuilder AddAgileConfig(
          this IConfigurationBuilder builder, Action<ConfigChangedArg> e = null)
        {
            return builder.AddAgileConfig(new ConfigClient(), e);
        }
    }

    public class AgileConfigSource : IConfigurationSource
    {
        protected IConfigClient ConfigClient { get; }

        public AgileConfigSource(IConfigClient client)
        {
            ConfigClient = client;
        }
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AgileConfigProvider(ConfigClient);
        }
    }
}
