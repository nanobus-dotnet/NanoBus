namespace NanoBus.Abstractions;

/// <summary>
/// The interface that outlines the message bus and its functionality.
/// </summary>
public interface IServiceBus
{
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
    /// The method that starts the service bus.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The method that stops the service bus.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
