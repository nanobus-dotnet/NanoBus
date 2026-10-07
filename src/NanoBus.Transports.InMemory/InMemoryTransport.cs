using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;
using NanoBus.Core;

namespace NanoBus.Transports.InMemory;

/// <summary>
/// In-memory transport which uses Channels rather than physical transports.
/// </summary>
public partial class InMemoryTransport : ITransport
{
    private readonly InMemoryTransportOptions _options;
    private readonly ILogger<InMemoryTransport> _logger;

    /// <summary>
    /// The manager of message delivery channels.
    /// </summary>
    private readonly ChannelManager _channelManager;

    /// <summary>
    /// The queue where undeliverable messages are stored.
    /// </summary>
    private readonly IDeadLetterQueue _deadLetterQueue;

    /// <summary>
    /// The background task that delivers messages.
    /// </summary>
    private Task? _messageDeliveryTask;

    /// <summary>
    /// The background task that redelivers messages from the DLQ.
    /// </summary>
    private Task? _errorDeliveryTask;

    /// <summary>
    /// The token source used to cancel the background message delivery and DLQ tasks.
    /// </summary>
    private readonly CancellationTokenSource _tokenSource;

    /// <summary>
    /// The registry where message handlers are stored.
    /// </summary>
    private readonly MessageHandlerRegistry _handlerRegistry;


    /// <param name="deadLetterQueue">The dead letter queue.</param>
    /// <param name="options">The options used.</param>
    /// <param name="logger">The logger.</param>
    public InMemoryTransport(IDeadLetterQueue deadLetterQueue, InMemoryTransportOptions? options = null, ILogger<InMemoryTransport>? logger = null)
    {
        _options = options ?? new();
        _logger = logger ?? NullLogger<InMemoryTransport>.Instance;
        _channelManager = new(_options.MaxChannelCapacity);
        _deadLetterQueue = deadLetterQueue;
        _handlerRegistry = new MessageHandlerRegistry(logger);
        _tokenSource = new CancellationTokenSource();
    }

