using AgileConfig.Client.RegisterCenter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AgileConfig.Client
{
    public class ConfigClientOptions
    {
        public string AppId { get; set; }

        public string Secret { get; set; }

        public string Nodes { get; set; }

        public string Name { get; set; }

        public string Tag { get; set; }

        public string ENV { get; set; }

        public int HttpTimeout { get; set; } = 100;

        public string CacheDirectory { get; set; }

        public ServiceRegisterInfo RegisterInfo { get; set; }

        public ILogger Logger { get; set; }

        public Action<ConfigChangedArg> ConfigChanged;

        public static ConfigClientOptions FromLocalAppsettingsOrEmpty(string json = "appsettings.json")
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            var localconfig = new ConfigurationBuilder()
                             .SetBasePath(Directory.GetCurrentDirectory())
                             .AddJsonFile(json).AddEnvironmentVariables().Build();

            var configSection = localconfig.GetSection("AgileConfig");
            if (!configSection.Exists())
            {
                return null;
            }

            return FromConfiguration(localconfig);
        }

        public static ConfigClientOptions FromLocalAppsettings(string json = "appsettings.json")
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            var localconfig = new ConfigurationBuilder()
                             .SetBasePath(Directory.GetCurrentDirectory())
                             .AddJsonFile(json).AddEnvironmentVariables().Build();

            return FromConfiguration(localconfig);
        }

        public static ConfigClientOptions FromConfiguration(IConfiguration config)
        {
            var configSection = config.GetSection("AgileConfig");
            if (!configSection.Exists())
            {
                throw new Exception($"Can not find section:AgileConfig from IConfiguration instance .");
            }

            var options = new ConfigClientOptions();

            var appId = config["AgileConfig:appId"];
            var serverNodes = config["AgileConfig:nodes"];

            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }
            if (string.IsNullOrEmpty(serverNodes))
            {
                throw new ArgumentNullException(nameof(serverNodes));
            }
            var secret = config["AgileConfig:secret"];
            var name = config["AgileConfig:name"];
            var tag = config["AgileConfig:tag"];
            var env = config["AgileConfig:env"];
            var timeout = config["AgileConfig:httpTimeout"];
            var cacheDir = config["AgileConfig:cache:directory"] ?? "";

            options.Name = name;
            options.Tag = tag;
            options.AppId = appId;
            options.Secret = secret;
            options.Nodes = serverNodes;
            options.ENV = string.IsNullOrEmpty(env) ? "" : env.ToUpper();
            options.CacheDirectory = cacheDir;
            if (int.TryParse(timeout, out int iTimeout))
            {
                options.HttpTimeout = iTimeout;
            }

            return options;
        }
    }
}
