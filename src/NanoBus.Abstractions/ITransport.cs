namespace NanoBus.Abstractions;

/// <summary>
/// The interface that outlines the transport for the message bus and its functionality.
/// </summary>
public interface ITransport
{
    /// <summary>
    /// The method used to register handler(s) for a given message type.
    /// </summary>
    /// <typeparam name="T">The type of message being handled.</typeparam>
    /// <param name="handler">The handler of the message.</param>
    void RegisterHandler<T>(IMessageHandler<T> handler) where T : IMessage;

    /// <summary>
    /// The method that sends a message to a message handler.
    /// </summary>
    /// <typeparam name="T">The type of message to send.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="metadata">The optional message metadata to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask SendAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage;

    /// <summary>
    /// The method that publishes a message to one or more message handlers.
    /// </summary>
    /// <typeparam name="T">The type of message to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="metadata">The optional message metadata to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask PublishAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage;

    /// <summary>
    /// The method that starts the transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The method that stops the transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all failure messages in the dead letter queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All failed messages in the DLQ.</returns>
    ValueTask<IEnumerable<FailedMessage>> GetDeadLetterMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all completely failed messages in the dead letter queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All completely failed messages in the DLQ.</returns>
    ValueTask<IEnumerable<FailedMessage>> GetFailedDeadLetterMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Immediately reprocesses all messages in the dead letter queue at the next interval.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ReprocessDeadLetterQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprocesses a single item in the dead letter queue.
    /// </summary>
    /// <param name="uniqueId">The unique id of the item to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ReprocessDeadLetterMessageAsync(string uniqueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes failed messages in the dead letter queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ProcessDeadLetterQueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the entire dead letter queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ClearDeadLetterQueueAsync(CancellationToken cancellationToken = default);
}
