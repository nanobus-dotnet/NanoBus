using DemoConsole.Messages;
using DemoConsole.Models;

namespace DemoConsole;

public static class SampleData
{
    public static List<Product> GetProducts()
    {
        return
        [
            new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop" },
            new Product { Id = 2, Name = "Mouse", Description = "Wireless mouse" },
            new Product { Id = 3, Name = "Keyboard", Description = "Mechanical keyboard" }
        ];
    }

    public static List<ProductOffer> GetProductOffers()
    {
        return
        [
            new ProductOffer
            {
                Id = 1,
                ProductId = 1,
                Title = "Laptop Spring Sale",
                Price = 899.99m,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30)
            },
            new ProductOffer
            {
                Id = 2,
                ProductId = 2,
                Title = "Mouse Discount",
                Price = 24.99m,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(15)
            },
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

    public static List<SalesOrder> GetSalesOrders()
    {
        return
        [
            new SalesOrder
            {
                Id = 1,
                OrderNumber = "Order-001",
                OrderDate = DateTime.Now.AddDays(-5),
                CustomerName = "John Smith",
                TotalAmount = 949.97m,
                Items =
                [
                    new SalesOrderItem { Id = 1, SalesOrderId = 1, ProductOfferId = 1, Quantity = 1, UnitPrice = 899.99m },
                    new SalesOrderItem { Id = 2, SalesOrderId = 1, ProductOfferId = 2, Quantity = 2, UnitPrice = 24.99m }
                ]
            },
            new SalesOrder
            {
                Id = 2,
                OrderNumber = "Order-002",
                OrderDate = DateTime.Now.AddDays(-2),
                CustomerName = "Jane Doe",
                TotalAmount = 159.98m,
                Items =
                [
                    new SalesOrderItem { Id = 3, SalesOrderId = 2, ProductOfferId = 3, Quantity = 2, UnitPrice = 79.99m }
                ]
            }
        ];
    }

    public static CreateSalesOrderCommand GetCreateSalesOrderCommand(int salesOrderId)
    {
        var order = GetSalesOrders().Single(x => x.Id == salesOrderId);
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
