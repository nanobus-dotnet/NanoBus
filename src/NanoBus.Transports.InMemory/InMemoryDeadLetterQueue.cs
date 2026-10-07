using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;

namespace NanoBus.Transports.InMemory;

/// <summary>
/// The in-memory manager of dead letter queue messages.
/// </summary>
/// <param name="logger">The logger to use.</param>
public class InMemoryDeadLetterQueue(ILogger? logger = null) : IDeadLetterQueue
{
    private readonly ConcurrentDictionary<string, FailedMessage> _queue = new();
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    /// <inheritdoc />
    public int MaxRetries { get; set; } = 3;

    /// <inheritdoc />
    public ValueTask<string> EnqueueAsync(IMessage message, string messageType, MessageMethod method, MessageContext context, DateTime lastFailedOn, Exception lastError,
             CancellationToken token, CancellationToken cancellationToken = default)
    {
        var delay = GetBackoffDelay(0);
        _logger.LogError(lastError, "Delivery failed for MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}' - Retrying in {Delay} - Retry Attempts {RetryAttempts}", context.MessageId, context.CorrelationId, messageType, delay, 1);
        var id = Guid.NewGuid().ToString();

        var failed = new FailedMessage(
            UniqueId: id,
            MessageType: messageType,
            Message: message,
            Method: method,
            Context: context,
            DeliveryAttempts: 1,
            LastFailedOn: lastFailedOn,
            NextRetryTime: lastFailedOn + delay,
            LastError: lastError,
            Token: token
        );

        _queue[id] = failed;
        return ValueTask.FromResult(id);
    }


    /// <inheritdoc />
    public ValueTask<IEnumerable<FailedMessage>> GetMessagesForRetryAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_queue.Values.Where(m => m.NextRetryTime <= DateTime.UtcNow && m.DeliveryAttempts <= MaxRetries));

    /// <inheritdoc />
    public ValueTask<IEnumerable<FailedMessage>> GetAllMessagesAsync(CancellationToken cancellationToken = default) 
        => ValueTask.FromResult(_queue.Values.AsEnumerable());

    /// <inheritdoc />
    public ValueTask<IEnumerable<FailedMessage>> GetAllFailedMessagesAsync(CancellationToken cancellationToken = default) 
        => ValueTask.FromResult(_queue.Values.Where(m => m.DeliveryAttempts > MaxRetries));

    /// <inheritdoc />
    public ValueTask<FailedMessage?> GetMessageAsync(string uniqueId, CancellationToken cancellationToken = default)
    {
        _queue.TryGetValue(uniqueId, out var failedMessage);
        return ValueTask.FromResult(failedMessage);
    }

    /// <inheritdoc />
    public ValueTask UpdateMessageRetryAsync(string uniqueId, int retryCount, Exception? lastError = null, CancellationToken cancellationToken = default)
    {
        if (!_queue.TryGetValue(uniqueId, out var failed))
            return ValueTask.CompletedTask;

        var backoffDelay = GetBackoffDelay(retryCount);

        _queue[uniqueId] = failed with
        {
            DeliveryAttempts = retryCount + 1,
            NextRetryTime = DateTime.UtcNow + backoffDelay,
            LastError = lastError
        };

        if (retryCount >= MaxRetries)
        {
            _logger.LogError(failed.LastError, "All retries failed for MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}'.", failed.Context.MessageId, failed.Context.CorrelationId, failed.MessageType);
            return ValueTask.CompletedTask;
        }

        _logger.LogError(failed.LastError, "Delivery failed for MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}'. Retrying in {BackoffDelay} Retry Attempts {RetryAttempts}", failed.Context.MessageId, failed.Context.CorrelationId, failed.MessageType, backoffDelay, retryCount + 1);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        _queue.TryRemove(id, out _);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearQueueAsync(CancellationToken cancellationToken = default)
    {
        _queue.Clear();
        return ValueTask.CompletedTask;
    }

    private static TimeSpan GetBackoffDelay(int retryCount)
    {
        // Exponential backoff max at 1 minute
        var delaySeconds = Math.Min(1 * (int)Math.Pow(2, retryCount), 60);
        return TimeSpan.FromSeconds(delaySeconds);
    }

}
