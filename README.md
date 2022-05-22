# AgileConfig_Client
AgileConfig 的客户端，.net core standard2.0实现 。     

[![package](https://github.com/kklldog/AgileConfig_Client/actions/workflows/publish2nuget.yml/badge.svg)](https://github.com/kklldog/AgileConfig_Client/actions/workflows/publish2nuget.yml)
![Nuget](https://img.shields.io/nuget/v/agileconfig.client?label=client%20version)
![Nuget](https://img.shields.io/nuget/dt/agileconfig.client?label=nuget%20download)
## 使用客户端
### 安装客户端
```
Install-Package AgileConfig.Client
```

☢️☢️☢️如果你的程序是Framework的程序请使用[AgileConfig.Client4FR](https://github.com/kklldog/AgileConfig.Client4FR)这个专门为Framework打造的client。使用当前版本有可能死锁造成cpu100% 的风险。☢️☢️☢️

### 初始化客户端
以asp.net core mvc项目为例：   
在appsettings.json文件内配置agileconfig的连接信息。
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
    "nodes": "http://localhost:5000,http://localhost:5001"//多个节点使用逗号分隔,
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
#### 配置项说明

|配置项名称|数据类型|配置项说明|是否必填|备注|
|--|--|--|--|--|
|appid|string|应用ID|是|对应后台管理中应用的`应用ID`|
|secret|string|应用密钥|是|对应后台管理中应用的`密钥`|
|nodes|string|应用配置节点|是|存在多个节点则使用逗号`,`分隔|
|name|string|连接客户端的自定义名称|否|方便在agile配置中心后台对当前客户端进行查阅与管理|
|tag|string|连接客户端自定义标签|否|方便在agile配置中心后台对当前客户端进行查阅与管理|
|env|string|配置中心的环境|否|通过此配置决定拉取哪个环境的配置信息；如果不配置，服务端会默认返回第一个环境的配置|
|cache|string|客户端的配置缓存设置|否|通过此配置可对拉取到本地的配置项文件进行相关设置|
|cache:directory|string|客户端的配置缓存文件存储地址配置|否|如设置了此目录则将拉取到的配置项cache文件存储到该目录，否则直接存储到站点根目录|
|cache:config_encrypt|bool|客户端缓存文件加密设置|否|如果设置为`true`则对缓存的文件内容进行加密|
|httpTimeout|int|http请求超时时间|否|配置 client 发送 http 请求的时候的超时时间，默认100s|

## UseAgileConfig
在 program 类上使用 UseAgileConfig 扩展方法，该方法会配置一个 AgileConfig 的配置源。
```
 public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseAgileConfig(e => Console.WriteLine($"configs {e.Action}"))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
```
## 读取配置
AgileConfig支持asp.net core 标准的IConfiguration，跟IOptions模式读取配置。还支持直接通过AgileConfigClient实例直接读取：
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
        /// 使用IConfiguration读取配置
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
        /// 直接使用ConfigClient的实例读取配置
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
        /// 使用Options模式读取配置
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
## 使用服务注册&发现
在 appsettings.json 的 AgileConfig 节点添加 serviceRegister 节点：
```
 "AgileConfig": {
    "appId": "test_app",
    "secret": "test_app",
    "nodes": "http://agileconfig_server.xbaby.xyz/",
    "name": "client123",
    "tag": "tag123",

    "serviceRegister": { //服务注册信息，如果不配置该节点，则不会启动任何跟服务注册相关的服务 可选
      "serviceId": "net6", //服务id，全局唯一，用来唯一标示某个服务
      "serviceName": "net6MVC服务测试", //服务名，可以重复，某个服务多实例部署的时候这个serviceName就可以重复
      "ip": "127.0.0.1", //服务的ip 可选
      "port": 5005, //服务的端口 可选
  }
```
其中 appId , secret 等配置同原来配置中心的使用方式没有任何改变。   
`serviceRegister` 节点描述的是服务注册信息（如果删除这个节点那么服务注册功能就不会启动）：   
- serviceId  
服务id，全局唯一，用来唯一标示某个服务
- serviceName  
服务名，可以重复，某个服务多实例部署的时候这个serviceName就可以重复  
- ip  
服务的ip 可选
- port   
服务的端口 可选
- metaData  
一个字符串数组，可以携带一些服务的相关信息，如版本等 可选
- alarmUrl  
告警地址 可选。   
如果某个服务出现异常情况，如一段时间内没有心跳，那么服务端会往这个地址 POST 一个请求并且携带服务相关信息，用户可以自己去实现提醒功能，比如发短信，发邮件等：
```
{
    "serviceId":"0001",
    "serviceName":"xxxx",
    "time":"2022-01-01T12:00:000",
    "status":"Unhealty",
    "message": "服务不健康"
}
```
- heartbeat:mode  
指定心跳的模式，server/client 。server代表服务端主动检测，client代表客户端主动上报。不填默认client模式 可选
- heartbeat:interval  
心跳的间隔，默认时间30s 可选
- heartbeat:url  
心跳模式为 server 的时候需要填写健康检测地址，如果是httpstatus为200段则判定存活，其它都视为失败 可选   
### 服务的注册
当配置好客户端后，启动对应的应用程序，服务信息会自动注册到服务端并且开始心跳。如果服务正确注册到服务端，控制台的服务管理界面可以查看：
![](https://static.xbaby.xyz/serviceregister.png)
### 服务发现
现在服务已经注册上去了，那么怎么才能拿到注册中心所有的服务呢？同样非常简单，在程序内只要注入`IDiscoveryService `接口就可以通过它拿到所有的注册的服务。
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
除了接口内置的方法，还有几个扩展方法方便用户使用，比如随机一个服务：
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
## 联系我
有什么问题可以mail我：minj.zhou@gmail.com
也可以加qq群：1022985150
