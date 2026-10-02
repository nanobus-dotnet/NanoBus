using NanoBus.Abstractions;
using NanoBus.Examples.DemoConsole.Messages;

namespace NanoBus.Examples.DemoConsole.Handlers;

public class RetryHandler : IMessageHandler<OrderConfirmedEvent>
{
    public Task HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Handler threw an exception");
    }
}
