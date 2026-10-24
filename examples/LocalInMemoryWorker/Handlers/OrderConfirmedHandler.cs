using LocalInMemoryWorker.Messages;
using NanoBus.Abstractions;

namespace LocalInMemoryWorker.Handlers;

public class OrderConfirmedHandler(ILogger<OrderConfirmedHandler> logger) : IMessageHandler<OrderConfirmedEvent>
{
    private readonly ILogger<OrderConfirmedHandler> _logger = logger;

    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received MessageId: {MessageId} CorrelationId: {CorrelationId}", context.MessageId, context.CorrelationId);
        _logger.LogInformation("Order confirmed: {OrderNumber} '{CustomerName}' '{OrderDate}' Total: {TotalAmount:C}", message.OrderNumber, message.CustomerName, message.OrderDate, message.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
