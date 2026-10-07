using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoBus.Abstractions;

namespace NanoBus.Core;

/// <summary>
/// 
/// </summary>
public class MessageHandlerRegistry(ILogger? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<IMessage, MessageContext, CancellationToken, Task>>> _handlers = [];

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void RegisterHandler<T>(IMessageHandler<T> handler) where T : IMessage
    {
        if (handler is null)
            throw new InvalidOperationException("Null handlers are not allowed.");

        var messageType = typeof(T).FullName!;

        var typeHandlers = _handlers.GetOrAdd(messageType, _ => []);
        typeHandlers.Add(async (msg, ctx, ct) => await handler.HandleAsync((T)msg, ctx, ct));

        _logger.LogInformation("Registered handler type '{HandlerType}'", handler.GetType().FullName!);
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
    public ConcurrentBag<Func<IMessage, MessageContext, CancellationToken, Task>> GetHandlers(string messageType)
    {
        if (!_handlers.TryGetValue(messageType, out var typeHandlers))
        {
            _logger.LogError("No handlers registered for type '{MessageType}'", messageType);
            throw new InvalidOperationException($"No handlers registered for type '{messageType}'");
        }

        return typeHandlers;
    }
}
