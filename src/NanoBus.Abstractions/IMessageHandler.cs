namespace NanoBus.Abstractions;

/// <summary>
/// The interface that outlines how message are handled. Message handlers must implement this.
/// </summary>
/// <typeparam name="T">The type of message being sent or published.</typeparam>
public interface IMessageHandler<T> where T : IMessage
{
    /// <summary>
    /// The function that handles the message that was sent or published from the message bus.
    /// </summary>
    /// <param name="message">The message that was sent or published from the bus.</param>
    /// <param name="context">The context of the message that was sent or published.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task HandleAsync(T message, MessageContext context, CancellationToken cancellationToken = default);
}
