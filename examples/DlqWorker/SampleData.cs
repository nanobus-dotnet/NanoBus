using DlqWorker.Messages;
using DlqWorker.Models;

namespace DlqWorker;

public static class SampleData
{
    public static List<Product> GetProducts()
    {
        return
        [
            new Product { Id = 3, Name = "Keyboard", Description = "Mechanical keyboard" }
        ];
    }

    public static List<ProductOffer> GetProductOffers()
    {
        return
        [
            new ProductOffer
            {
                Id = 3,
                ProductId = 3,
                Title = "Keyboard Bundle",
                Price = 79.99m,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(60)
            }
        ];
    }

    public static CreateSalesOrderCommand GetCreateSalesOrderCommand()
    {
        var order = new SalesOrder
        {
            Id = 2,
            OrderNumber = "Order-002",
            OrderDate = DateTime.Now.AddDays(-2),
            CustomerName = "Catherine Stevens",
            TotalAmount = 159.98m,
            Items =
            [
                new SalesOrderItem { Id = 3, SalesOrderId = 2, ProductOfferId = 3, Quantity = 2, UnitPrice = 79.99m }
            ]
        };

        return new CreateSalesOrderCommand
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            CustomerName = order.CustomerName,
            TotalAmount = order.TotalAmount,
            OrderItems = order.Items
        };
    }
}
