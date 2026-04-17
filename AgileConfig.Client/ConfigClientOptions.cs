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

        public int ReconnectInterval { get; set; } = 5;

        public bool CacheEnabled { get; set; } = true;

        public string CacheDirectory { get; set; }
        /// <summary>
        /// Encrypt cached configuration files.
        /// </summary>
        /// <value></value>
        public bool ConfigCacheEncrypt { get; set; } = false;

        public ServiceRegisterInfo RegisterInfo { get; set; }

        public ILogger Logger { get; set; }

        [Obsolete("This Action will be obsolete, please use ReLaoded Action instead of.")]
        public Action<ConfigChangedArg> ConfigChanged;

        /// <summary>
        /// Raised after the newest configuration has been loaded locally.
        /// </summary>
        public Action<ConfigReloadedArgs> ReLoaded;

        /// <summary>
        /// Ensure the current directory contains the json configuration file and
        /// return the exact directory path when it does.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private static string EnsureCurrentDirectory(string json)
        {
            var rootDir = Directory.GetCurrentDirectory();
            var jsonFile = Path.Combine(rootDir, json);
            if (!File.Exists(jsonFile))
            {
                rootDir = AppDomain.CurrentDomain.BaseDirectory;
                jsonFile = Path.Combine(rootDir, json);
                if (!File.Exists(jsonFile))
                {
                    throw new FileNotFoundException("Can not find app config file .", jsonFile);
                }
            }

            return rootDir;
        }


        public static ConfigClientOptions FromLocalAppsettingsOrEmpty(string json = "appsettings.json")
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            var rootDir = EnsureCurrentDirectory(json);

            var localconfig = new ConfigurationBuilder()
                             .SetBasePath(rootDir)
                             .AddJsonFile(json)
                             .AddEnvironmentVariables()
                             .AddCommandLine(Environment.GetCommandLineArgs())
                             .Build();

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

            var rootDir = EnsureCurrentDirectory(json);

            var localconfig = new ConfigurationBuilder()
                             .SetBasePath(rootDir)
                             .AddJsonFile(json)
                             .AddEnvironmentVariables()
                             .AddCommandLine(Environment.GetCommandLineArgs())
                             .Build();

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
            var cacheEncrypt = config.GetValue("AgileConfig:cache:config_encrypt", false);
            var cacheEnabled = config.GetValue("AgileConfig:cache:enabled", true);
            var reconnectInterval = config.GetValue("AgileConfig:reconnectInterval", 5);

            options.Name = name;
            options.Tag = tag;
            options.AppId = appId;
            options.Secret = secret;
            options.Nodes = serverNodes;
            options.ENV = string.IsNullOrEmpty(env) ? "" : env.ToUpper();
            options.CacheEnabled = cacheEnabled;
            options.CacheDirectory = cacheDir;
            options.ConfigCacheEncrypt = cacheEncrypt;
            options.ReconnectInterval = reconnectInterval;

            if (int.TryParse(timeout, out int iTimeout))
            {
                options.HttpTimeout = iTimeout;
            }

            //read service info
            var serviceRegisterConf = config.GetSection("AgileConfig:serviceRegister");
            if (serviceRegisterConf.Exists())
            {
                options.RegisterInfo = new ServiceRegisterInfo();
            }
            else
            {
                return options;
            }

            var serviceId = config["AgileConfig:serviceRegister:serviceId"];
            if (string.IsNullOrWhiteSpace(serviceId))
            {
                // Try recovering the ID from the local cache if it is missing in configuration.
                serviceId = TryGetIdFromLocal(cacheDir, appId);
                DefaultConsoleLogger.LogInformation("because serviceId is empty in the configuration , try to read serviceId from local cache file , the id = " + serviceId);
            }
            if (string.IsNullOrWhiteSpace(serviceId))
            {
                // Generate a GUID when restoration fails.
                serviceId = Guid.NewGuid().ToString("N");
                // Persist it locally so the same ID is reused after restarts.
                WriteIdToLocal(cacheDir, appId, serviceId);
                DefaultConsoleLogger.LogInformation("generate a serviceId = " + serviceId);
            }

            var serviceName = config["AgileConfig:serviceRegister:serviceName"];
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException("serviceRegister:serviceName");
            }

            var ip = config["AgileConfig:serviceRegister:ip"];
            var port = config["AgileConfig:serviceRegister:port"];
            var alarmUrl = config["AgileConfig:serviceRegister:alarmUrl"];
            var checkUrl = config["AgileConfig:serviceRegister:heartbeat:url"];
            var mode = config["AgileConfig:serviceRegister:heartbeat:mode"];
            var heartbeatInverval = config.GetValue("AgileConfig:serviceRegister:heartbeat:interval", 30);
            var reregisterInterval = config.GetValue("AgileConfig:serviceRegister:reregisterInterval", 5);
            var metaData = new List<string>();
            config.GetSection("AgileConfig:serviceRegister:metaData").Bind(metaData);
            options.RegisterInfo.ServiceId = serviceId;
            options.RegisterInfo.ServiceName = serviceName;
            options.RegisterInfo.Ip = ip;
            options.RegisterInfo.CheckUrl = checkUrl;
            options.RegisterInfo.AlarmUrl = alarmUrl;
            options.RegisterInfo.ReregisterInterval = reregisterInterval;
            options.RegisterInfo.Interval = heartbeatInverval;

            if (string.IsNullOrWhiteSpace(mode))
            {
                mode = "client";
            }
            options.RegisterInfo.HeartBeatMode = mode;
            options.RegisterInfo.MetaData = metaData;
            if (int.TryParse(port, out int iport))
            {
                options.RegisterInfo.Port = iport;
            }

            return options;
        }

        private static string TryGetIdFromLocal(string cacheDir, string appId)
        {
            string idFileName = Path.Combine(cacheDir, $"{appId}.agileconfig.client.serviceid");

            try
            {
                var lines = File.ReadAllLines(idFileName);

                if (lines?.Length > 0)
                {
                    return lines[0];
                }
            }
            catch 
            {
            }

            return "";
        }

        private static void WriteIdToLocal(string cacheDir, string appId, string serviceId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(cacheDir) && !Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                string idFileName = Path.Combine(cacheDir, $"{appId}.agileconfig.client.serviceid");

                File.WriteAllText(idFileName, serviceId);
            }
            catch (Exception e)
            {
                DefaultConsoleLogger.LogError(e, "Can not save serviceId to local");
            }
        }

        private static readonly object _consoleLoggerLock = new object();
        private static ILogger _consoleLogger;
        // Keep the factory alive so the logger it produced remains functional.
        private static ILoggerFactory _consoleLoggerFactory;
        public static ILogger DefaultConsoleLogger
        {
            get
            {
                if (_consoleLogger == null)
                {
                    lock (_consoleLoggerLock)
                    {
                        if (_consoleLogger == null)
                        {
                            _consoleLoggerFactory = LoggerFactory.Create(lb =>
                            {
                                lb.SetMinimumLevel(LogLevel.Trace);
                                lb.AddConsole();
                            });
                            _consoleLogger = _consoleLoggerFactory.CreateLogger<ConfigClient>();
                        }
                    }
                }

                return _consoleLogger;
            }
        }
    }
}
