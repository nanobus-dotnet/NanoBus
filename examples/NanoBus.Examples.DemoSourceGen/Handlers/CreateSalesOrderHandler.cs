using NanoBus.Abstractions;
using NanoBus.Examples.DemoSourceGen.Messages;

namespace NanoBus.Examples.DemoSourceGen.Handlers;

public class CreateSalesOrderHandler(ILogger<CreateSalesOrderHandler> logger) : IMessageHandler<CreateSalesOrderCommand>
{
    private readonly ILogger<CreateSalesOrderHandler> _logger = logger;

    public async ValueTask HandleAsync(CreateSalesOrderCommand message, MessageContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received MessageId: {MessageId} CorrelationId: {CorrelationId}", context.MessageId, context.CorrelationId);
        _logger.LogInformation("Creating sales order: {OrderNumber} '{CustomerName}' {OrderDate} Total: {TotalAmount:C}", message.OrderNumber, message.CustomerName, message.OrderDate, message.TotalAmount);

        foreach (var orderItem in message.OrderItems)
        {
            var offer = SampleData.GetProductOffers().Single(x => x.Id == orderItem.ProductOfferId);

            _logger.LogInformation("  {offer.Title} {offer.Price:C}", offer.Title, offer.Price);

            var product = SampleData.GetProducts().Single(x => x.Id == offer.ProductId);

            _logger.LogInformation("    {Name} - {Description} - Quantity: {Quantity}", product.Name, product.Description, orderItem.Quantity);
        }

        // Save to database

        _logger.LogInformation("Created sales order: {OrderNumber} '{CustomerName}' {OrderDate} Total: {TotalAmount:C}", message.OrderNumber, message.CustomerName, message.OrderDate, message.TotalAmount);

        // Publish OrderConfirmedEvent

        await context.PublishAsync(new OrderConfirmedEvent
        {
            OrderId = message.Id,
            OrderNumber = message.OrderNumber,
            CustomerName = message.CustomerName,
            OrderDate = message.OrderDate,
            TotalAmount = message.TotalAmount
        }, new MessageMetadata
        {
            CorrelationId = context.CorrelationId
        }, cancellationToken);
    }
}
