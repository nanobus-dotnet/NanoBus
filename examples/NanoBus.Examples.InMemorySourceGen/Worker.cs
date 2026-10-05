using NanoBus.Abstractions;

namespace NanoBus.Examples.InMemorySourceGen;

public class Worker(IServiceBus serviceBus, ILogger<Worker> logger) : BackgroundService
{
    private readonly IServiceBus _serviceBus = serviceBus;
    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _serviceBus.StartAsync(stoppingToken);
        await Task.Delay(TimeSpan.FromMilliseconds(10), stoppingToken); // Allow startup messages to be logged to the console.

        _logger.LogInformation("Processing sales orders");

        //while (!stoppingToken.IsCancellationRequested)
        //{
            foreach (var order in SampleData.GetSalesOrders().Where(x => x.Id == 1))
            {
                var createOrderCommand = SampleData.GetCreateSalesOrderCommand(order.Id);
                await _serviceBus.SendAsync(createOrderCommand, cancellationToken: stoppingToken);
            }

            //await Task.Delay(10000, stoppingToken);

            //await _serviceBus.StopAsync(stoppingToken);
            //return;
        //}
    }
}
