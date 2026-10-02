using NanoBus.Examples.DemoInjection;
using NanoBus.Examples.DemoInjection.Handlers;
using NanoBus.Transports.Demo.DependencyInjection;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(configure =>
{
    configure.MinimumLevel.Debug()
    .WriteTo.Console();
});

builder.Services.AddSingleton<ICreateSalesOrderHandler, CreateSalesOrderHandler>();
builder.Services.AddSingleton<IOrderConfirmedHandler, OrderConfirmedHandler>();
builder.Services.AddSingleton<IRetryHandler, RetryHandler>();

builder.Services.AddDemoServiceBus((sp, transport) =>
{
    var createOrderHandler = sp.GetRequiredService<ICreateSalesOrderHandler>();
    var orderConfirmedHandler = sp.GetRequiredService<IOrderConfirmedHandler>();
    var retryHandler = sp.GetRequiredService<IRetryHandler>();

    transport.RegisterHandler(createOrderHandler);
    transport.RegisterHandler(orderConfirmedHandler);
    //transport.RegisterHandler(retryHandler);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
