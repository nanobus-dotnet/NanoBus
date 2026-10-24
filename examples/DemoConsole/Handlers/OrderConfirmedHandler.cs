using DemoConsole.Messages;
using NanoBus.Abstractions;

namespace DemoConsole.Handlers;

public class OrderConfirmedHandler : IMessageHandler<OrderConfirmedEvent>
{
    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Received MessageId: {context.MessageId} CorrelationId: {context.CorrelationId}");
        Console.WriteLine($"Order confirmed: {message.OrderNumber} '{message.CustomerName}' '{message.OrderDate}' Total: {message.TotalAmount:C}");

        Console.WriteLine();
        return ValueTask.CompletedTask;
    }
}
