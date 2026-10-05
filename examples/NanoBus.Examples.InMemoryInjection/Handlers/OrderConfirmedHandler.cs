using NanoBus.Abstractions;
using NanoBus.Examples.InMemoryInjection.Messages;

namespace NanoBus.Examples.InMemoryInjection.Handlers;

public class OrderConfirmedHandler(ILogger<OrderConfirmedHandler> logger) : IMessageHandler<OrderConfirmedEvent>
{
    private readonly ILogger<OrderConfirmedHandler> _logger = logger;

    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received MessageId: {MessageId} CorrelationId: {CorrelationId}", context.MessageId, context.CorrelationId);
        _logger.LogInformation("Order confirmed: {OrderNumber} '{CustomerName}' '{OrderDate}' Total: {TotalAmount:C}", message.OrderNumber, message.CustomerName, message.OrderDate, message.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
