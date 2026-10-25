using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;

namespace NanoBus.Core;

/// <summary>
/// Registry for all message handlers implementing IMessageHandler.
/// </summary>
public class MessageHandlerRegistry(ILogger? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    private readonly ConcurrentDictionary<string, ConcurrentBag<(string HandlerType, Func<IMessage, MessageContext, CancellationToken, ValueTask> Handler)>> _handlers = [];

    /// <summary>
    /// Registers the handler of a given message type.
    /// </summary>
    /// <typeparam name="T">The message type being handled.</typeparam>
    /// <param name="handler">The handler of the message.</param>
    public void RegisterHandler<T>(IMessageHandler<T> handler) where T : IMessage
    {
        var messageType = typeof(T).FullName!;
        var handlerType = handler.GetType().FullName!;

        async ValueTask handleMethod(IMessage msg, MessageContext ctx, CancellationToken ct)
            => await handler.HandleAsync((T)msg, ctx, ct);

        var typeHandlers = _handlers.GetOrAdd(messageType, _ => []);
        typeHandlers.Add((handlerType, handleMethod));
        _logger.LogInformation("Registered handler type '{HandlerType}'", handlerType);
    }

    /// <summary>
    /// Determines if a handler exists for a given message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>True if the handler exists, otherwise false.</returns>
    public bool HandlerExists(string messageType)
    {
        return _handlers.ContainsKey(messageType);
    }

    /// <summary>
    /// Gets handlers for a given message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The collection of handlers that handle the message type.</returns>
    public IReadOnlyList<(string HandlerType, Func<IMessage, MessageContext, CancellationToken, ValueTask> Handler)> GetHandlers(string messageType)
    {
        if (!_handlers.TryGetValue(messageType, out var typeHandlers))
        {
            _logger.LogError("No handlers registered for type '{MessageType}'", messageType);
            throw new InvalidOperationException($"No handlers registered for type '{messageType}'");
        }

        return [.. typeHandlers];
    }
}
