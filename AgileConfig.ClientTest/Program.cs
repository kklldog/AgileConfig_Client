using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using AgileConfig.Client;
using AgileConfig.Client.RegisterCenter;
using System.Threading;

namespace AgileConfigClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var client = new ConfigClient();
            var lf = serviceProvider.GetService<ILoggerFactory>();

            Task.Run(async () =>
            {
                try
                {
                    client.Logger = lf.CreateLogger<ConfigClient>();
                    client.ReLoaded += Client_ReLoaded;
                    await client.ConnectAsync();
                    await Task.Run(async () =>
                    {
                        while (true)
                        {
                            await Task.Delay(5000);
                            foreach (string key in client.Data.Keys)
                            {
                                var val = client[key];
                                Console.WriteLine("{0} : {1}", key, val);
                            }
                        }
                    });

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });

            // regiseter center / service discovery
            Task.Run(async ()=> {
                try
                {
                    var regService = new RegisterService(client, lf);
                    var disService = new DiscoveryService(client, lf);
                    var service = new RegisterHostedService(regService, lf);

                    await service.StartAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });

            Console.ReadLine();
        }

        private static void Client_ReLoaded(ConfigReloadedArgs obj)
        {
            var oldConfigs = obj.OldConfigs;
            var newConfigs = obj.NewConfigs;

            foreach (var item in newConfigs)
            {
                Console.WriteLine($"new {item.Key}={item.Value}");
            }
            foreach (var item in oldConfigs)
            {
                Console.WriteLine($"old {item.Key}={item.Value}");
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole());
            services.Configure<LoggerFilterOptions>(op =>
            {
                op.MinLevel = LogLevel.Trace;
            });
        }
    }
}
