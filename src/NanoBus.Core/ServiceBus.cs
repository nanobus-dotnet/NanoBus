using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;

namespace NanoBus.Core;

/// <summary>
/// Implementation of the ServiceBus.
/// </summary>
/// <param name="transport">The given transport to use.</param>
/// <param name="logger">The logger.</param>
public class ServiceBus(ITransport transport, ILogger<ServiceBus>? logger = null) : IServiceBus
{
    private readonly ITransport _transport = transport;
    private readonly ILogger<ServiceBus> _logger = logger ?? NullLogger<ServiceBus>.Instance;
    private bool _isStarted;

    /// <inheritdoc />
    public async Task PublishAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        metadata ??= new MessageMetadata();

        await _transport.PublishAsync(message, metadata, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Message published - MessageId: {MessageId} CorrelationId: {CorrelationId}", metadata.MessageId, metadata.CorrelationId);
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        metadata ??= new MessageMetadata();
        await _transport.SendAsync(message, metadata, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Message sent - MessageId: {MessageId} CorrelationId: {CorrelationId}", metadata.MessageId, metadata.CorrelationId);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            _logger.LogError("Service Bus already started.");
            return;
        }

        _isStarted = true;
        await _transport.StartAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Service Bus started.");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            _logger.LogError("Service Bus already stopped.");
            return;
        }
        _isStarted = false;

        await _transport.StopAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Service Bus stopped.");
    }
}
