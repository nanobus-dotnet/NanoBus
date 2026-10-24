using NanoBus.Abstractions;

namespace DlqWorker;

public class Worker(IServiceBus serviceBus, ILogger<Worker> logger) : BackgroundService
{
    private readonly IServiceBus _serviceBus = serviceBus;
    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(10), stoppingToken); // Allow startup messages to be logged to the console.

        await _serviceBus.StartAsync(stoppingToken);

        var command = SampleData.GetCreateSalesOrderCommand();

        await _serviceBus.SendAsync(command, cancellationToken: stoppingToken);

        // Allow message to sit in DLQ.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        var dlqMessage = (await _serviceBus.GetFailedDeadLetterMessagesAsync(stoppingToken)).First();

        _logger.LogInformation("DLQ message found - UniqueId: {UniqueId} - MessageId: {MessageId} - CorrelationId: {CorrelationId} - Last Error: {LastError}", dlqMessage.UniqueId, dlqMessage.Context.MessageId, dlqMessage.Context.CorrelationId, dlqMessage.LastError.Message);

        // Attempt processing again.
        await _serviceBus.ReprocessDeadLetterMessageAsync(dlqMessage.UniqueId, stoppingToken);

        // Allow message to sit in DLQ.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        await _serviceBus.ReprocessDeadLetterQueueAsync(stoppingToken);
    }
}
