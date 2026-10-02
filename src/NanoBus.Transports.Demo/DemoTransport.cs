using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;

namespace NanoBus.Transports.Demo;

/// <summary>
/// Transport used for demoing learning NanoBus concepts. Handlers are added directly and  no physical transports are used.
/// </summary>
/// <param name="options">The options used.</param>
/// <param name="logger">The logger.</param>
public class DemoTransport(TransportOptions? options = null, ILogger<DemoTransport>? logger = null) : ITransport
{
    private readonly TransportOptions _options = options ?? new();
    private readonly ILogger<DemoTransport> _logger = logger ?? NullLogger<DemoTransport>.Instance;

    /// <summary>
    /// Holds references to all handler methods by type name.
    /// </summary>
    protected readonly ConcurrentDictionary<string, ConcurrentBag<Func<IMessage, MessageContext, CancellationToken, Task>>> _handlers = [];

    /// <inheritdoc />
    public void RegisterHandler<T>(IMessageHandler<T> handler) where T : IMessage
    {
        if (handler is null)
            throw new InvalidOperationException("Null handlers are not allowed.");

        var messageType = typeof(T).FullName!;

        var typeHandlers = _handlers.GetOrAdd(messageType, _ => []);
        typeHandlers.Add(async (msg, ctx, ct) => await handler.HandleAsync((T)msg, ctx, ct));

        _logger.LogInformation("Registered handler type '{HandlerType}'", handler.GetType().FullName!);

    }
    /// <inheritdoc />
    public async Task PublishAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        var typeName = message.GetType().FullName!;

        if (!_handlers.TryGetValue(typeName, out var typeHandlers))
        {
            _logger.LogError("No handlers registered for type '{MessageType}'", typeName);
            throw new InvalidOperationException($"No handlers registered for type '{typeName}'");
        }

        var context = new MessageContext(this, metadata?.MessageId, metadata?.CorrelationId, metadata?.Headers);


        foreach (var handler in typeHandlers)
        {
            var retries = _options.MaxRetries;

            while (retries >= 0)
            {
                try
                {
                    await handler(message, context, cancellationToken)
                        .ConfigureAwait(false);

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error delivering message - MessageId: {MessageId} CorrelationId: {CorrelationId}. {Retries} retries left.", context.MessageId, context.CorrelationId, retries--);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        var typeName = message.GetType().FullName!;

        if (!_handlers.TryGetValue(typeName, out var typeHandlers))
        {
            _logger.LogError("No handlers registered for type '{MessageType}'", typeName);
            throw new InvalidOperationException($"No handlers registered for type '{typeName}");
        }

        if (typeHandlers.Count > 1)
        {
            _logger.LogWarning("More than one handler registered for message type '{MessageType}'. Only the last handler registered will receive the message.", typeName);
        }

        var context = new MessageContext(this, metadata?.MessageId, metadata?.CorrelationId, metadata?.Headers);

        var retries = _options.MaxRetries;

        while (retries >= 0)
        {
            try
            {
                await typeHandlers.Last()(message, context, cancellationToken)
                    .ConfigureAwait(false);

                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering message - MessageId: {MessageId} CorrelationId: {CorrelationId}. {Retries} retries left.", context.MessageId, context.CorrelationId, retries--);
            }
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
