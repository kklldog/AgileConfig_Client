# AgileConfig_Client
AgileConfig的客户端，.net core standard2.0实现，core跟framework的.net程序都可以使用。
## 使用客户端
### 安装客户端
```
Install-Package AgileConfig.Client
```
### 初始化客户端
以asp.net core mvc项目为例：
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
在appsettings.json文件内配置agileconfig的连接信息。
```
       public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                //new一个client实例
                var configClient = new ConfigClient();
                //使用AddAgileConfig配置一个新的IConfigurationSource
                config.AddAgileConfig(configClient);
                //找一个变量挂载client实例，以便其他地方可以直接使用实例访问配置
                ConfigClient = configClient;
                //注册配置项修改事件
                configClient.ConfigChanged += ConfigClient_ConfigChanged;
            })
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

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IOptions<DbConfigOptions> dbOptions)
        {
            _logger = logger;
            _IConfiguration = configuration;
            _dbOptions = dbOptions;
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
            var userId = Program.ConfigClient["userId"];
            var dbConn = Program.ConfigClient["db:connection"];

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