    /// <inheritdoc />
    public void RegisterHandler<T>(IMessageHandler<T> handler) where T : IMessage
    {
        _handlerRegistry.RegisterHandler(handler);
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        await WriteToChannel(message, MessageMethod.Publish, metadata, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SendAsync<T>(T message, MessageMetadata? metadata = null, CancellationToken cancellationToken = default) where T : IMessage
    {
        await WriteToChannel(message, MessageMethod.Send, metadata, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Start the background task to deliver messages.
        _messageDeliveryTask = DeliverMessagesAsync(_tokenSource.Token);

        // Start the background task to retry failed messages.
        _errorDeliveryTask = ProcessDeadLetterQueueAsync(_tokenSource.Token);
    }


    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _tokenSource.CancelAsync()
            .ConfigureAwait(false);

        _channelManager.CompleteAll();

        // Await both tasks.
        if (_messageDeliveryTask != null)
            await _messageDeliveryTask
                .ConfigureAwait(false);

        if (_errorDeliveryTask != null)
            await _errorDeliveryTask
                .ConfigureAwait(false);
    }


    /// <summary>
    /// Writes the message to its channel.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message.</param>
    /// <param name="method">The delivery method, either Send or Publish.</param>
    /// <param name="metadata">The message metadata.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>The task to write to the message channel.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async ValueTask WriteToChannel<T>(T message, MessageMethod method, MessageMetadata? metadata, CancellationToken cancellationToken) where T : IMessage
    {
        var messageType = message.GetType().FullName!;

        if (!_handlerRegistry.HandlerExists(messageType))
        {
            _logger.LogError("No handlers registered for type '{MessageType}'", messageType);
            throw new InvalidOperationException($"No handlers registered for type '{messageType}'");
        }

        // Send the message to its channel.

        var context = new MessageContext(this, metadata?.MessageId, metadata?.CorrelationId, metadata?.Headers);

        await _channelManager.EnqueueMessageAsync(messageType, message, method, context, 0, cancellationToken)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Deliver all messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task to deliver all messages.</returns>
    private async Task DeliverMessagesAsync(CancellationToken cancellationToken)
    {
        var processingTasks = new Dictionary<string, Task>();

        try
        {
            // Listen for notifications of new message types.
            await foreach (var messageType in _channelManager.GetPendingMessageTypesAsync(cancellationToken))
            {
                // Only spawn one processing task per type if not already running.
                if (!processingTasks.ContainsKey(messageType))
                {
                    var task = ProcessChannelAsync(messageType, cancellationToken)
                        .ContinueWith(_ => processingTasks.Remove(messageType), cancellationToken);
                    processingTasks[messageType] = task;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in the message delivery process.");
        }

        // Wait for all processing tasks to complete.
        try
        {
            await Task.WhenAll(processingTasks.Values).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }


    /// <summary>
    /// Processes the channel by message type.
    /// </summary>
    /// <param name="messageType">The message type to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task to process the channel.</returns>
    private async Task ProcessChannelAsync(string messageType, CancellationToken cancellationToken)
    {
        // Read all messages in the channel.
        await foreach (var (message, method, context, retryCount, token) in _channelManager.ReadAllAsync(messageType, cancellationToken))
        {
            var handlers = _handlerRegistry.GetHandlers(messageType);

            // Invoke the handlers for each message.
            try
            {
                switch (method)
                {
                    case MessageMethod.Send:
                        if (handlers.Count > 1)
                            _logger.LogWarning("More than one handler for sent message type '{MessageType}'", messageType);

                        await handlers.First().Invoke(message, context, token)
                            .ConfigureAwait(false);

                        break;

                    case MessageMethod.Publish:
                        var publishTasks = handlers.Select(h => h.Invoke(message, context, token)).ToList();

                        await Task.WhenAll(publishTasks)
                            .ConfigureAwait(false);
                        break;
                }

            }
            catch (Exception ex)
            {
                await _deadLetterQueue.EnqueueAsync(message, messageType, method, context, DateTime.UtcNow, ex, token)
                    .ConfigureAwait(false);
            }
        }
    }


    /// <summary>
    /// Retries failed messages in the dead letter queue.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the task.</param>
    /// <returns>The retry task.</returns>
    public async Task ProcessDeadLetterQueueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Retrieve messages from the DLQ that are ready to be retried.
                var readyMessages = await _deadLetterQueue.GetMessagesForRetryAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var (id, messageType, message, method, context, retryCount, nextRetryTime, _, _, token) in readyMessages)
                {
                    var handlers = _handlerRegistry.GetHandlers(messageType);

                    try
                    {
                        // Attempt redelivery.
                        switch (method)
                        {
                            case MessageMethod.Send:
                                if (handlers.Count > 1)
                                    _logger.LogWarning("More than one handler for sent message type '{MessageType}'", messageType);

                                await handlers.First().Invoke(message, context, token)
                                    .ConfigureAwait(false);

                                break;

                            case MessageMethod.Publish:
                                var publishTasks = handlers.Select(h => h.Invoke(message, context, token)).ToList();

                                await Task.WhenAll(publishTasks)
                                    .ConfigureAwait(false);
                                break;
                        }

                        await _deadLetterQueue.RemoveAsync(id, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await _deadLetterQueue.UpdateMessageRetryAsync(id, retryCount, ex, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                // Check every second.
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected exception.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error occurred delivering failed messages.");
        }
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<FailedMessage>> GetDeadLetterMessagesAsync(CancellationToken cancellationToken = default) 
        => await _deadLetterQueue.GetAllMessagesAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask ReprocessDeadLetterQueueAsync(CancellationToken cancellationToken = default)
    {
        foreach (var failedMessage in await _deadLetterQueue.GetAllMessagesAsync(cancellationToken)
            .ConfigureAwait(false))
        {
            await _deadLetterQueue.UpdateMessageRetryAsync(failedMessage.UniqueId, 0, null, cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("All DLQ messages reprocessed.");
    }

    /// <inheritdoc />
    public async ValueTask ReprocessDeadLetterMessageAsync(string uniqueId, CancellationToken cancellationToken = default)
    {
        var message = await _deadLetterQueue.GetMessageAsync(uniqueId, cancellationToken)
            .ConfigureAwait(false);

        if (message is null)
            return;

        var context = message.Context;

        await _deadLetterQueue.UpdateMessageRetryAsync(message.UniqueId, 0, null, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Reprocessed failed DLQ message - {UniqueId} - MessageId: {MessageId} - CorrelationId: {CorrelationId}", uniqueId, context.MessageId, context.CorrelationId);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<FailedMessage>> GetFailedDeadLetterMessagesAsync(CancellationToken cancellationToken = default)
        => await _deadLetterQueue.GetAllFailedMessagesAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask ClearDeadLetterQueueAsync(CancellationToken cancellationToken = default)
    {
        await _deadLetterQueue.ClearQueueAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
