using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;

namespace NanoBus.Transports.InMemory;


/// <summary>
/// In-memory transport which uses Channels rather than physical transports.
/// </summary>
public partial class InMemoryTransport : ITransport
{
    private readonly InMemoryTransportOptions _options;
    private readonly ILogger<InMemoryTransport> _logger;


    private readonly ChannelManager _channelManager;

    /// <summary>
    /// Stores the message handlers by message type.
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<IMessage, MessageContext, CancellationToken, Task>>> _handlers = [];

    private readonly DeadLetterQueueManager _dlqManager;

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
    private readonly CancellationTokenSource _tokenSource = new();

    /// <param name="options">The options used.</param>
    /// <param name="logger">The logger.</param>
    public InMemoryTransport(InMemoryTransportOptions? options = null, ILogger<InMemoryTransport>? logger = null)
    {
        _options = options ?? new();
        _logger = logger ?? NullLogger<InMemoryTransport>.Instance;
        _channelManager = new(_options.MaxChannelCapacity);
        _dlqManager = new(_logger) { MaxRetries = _options.MaxRetries };
    }


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
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Start the background task to deliver messages.
        _messageDeliveryTask = DeliverMessagesAsync(_tokenSource.Token);

        // Start the background task to retry failed messages.
        _errorDeliveryTask = RetryFailedMessagesAsync(_tokenSource.Token);

        return ValueTask.CompletedTask;
    }


    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
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
    private async Task WriteToChannel<T>(T message, MessageMethod method, MessageMetadata? metadata, CancellationToken cancellationToken) where T : IMessage
    {
        var messageType = message.GetType().FullName!;

        if (!_handlers.ContainsKey(messageType))
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
            if (!_handlers.TryGetValue(messageType, out var handlers))
            {
                _logger.LogWarning("No handlers found for message type '{MessageType}'", messageType);
                continue;
            }

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
                var backoffDelay = DeadLetterQueueManager.GetBackoffDelay(retryCount);
                _logger.LogError(ex, "Delivery failed for MessageId: {MessageId} - Correlation Id: {CorrelationId} - MessageType: '{MessageType}'. Retrying in {Delay}", context.MessageId, context.CorrelationId, messageType, backoffDelay);

                _dlqManager.Enqueue(message, messageType, method, context, token, ex);
            }
        }
    }


    /// <summary>
    /// Retries failed messages in the dead letter queue.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the task.</param>
    /// <returns>The retry task.</returns>
    private async Task RetryFailedMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Retrieve messages from the DLQ that are ready to be retried.
                var readyMessages = _dlqManager.GetMessagesForRetry();

                foreach (var (id, messageType, message, method, context, retryCount, nextRetryTime, _, token) in readyMessages)
                {
                    if (!_handlers.TryGetValue(messageType, out var handlers))
                    {
                        _logger.LogWarning("No handlers found for message type '{MessageType}'", messageType);
                        continue;
                    }

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

                        _dlqManager.Remove(id);
                    }
                    catch (Exception ex)
                    {
                        _dlqManager.UpdateMessageRetry(id, retryCount + 1, ex);
                    }
                }

                // Check every second.
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException) // expected exception.
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error occurred delivering failed messages.");
        }
    }
}
