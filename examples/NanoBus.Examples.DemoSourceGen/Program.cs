using NanoBus.Examples.DemoSourceGen;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(cfg =>
{
    cfg.WriteTo
        .Console()
        .MinimumLevel.Debug();
});

builder.Services.AddDemoServiceBus();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
