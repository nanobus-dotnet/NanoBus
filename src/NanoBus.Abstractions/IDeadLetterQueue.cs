namespace NanoBus.Abstractions;

/// <summary>
/// The manager of dead letter queue messages.
/// </summary>
public interface IDeadLetterQueue
{
    /// <summary>
    /// The max number of times to retry message delivery.
    /// </summary>
    int MaxDeliveryAttempts { get; set; }

    /// <summary>
    /// Enqueues a message to the DLQ.
    /// </summary>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="messageType">The message type.</param>
    /// <param name="method">The message method (Send/Publish).</param>
    /// <param name="context">The context of the message.</param>
    /// <param name="lastFailedOn">The time the message last failed.</param>
    /// <param name="deliveryAttempts">The number of attempts of message delivery.</param>
    /// <param name="lastError">The last error experienced during delivery.</param>
    /// <param name="invokerToken">The token used to cancel the send/publish operation.</param>
    /// <param name="cancellationToken">The token used to cancel this operation.</param>
    /// <returns>The id of the enqueued message.</returns>
    ValueTask<string> EnqueueAsync(IMessage message, string messageType, MessageMethod method, 
        MessageContext context, DateTime lastFailedOn, int deliveryAttempts, Exception lastError, CancellationToken invokerToken, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all completely failed messages in the DLQ.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<IEnumerable<FailedMessage>> GetAllFailedMessagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets all the messages in the DLQ.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<IEnumerable<FailedMessage>> GetAllMessagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a failed message by unique id.
    /// </summary>
    /// <param name="uniqueId">The id of the failed message to get.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<FailedMessage?> GetMessageAsync(string uniqueId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the messages ready to be retried.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<IEnumerable<FailedMessage>> GetMessagesForRetryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Removes a message from the DLQ.
    /// </summary>
    /// <param name="id">The id of the message to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<FailedMessage?> RemoveAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the dead letter queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ClearQueueAsync(CancellationToken cancellationToken);
}