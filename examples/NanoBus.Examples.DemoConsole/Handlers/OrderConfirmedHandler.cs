using NanoBus.Abstractions;
using NanoBus.Examples.DemoConsole.Messages;

namespace NanoBus.Examples.DemoConsole.Handlers;

public class OrderConfirmedHandler : IMessageHandler<OrderConfirmedEvent>
{
    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Received MessageId: {context.MessageId} CorrelationId: {context.CorrelationId}");
        Console.WriteLine($"Order confirmed: {message.OrderNumber} '{message.CustomerName}' '{message.OrderDate}' Total: {message.TotalAmount:C}");

        Console.WriteLine();
        return ValueTask.CompletedTask;
    }
}
