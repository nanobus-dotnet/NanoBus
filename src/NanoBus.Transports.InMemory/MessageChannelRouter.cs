using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using NanoBus.Abstractions;

namespace NanoBus.Transports.InMemory;


/// <summary>
/// Routes messages to channels for delivery and query.
/// </summary>
public class MessageChannelRouter(int boundedChannelCapacity = 100) : IMessageChannelRouter
{
    private readonly ConcurrentDictionary<string, Channel<(IMessage Message, MessageMethod Method, MessageContext Context, CancellationToken InvokerToken)>> _typeChannels = new();
    private readonly Channel<string> _channelNotification = Channel.CreateUnbounded<string>();

    /// <inheritdoc />
    public int BoundedChannelCapacity { get; } = boundedChannelCapacity;

    /// <inheritdoc />
    public async ValueTask<bool> EnqueueMessageAsync(string messageType, IMessage message, MessageMethod method, 
        MessageContext context, CancellationToken invokerToken)
    {
        var channel = _typeChannels.GetOrAdd(messageType, _ =>
            Channel.CreateBounded<(IMessage, MessageMethod, MessageContext, CancellationToken)>(
                new BoundedChannelOptions(BoundedChannelCapacity) { FullMode = BoundedChannelFullMode.Wait }));

        if (!await channel.Writer.WaitToWriteAsync(invokerToken)
            .ConfigureAwait(false))
        {
            return false; // The channel is closed.
        }

        await channel.Writer.WriteAsync((message, method, context, invokerToken), invokerToken)
            .ConfigureAwait(false);

        // Notify only after a successful write so the message processing can begin in the caller.
        await _channelNotification.Writer.WriteAsync(messageType, invokerToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<(IMessage Message, MessageMethod Method, MessageContext Context, CancellationToken InvokerToken)>
        ReadAllAsync(string messageType, CancellationToken cancellationToken = default)
    {
        if (!_typeChannels.TryGetValue(messageType, out var channel))
            throw new InvalidOperationException($"No Channel for message type: '{messageType}'");

        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetPendingMessageTypesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var typeName in _channelNotification.Reader.ReadAllAsync(cancellationToken))
        {
            yield return typeName;
        }
    }

    /// <inheritdoc />
    public void CompleteAll()
    {
        foreach (var channel in _typeChannels.Values)
        {
            channel.Writer.TryComplete();
        }
        _channelNotification.Writer.TryComplete();
    }
}
