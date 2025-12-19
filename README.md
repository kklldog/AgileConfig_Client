# AgileConfig_Client

AgileConfig client implemented with .NET Standard 2.0.

![Nuget](https://github.com/kklldog/AgileConfig_Client/actions/workflows/publish2nuget.yml/badge.svg?branch=publish)
![Nuget](https://img.shields.io/nuget/v/agileconfig.client)
![Nuget](https://img.shields.io/nuget/dt/agileconfig.client?label=download)

## 项目概述 / Project Overview

AgileConfig_Client 是 AgileConfig 分布式配置中心的 .NET 客户端库，基于 .NET Standard 2.0 实现，可以在 .NET Core、.NET Framework 和 .NET 5+ 应用中使用。

**AgileConfig_Client** is the official .NET client library for the AgileConfig distributed configuration center. Built on .NET Standard 2.0, it provides seamless integration with .NET Core, .NET Framework, and .NET 5+ applications.

### 核心功能 / Key Features

#### 🔧 配置管理 / Configuration Management
- 集中式配置管理，支持动态配置更新 / Centralized configuration management with dynamic updates
- 支持多种配置读取方式（IConfiguration、IOptions、ConfigClient）/ Multiple access patterns (IConfiguration, IOptions, ConfigClient)
- 本地配置缓存，支持缓存加密 / Local configuration caching with optional encryption

#### 🔍 服务注册与发现 / Service Registration & Discovery
- 自动服务注册，支持多实例部署 / Automatic service registration for multi-instance deployments
- 灵活的心跳机制（客户端上报 / 服务端探测）/ Flexible heartbeat modes (client-side or server-side)
- 服务健康检查和告警通知 / Health checks with alarm notifications

### 应用场景 / Use Cases

- 微服务架构的配置统一管理 / Unified configuration management for microservices
- 多环境配置隔离（开发、测试、生产）/ Environment-based configuration isolation (dev, test, prod)
- 服务发现和负载均衡 / Service discovery and load balancing
- 配置热更新，无需重启应用 / Hot configuration updates without application restart

### 技术特点 / Technical Highlights

- ✅ 基于 .NET Standard 2.0，跨平台支持
- ✅ 与 ASP.NET Core 配置系统无缝集成
- ✅ 支持多节点高可用部署
- ✅ 提供丰富的示例项目（MVC、Console、WPF、Windows Service）
- ✅ Built on .NET Standard 2.0 with cross-platform support
- ✅ Seamless integration with ASP.NET Core configuration system
- ✅ High availability with multi-node support
- ✅ Rich sample projects (MVC, Console, WPF, Windows Service)

## Using the client

### Install the client

```
Install-Package AgileConfig.Client
```

☢️☢️☢️ If your application targets .NET Framework please use [AgileConfig.Client4FR](https://github.com/kklldog/AgileConfig.Client4FR), which is built specifically for Framework apps. Using the current package may lead to deadlocks and 100% CPU usage. ☢️☢️☢️

### Initialize the client

Using an ASP.NET Core MVC project as an example, configure the AgileConfig connection info in `appsettings.json`.

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",

  //agile_config
  "AgileConfig": {
    "appId": "app",
    "secret": "xxx",
    "nodes": "http://localhost:5000,http://localhost:5001",//Multiple nodes are separated by commas
    "name": "client1",
    "tag": "tag1",
    "env": "DEV",
    "httpTimeout": "100",
    "cache": {
      "directory": "agile/config"
    }
  }
}
```

#### Configuration options

|Name|Type|Description|Required|Remarks|
|--|--|--|--|--|
|appid|string|Application ID|Yes|Matches the **Application ID** configured in the server console|
|secret|string|Application secret|Yes|Matches the **Secret** configured in the server console|
|nodes|string|Server node endpoints|Yes|Separate multiple nodes with commas (`,`)|
|name|string|Custom client name|No|Helps identify the client in the AgileConfig admin console|
|tag|string|Custom client tag|No|Helps group or search for the client in the admin console|
|env|string|Target environment|No|Determines which environment configuration the client pulls. If omitted, the server returns the first environment by default|
|cache|string|Client cache configuration|No|Allows additional settings for persisting configs locally|
|cache:enabled|bool|Cache the last pulled configuration locally|No|Defaults to `true`|
|cache:directory|string|Directory where cached configuration files are stored|No|If omitted, cache files are stored in the application root|
|cache:config_encrypt|bool|Encrypt cached configuration files|No|When `true` the cache file contents are encrypted|
|httpTimeout|int|HTTP request timeout|No|Timeout in seconds for HTTP requests; defaults to 100s|

## UseAgileConfig

Call the `UseAgileConfig` extension on `Program` to add the AgileConfig configuration source.

```
 public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseAgileConfig(e => Console.WriteLine($"configs {e.Action}"))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
```

### Switch appsettings json file by environment ?

```
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

builder.Host.UseAgileConfig($"appsetting.{environment}.json");

```

## Read configuration

AgileConfig supports the standard ASP.NET Core `IConfiguration` and `IOptions` patterns. You can also read values directly from an `AgileConfigClient` instance.

```
public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _IConfiguration;
        private readonly IOptions<DbConfigOptions> _dbOptions;
        private readonly IConfigClient _IConfigClient;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IOptions<DbConfigOptions> dbOptions, IConfigClient configClient)
        {
            _logger = logger;
            _IConfiguration = configuration;
            _dbOptions = dbOptions;
            _IConfigClient = configClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Read configuration values via IConfiguration
        /// </summary>
        /// <returns></returns>
        public IActionResult ByIConfiguration()
        {
            var userId = _IConfiguration["userId"];
            var dbConn = _IConfiguration["db:connection"];

            ViewBag.userId = userId;
            ViewBag.dbConn = dbConn;

            return View();
        }

        /// <summary>
        /// Read configuration values directly from ConfigClient
        /// </summary>
        /// <returns></returns>
        public IActionResult ByInstance()
        {
            var userId = _IConfigClient["userId"];
            var dbConn = _IConfigClient["db:connection"];

            ViewBag.userId = userId;
            ViewBag.dbConn = dbConn;

            return View("ByInstance");
        }

        /// <summary>
        /// Read configuration values via the Options pattern
        /// </summary>
        /// <returns></returns>
        public IActionResult ByOptions()
        {
            var dbConn = _dbOptions.Value.connection;
            ViewBag.dbConn = dbConn;

            return View("ByOptions");
        }
    }
```

## Service registration & discovery

Add the `serviceRegister` section under `AgileConfig` in `appsettings.json`:

```
 "AgileConfig": {
    "appId": "test_app",
    "secret": "test_app",
    "nodes": "http://agileconfig_server.xbaby.xyz/",
    "name": "client123",
    "tag": "tag123",

    "serviceRegister": { //Service registration information; omit this section to disable registration (optional)
      "serviceId": "net6", //Service ID. Must be globally unique. Optional since client 1.6.12, otherwise a GUID is generated
      "serviceName": "net6 MVC sample", //Service name. Can repeat when you deploy multiple instances of the same service
      "ip": "127.0.0.1", //Service IP (optional)
      "port": 5005 //Service port (optional)
    }
  }
```

The `appId`, `secret`, and other standard configuration fields work exactly the same as with the configuration center alone.

`serviceRegister` describes the service registration payload. Removing this node disables registration entirely:

- **serviceId** – Globally unique service identifier. Optional starting with client 1.6.12; when omitted the client generates a GUID.
- **serviceName** – Friendly service name. Can repeat when multiple instances of the same service are deployed.
- **ip** – Service IP address (optional).
- **port** – Service port (optional).
- **metaData** – Optional string array for metadata such as version information.
- **alarmUrl** – Optional alert callback endpoint. If the service becomes unhealthy (for example, no heartbeat is detected for a period of time) the server POSTs a notification to this address with service details. You can implement the endpoint to send SMS, emails, and so on:

```
{
    "serviceId":"0001",
    "serviceName":"xxxx",
    "time":"2022-01-01T12:00:000",
    "status":"Unhealty",
    "message": "Service is unhealthy"
}
```

- **heartbeat:mode** – Heartbeat mode: `server` for server-side probing, `client` for client-side reporting (default).
- **heartbeat:interval** – Heartbeat interval in seconds (default 30s).
- **heartbeat:url** – Health-check endpoint when `mode` is `server`. HTTP 2xx indicates healthy, all other statuses are treated as failure.

### Service registration

After configuring the client and starting the application, the service information is automatically registered and heartbeats begin. You can verify the registration from the service management page in the console.

### Service discovery

Inject `IDiscoveryService` to access all registered services from your application.

```
public interface IDiscoveryService
    {
        string DataVersion { get; }
        List<ServiceInfo> UnHealthyServices { get; }
        List<ServiceInfo> HealthyServices { get; }
        List<ServiceInfo> Services { get; }
        Task RefreshAsync();
    }
```

Besides the built-in members, a few extension methods are provided to simplify common scenarios such as selecting a random service instance:

```
    public static class DiscoveryServiceExtension
    {
        public static IEnumerable<ServiceInfo> GetByServiceName(this IDiscoveryService ds, string serviceName)
        {
            return ds.Services.GetByServiceName(serviceName);
        }

        public static ServiceInfo GetByServiceId(this IDiscoveryService ds, string serviceId)
        {
            return ds.Services.GetByServiceId(serviceId);
        }

        public static ServiceInfo RandomOne(this IDiscoveryService ds, string serviceName)
        {
            return ds.Services.RandomOne(serviceName);
        }
    }
```

## Contact

Email: minj.zhou@gmail.com

QQ group: 1022985150
