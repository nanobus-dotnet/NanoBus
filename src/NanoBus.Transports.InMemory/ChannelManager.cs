using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using NanoBus.Abstractions;

namespace NanoBus.Transports.InMemory;

/// <summary>
/// The manager of message channels.
/// </summary>
/// <param name="boundedChannelCapacity">The capacity of the channels before messages wait.</param>
public class ChannelManager(int boundedChannelCapacity = 100)
{
    private readonly ConcurrentDictionary<string, Channel<(IMessage Message, MessageMethod Method, MessageContext Context, int RetryCount, CancellationToken Token)>> _typeChannels = new();
    private readonly Channel<string> _channelNotification = Channel.CreateUnbounded<string>();

    /// <summary>
    /// The max capacity of the channel
    /// </summary>
    public int BoundedChannelCapacity { get; } = boundedChannelCapacity;

    /// <summary>
    /// Enqueues the message.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="method">The method used, either Send or Publish.</param>
    /// <param name="context">The context to pass to the handler(s).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="retryCount"></param>
    /// <returns>The task to enqueue messages.</returns>
    public async ValueTask<bool> EnqueueMessageAsync(string messageType, IMessage message, MessageMethod method,
        MessageContext context, int retryCount, CancellationToken cancellationToken)
    {
        var channel = _typeChannels.GetOrAdd(messageType, _ =>
            Channel.CreateBounded<(IMessage, MessageMethod, MessageContext, int, CancellationToken)>(
                new BoundedChannelOptions(BoundedChannelCapacity) { FullMode = BoundedChannelFullMode.Wait }));

        if (!await channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
        {
            return false; // The channel is closed.
        }

        await channel.Writer.WriteAsync((message, method, context, retryCount, cancellationToken), cancellationToken)
            .ConfigureAwait(false);

        // Notify only after a successful write so the message processing can begin in the caller.
        await _channelNotification.Writer.WriteAsync(messageType, cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Read all the messages from the queue.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="cancellationToken">the token used to cancel the operation.</param>
    /// <returns>The items that are ready to be processed.</returns>
    public IAsyncEnumerable<(IMessage Message, MessageMethod Method, MessageContext Context, int RetryCount, CancellationToken Token)>
        ReadAllAsync(string messageType, CancellationToken cancellationToken)
    {
        var channel = _typeChannels.GetOrAdd(messageType, _ =>
            Channel.CreateBounded<(IMessage, MessageMethod, MessageContext, int, CancellationToken)>(
                new BoundedChannelOptions(BoundedChannelCapacity) { FullMode = BoundedChannelFullMode.Wait }));

        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    /// Get the pending message types that are ready to be read.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The message types that are read.</returns>
    public async IAsyncEnumerable<string> GetPendingMessageTypesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var typeName in _channelNotification.Reader.ReadAllAsync(cancellationToken))
        {
            yield return typeName;
        }
    }

    /// <summary>
    /// Completes all channels so no more writing is possible.
    /// </summary>
    public void CompleteAll()
    {
        foreach (var channel in _typeChannels.Values)
        {
            channel.Writer.TryComplete();
        }
        _channelNotification.Writer.TryComplete();
    }
}
