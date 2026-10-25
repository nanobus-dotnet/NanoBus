using NanoBus.Abstractions;

namespace LocalInMemoryWorker;

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

            // Allow a bit of time then get the DLQ messages.
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            var failedMessages = (await _serviceBus.GetFailedDeadLetterMessagesAsync(stoppingToken)).ToList();

            if (failedMessages.Count == 0)
            {
                _logger.LogInformation("No messages in the DLQ.");
                continue;
            }

            _logger.LogInformation("{Count} messages found in the DLQ.", failedMessages.Count);

            // Reprocess a single DLQ message.
            var uniqueId = failedMessages.First().UniqueId;

            await _serviceBus.ReprocessDeadLetterMessageAsync(uniqueId, stoppingToken);

            // Then reprocess all DLQ messages.
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            foreach (var message in failedMessages)
            {
                var context = message.Context;
                _logger.LogInformation("DLQ Message: {UniqueId} - MessageId: {MessageId} - CorrelationId: {CorrelationId} - {DeliveryAttempts} Delivery Attempts - Failed at: {LastFailedOn} - {ErrorMessage}", message.UniqueId, context.MessageId, context.CorrelationId, message.DeliveryAttempts, message.LastFailedOn, message.LastError?.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            // Reprocess the entire DLQ.
            await _serviceBus.ReprocessDeadLetterQueueAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            // Clear the DLQ.
            await _serviceBus.ClearDeadLetterQueueAsync(stoppingToken);

        }

    }
}
