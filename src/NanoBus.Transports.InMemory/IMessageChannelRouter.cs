using NanoBus.Abstractions;

namespace NanoBus.Transports.InMemory;

/// <summary>
/// Routes messages to channels for delivery and query.
/// </summary>
public interface IMessageChannelRouter
{
    /// <summary>
    /// The capacity of all channels.
    /// </summary>
    int BoundedChannelCapacity { get; }

    /// <summary>
    /// Completes all channels.
    /// </summary>
    void CompleteAll();

    /// <summary>
    /// Enqueues a message to the given channel.
    /// </summary>
    /// <param name="messageType">The type of message.</param>
    /// <param name="message">The message.</param>
    /// <param name="method">The delivery method (Send/Receive).</param>
    /// <param name="context">The message context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The success state of the enqueue operation.</returns>
    ValueTask<bool> EnqueueMessageAsync(string messageType, IMessage message, MessageMethod method, 
        MessageContext context, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets the message types of messages pending delivery.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of message types.</returns>
    IAsyncEnumerable<string> GetPendingMessageTypesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads all messages from the message type channel.
    /// </summary>
    /// <param name="messageType">The message type of messages to read from the given channel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of messages from the given channel.</returns>
    IAsyncEnumerable<(IMessage Message, MessageMethod Method, MessageContext Context, CancellationToken InvokerToken)> 
        ReadAllAsync(string messageType, CancellationToken cancellationToken);
}