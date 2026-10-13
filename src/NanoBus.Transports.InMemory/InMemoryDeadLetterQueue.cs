using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;

namespace NanoBus.Transports.InMemory;

/// <summary>
/// The in-memory manager of dead letter queue messages.
/// </summary>
/// <param name="logger">The logger to use.</param>
public class InMemoryDeadLetterQueue(ILogger<InMemoryDeadLetterQueue>? logger = null) : IDeadLetterQueue
{
    private readonly ConcurrentDictionary<string, FailedMessage> _queue = new();
    private readonly ILogger _logger = logger ?? NullLogger<InMemoryDeadLetterQueue>.Instance;

    /// <inheritdoc />
    public int MaxDeliveryAttempts { get; set; } = 4;

    /// <summary>
    /// Enqueues a failed message with exponential backoff retry scheduling.
    /// </summary>
    /// <returns>The unique Id of the enqueued message.</returns>
    public ValueTask<string> EnqueueAsync(IMessage message, string messageType, MessageMethod method, MessageContext context, DateTime lastFailedOn, int deliveryAttempt, Exception lastError, CancellationToken invokerToken, CancellationToken cancellationToken)
    {
        if (deliveryAttempt <= 0)
        {
            _logger.LogWarning("Delivery attempt must be at least 1. Setting to 1.");
            deliveryAttempt = 1;
        }
        var id = Guid.NewGuid().ToString();

        var delay = GetBackoffDelay(deliveryAttempt - 1);

        var failed = new FailedMessage(
            UniqueId: id,
            MessageType: messageType,
            Message: message,
            Method: method,
            Context: context,
            DeliveryAttempts: deliveryAttempt + 1,
            LastFailedOn: lastFailedOn,
            NextRetryTime: lastFailedOn + delay,
            LastError: lastError,
            Token: invokerToken
            );

        _queue[id] = failed;

        if (deliveryAttempt >= MaxDeliveryAttempts)
        {
            _logger.LogError(lastError, "All retries failed for MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}'.", context.MessageId, context.CorrelationId, messageType);
            return ValueTask.FromResult(id);
        }

        _logger.LogError(lastError, "Enqueuing failed message MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}' - Retrying in {Delay} - Attempt {DeliveryAttempt}", context.MessageId, context.CorrelationId, messageType, delay, deliveryAttempt + 1);

        return ValueTask.FromResult(id);
    }


    /// <inheritdoc />
    public ValueTask<IEnumerable<FailedMessage>> GetMessagesForRetryAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(_queue.Values.ToList().Where(m => m.NextRetryTime <= DateTime.UtcNow && m.DeliveryAttempts <= MaxDeliveryAttempts));

    /// <inheritdoc />
    public ValueTask<IEnumerable<FailedMessage>> GetAllMessagesAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(_queue.Values.ToList().AsEnumerable());

    /// <inheritdoc />
    public ValueTask<IEnumerable<FailedMessage>> GetAllFailedMessagesAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(_queue.Values.ToList().Where(m => m.DeliveryAttempts > MaxDeliveryAttempts));

    /// <inheritdoc />
    public ValueTask<FailedMessage?> GetMessageAsync(string uniqueId, CancellationToken cancellationToken)
    {
        _queue.TryGetValue(uniqueId, out var failedMessage);
        return ValueTask.FromResult(failedMessage);
    }

    /// <inheritdoc />
    public ValueTask<FailedMessage?> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        _queue.Remove(id, out var failedMessage);
        return ValueTask.FromResult(failedMessage);
    }

    /// <inheritdoc />
    public ValueTask ClearQueueAsync(CancellationToken cancellationToken)
    {
        _queue.Clear();
        return ValueTask.CompletedTask;
    }

    private static TimeSpan GetBackoffDelay(int retryCount)
    {
        // Exponential backoff max at 1 minute
        var delaySeconds = Math.Min((int)Math.Pow(2, retryCount), 60);
        return TimeSpan.FromSeconds(delaySeconds);
    }

}
