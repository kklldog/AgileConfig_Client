using AgileConfig.Client.Extensions;
using AgileWorkerServiceNET8;

var builder = Host.CreateApplicationBuilder(args);
builder.UseAgileConfig();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();