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
    Task SendAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage;

    /// <summary>
    /// The method that publishes a message to one or more message handlers.
    /// </summary>
    /// <typeparam name="T">The type of message to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="metadata">The optional message metadata to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PublishAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage;

    /// <summary>
    /// The method that starts the transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task result.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The method that stops the transport.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>the Task result.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
