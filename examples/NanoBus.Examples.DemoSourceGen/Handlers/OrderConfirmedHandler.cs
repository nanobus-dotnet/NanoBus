using NanoBus.Abstractions;
using NanoBus.Examples.DemoSourceGen.Messages;

namespace NanoBus.Examples.DemoSourceGen.Handlers;

public class OrderConfirmedHandler(ILogger<OrderConfirmedHandler> logger) : IMessageHandler<OrderConfirmedEvent>
{
    private readonly ILogger<OrderConfirmedHandler> _logger = logger;

    public Task HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received MessageId: {MessageId} CorrelationId: {CorrelationId}", context.MessageId, context.CorrelationId);
        _logger.LogInformation("Order confirmed: {OrderNumber} '{CustomerName}' '{OrderDate}' Total: {TotalAmount:C}", message.OrderNumber, message.CustomerName, message.OrderDate, message.TotalAmount);

        return Task.CompletedTask;
    }
}
