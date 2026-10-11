using DemoConsole.Messages;
using NanoBus.Abstractions;

namespace DemoConsole.Handlers;

public class RetryHandler : IMessageHandler<OrderConfirmedEvent>
{
    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Handler threw an exception");
    }
}
