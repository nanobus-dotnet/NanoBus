using NanoBus.Examples.DemoInjection;
using NanoBus.Examples.DemoInjection.Handlers;
using NanoBus.Transports.Demo;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(configure =>
{
    configure.MinimumLevel
        .Debug()
        .WriteTo.Console();
});

builder.Services.AddScoped<CreateSalesOrderHandler>();
builder.Services.AddScoped<OrderConfirmedHandler>();
builder.Services.AddScoped<RetryHandler>();

builder.Services.AddDemoServiceBus(null, (provider, transport) =>
{
    using var scope = provider.CreateScope();
    var createOrderHandler = scope.ServiceProvider.GetRequiredService<CreateSalesOrderHandler>();
    var orderConfirmedHandler = scope.ServiceProvider.GetRequiredService<OrderConfirmedHandler>();
    var retryHandler = scope.ServiceProvider.GetRequiredService<RetryHandler>();

    transport.RegisterHandler(createOrderHandler);
    transport.RegisterHandler(orderConfirmedHandler);

    // Uncomment this line to see retries.
    //transport.RegisterHandler(retryHandler);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
