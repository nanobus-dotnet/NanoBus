using LocalInMemoryWorker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(cfg =>
{
    cfg.MinimumLevel
        .Debug()
        .WriteTo.Console();
});

builder.Services.AddInMemoryServiceBus();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
