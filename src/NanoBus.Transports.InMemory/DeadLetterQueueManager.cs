using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;

namespace NanoBus.Transports.InMemory;

/// <summary>
/// Manager of dead letter queue message.
/// </summary>
/// <param name="logger">The logger to use.</param>
public class DeadLetterQueueManager(ILogger? logger = null)
{
    private readonly ConcurrentDictionary<string, FailedMessage> _queue = new();
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    /// <summary>
    /// The max number of times to retry message delivery.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enqueues a message to the DLQ.
    /// </summary>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="messageType">The message type.</param>
    /// <param name="method">The message method (Send/Publish).</param>
    /// <param name="context">The context of the message.</param>
    /// <param name="lastError">The last error experienced during delivery.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The id of the message that was enqueued.</returns>
    public string Enqueue(IMessage message, string messageType, MessageMethod method, MessageContext context,
         CancellationToken cancellationToken, Exception lastError)
    {

        _logger.LogError(lastError, "Delivery failed for MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}'. Retry Attempts {RetryAttempts}", context.MessageId, context.CorrelationId, messageType, 1);
        var id = Guid.NewGuid().ToString();

        var failed = new FailedMessage(
            UniqueId: id,
            MessageType: messageType,
            Message: message,
            Method: method,
            Context: context,
            RetryCount: 0,
            NextRetryTime: DateTime.UtcNow + GetBackoffDelay(0),
            LastError: lastError,
            cancellationToken
        );

        _queue[id] = failed;
        return id;
    }

    /// <summary>
    /// Gets the messages ready to be retried.
    /// </summary>
    /// <returns>The list of failed message for retry.</returns>
    public List<FailedMessage> GetMessagesForRetry() =>
        [.. _queue.Values.Where(m => m.NextRetryTime <= DateTime.UtcNow && m.RetryCount < MaxRetries)];

    /// <summary>
    /// Updates the retry message.
    /// </summary>
    /// <param name="id">The id of the message to update.</param>
    /// <param name="retryCount">The current retry count.</param>
    /// <param name="lastError">The last error experience during delivery.</param>
    public void UpdateMessageRetry(string id, int retryCount, Exception? lastError = null)
    {
        if (!_queue.TryGetValue(id, out var failed))
            return;

        var backoffDelay = GetBackoffDelay(retryCount);

        _queue[id] = failed with
        {
            RetryCount = retryCount,
            NextRetryTime = DateTime.UtcNow + backoffDelay,
            LastError = lastError
        };

        if (retryCount == MaxRetries)
        {
            _logger.LogError(failed.LastError, "All retries failed for MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}'.", failed.Context.MessageId, failed.Context.CorrelationId, failed.MessageType);
            return;
        }

        _logger.LogError(failed.LastError, "Delivery failed for MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}'. Retrying in {BackoffDelay} Retry Attempts {RetryAttempts}", failed.Context.MessageId, failed.Context.CorrelationId, failed.MessageType, backoffDelay, retryCount);
    }

    /// <summary>
    /// Removes a message from the queue if the delivery succeeds.
    /// </summary>
    /// <param name="id">The id of the message to remove.</param>
    public void Remove(string id) => _queue.TryRemove(id, out _);

    /// <summary>
    /// Gets the backoff delay of message redelivery.
    /// </summary>
    /// <param name="retryCount">The current retry count.</param>
    /// <returns>The time to delay the message delivery.</returns>
    public static TimeSpan GetBackoffDelay(int retryCount)
    {
        // Exponential backoff max at 1 minute
        var delaySeconds = Math.Min(1 * (int)Math.Pow(2, retryCount), 60);
        return TimeSpan.FromSeconds(delaySeconds);
    }
}
