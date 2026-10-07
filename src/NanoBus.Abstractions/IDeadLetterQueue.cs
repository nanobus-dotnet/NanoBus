namespace NanoBus.Abstractions;

/// <summary>
/// The manager of dead letter queue messages.
/// </summary>
public interface IDeadLetterQueue
{
    /// <summary>
    /// The max number of times to retry message delivery.
    /// </summary>
    int MaxRetries { get; set; }

    /// <summary>
    /// Enqueues a message to the DLQ.
    /// </summary>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="messageType">The message type.</param>
    /// <param name="method">The message method (Send/Publish).</param>
    /// <param name="context">The context of the message.</param>
    /// <param name="lastFailedOn">The time the message last failed.</param>
    /// <param name="lastError">The last error experienced during delivery.</param>
    /// <param name="token">The token used to cancel the send/publish operation.</param>
    /// <param name="cancellationToken">The token used to cancel this operation.</param>
    /// <returns>The id of the enqueued message.</returns>
    ValueTask<string> EnqueueAsync(IMessage message, string messageType, MessageMethod method, MessageContext context, DateTime lastFailedOn, Exception lastError, CancellationToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all completely failed messages in the DLQ.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<IEnumerable<FailedMessage>> GetAllFailedMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all the messages in the DLQ.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<IEnumerable<FailedMessage>> GetAllMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a failed message by unique id.
    /// </summary>
    /// <param name="uniqueId">The id of the failed message to get.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<FailedMessage?> GetMessageAsync(string uniqueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the messages ready to be retried.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<IEnumerable<FailedMessage>> GetMessagesForRetryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a message from the queue if the delivery succeeds.
    /// </summary>
    /// <param name="id">The id of the message to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the retry message.
    /// </summary>
    /// <param name="uniqueId">The id of the message to update.</param>
    /// <param name="retryCount">The current retry count.</param>
    /// <param name="lastError">The last error experience during delivery.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask UpdateMessageRetryAsync(string uniqueId, int retryCount, Exception? lastError = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the dead letter queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    ValueTask ClearQueueAsync(CancellationToken cancellationToken = default);
}