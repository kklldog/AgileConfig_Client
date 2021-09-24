# AgileConfig_Client
AgileConfig的客户端，.net core standard2.0实现，core跟framework的.net程序都可以使用。
## 使用客户端
### 安装客户端
```
Install-Package AgileConfig.Client
```

☢️如果你的程序是framework的程序请使用[frameworkVersion](https://github.com/kklldog/AgileConfig_Client/tree/frameworkVersion)分支的代码自己编译。使用master版本有可能死锁造成cpu100% ☢️

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
    "tag": "tag1"
  }
}

```
在program的ConfigureAppConfiguration方法内使用AddAgileConfig添加一个配置源。
```
       public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddAgileConfig((arg) =>
                {
                    Console.WriteLine($"action:{arg.Action} key:{arg.Key}");
                });
            })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
```
或者使用UseAgileConfig扩展方法。
```
 public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseAgileConfig(e => Console.WriteLine($"configs {e.Action}"))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
```
如果需要使用ConfigClient的实例来直接读取配置，可以在startup类的ConfigureServices方法内配置AddAgileConfig，以便使用IConfigClient接口直接注入ConfigClient的实例。
```
     public void ConfigureServices(IServiceCollection services)
        {
            services.AddAgileConfig();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AgileConfigMVCSampleNET5", Version = "v1" });
            });
        }
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
## 联系我
有什么问题可以mail我：minj.zhou@gmail.com
也可以加qq群：1022985150
