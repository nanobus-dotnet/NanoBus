using NanoBus.Abstractions;
using NanoBus.Examples.DemoConsole.Messages;

namespace NanoBus.Examples.DemoConsole.Handlers;

public class CreateSalesOrderHandler : IMessageHandler<CreateSalesOrderCommand>
{
    public async ValueTask HandleAsync(CreateSalesOrderCommand message, MessageContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Received MessageId: {context.MessageId} CorrelationId: {context.CorrelationId}");
        Console.WriteLine($"Creating sales order: {message.OrderNumber} '{message.CustomerName}' '{message.OrderDate}' Total: {message.TotalAmount:C}");

        foreach (var orderItem in message.OrderItems)
        {
            var offer = SampleData.GetProductOffers().Single(x => x.Id == orderItem.ProductOfferId);

            Console.WriteLine($"    {offer.Title} {offer.Price:C}");

            var product = SampleData.GetProducts().Single(x => x.Id == offer.ProductId);

            Console.WriteLine($"        {product.Name} - {product.Description} - Quantity: {orderItem.Quantity}");
        }

        Console.WriteLine();

        // Save to database

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
