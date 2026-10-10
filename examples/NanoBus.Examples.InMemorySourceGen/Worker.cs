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

        while (!stoppingToken.IsCancellationRequested)
        {

            foreach (var order in SampleData.GetSalesOrders())
            {
                var createOrderCommand = SampleData.GetCreateSalesOrderCommand(order.Id);
                await _serviceBus.SendAsync(createOrderCommand, cancellationToken: stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(new Random().Next(1, 5)), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(new Random().Next(15, 20)), stoppingToken);

            var failedMessages = await _serviceBus.GetFailedDeadLetterMessagesAsync(stoppingToken);

            if (!failedMessages.Any())
                continue;

            // Allow a bit of time then get the DLQ messages.
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            // Reprocess a single DLQ message.
            var uniqueId = failedMessages.First().UniqueId;

            await _serviceBus.ReprocessDeadLetterMessageAsync(uniqueId, stoppingToken);

            // Then reprocess all DLQ messages.
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            foreach (var message in failedMessages)
            {
                var context = message.Context;
                _logger.LogInformation("DLQ Message: {UniqueId} - MessageId: {MessageId} - CorrelationId: {CorrelationId} - {DeliveryAttempts} Delivery Attempts - Failed at: {LastFailedOn} - {ErrorMessage}", message.UniqueId, context.MessageId, context.CorrelationId, message.DeliveryAttempts, message.LastFailedOn, message.LastError?.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            await _serviceBus.ReprocessDeadLetterQueueAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            await _serviceBus.ClearDeadLetterQueueAsync(stoppingToken);


        }
    }
}
