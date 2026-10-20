using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;
using NanoBus.Core;

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
    private readonly MessageHandlerRegistry _handlerRegistry = new(logger);

    /// <inheritdoc />
    public void RegisterHandler<T>(IMessageHandler<T> handler) where T : IMessage
    {
        _handlerRegistry.RegisterHandler(handler);
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        var messageType = message.GetType().FullName!;

        var context = new MessageContext(this, metadata?.MessageId, metadata?.CorrelationId, metadata?.Headers);


        foreach (var handler in _handlerRegistry.GetHandlers(messageType))
        {
            var attempts = 1;

            while (attempts <= _options.MaxDeliveryAttempts)
            {
                try
                {
                    await handler(message, context, cancellationToken)
                        .ConfigureAwait(false);

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error delivering message - MessageId: {MessageId} CorrelationId: {CorrelationId}. {Retries} retries left.", context.MessageId, context.CorrelationId, attempts++);
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask SendAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        var messageType = message.GetType().FullName!;

        var typeHandlers = _handlerRegistry.GetHandlers(messageType);

        if (typeHandlers.Count > 1)
        {
            _logger.LogWarning("More than one handler registered for sent message type '{MessageType}'. Only the first handler registered will receive the message.", messageType);
        }

        var context = new MessageContext(this, metadata?.MessageId, metadata?.CorrelationId, metadata?.Headers);

        var attempts = 1;

        while (attempts <= _options.MaxDeliveryAttempts)
        {
            try
            {
                await typeHandlers[0](message, context, cancellationToken)
                    .ConfigureAwait(false);

                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering message - MessageId: {MessageId} CorrelationId: {CorrelationId}. {Retries} retries left.", context.MessageId, context.CorrelationId, attempts++);
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

    /// <inheritdoc />
    public ValueTask<IEnumerable<FailedMessage>> GetDeadLetterMessagesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IEnumerable<FailedMessage>>([]);
    }

    /// <inheritdoc />
    public ValueTask ReprocessDeadLetterQueueAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ReprocessDeadLetterMessageAsync(string uniqueId, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<FailedMessage>> GetFailedDeadLetterMessagesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IEnumerable<FailedMessage>>([]);
    }

    /// <inheritdoc />
    public Task ProcessDeadLetterQueueAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearDeadLetterQueueAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
