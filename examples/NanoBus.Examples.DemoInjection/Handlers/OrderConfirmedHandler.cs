using NanoBus.Abstractions;
using NanoBus.Examples.DemoInjection.Messages;

namespace NanoBus.Examples.DemoInjection.Handlers;

public interface IOrderConfirmedHandler : IMessageHandler<OrderConfirmedEvent> { }

public class OrderConfirmedHandler(ILogger<OrderConfirmedHandler> logger) : IOrderConfirmedHandler
{
    private readonly ILogger<OrderConfirmedHandler> _logger = logger;

    public Task HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received MessageId: {MessageId} CorrelationId: {CorrelationId}", context.MessageId, context.CorrelationId);
        _logger.LogInformation("Order confirmed: {OrderNumber} '{CustomerName}' '{OrderDate}' Total: {TotalAmount:C}", message.OrderNumber, message.CustomerName, message.OrderDate, message.TotalAmount);

        return Task.CompletedTask;
    }
}
