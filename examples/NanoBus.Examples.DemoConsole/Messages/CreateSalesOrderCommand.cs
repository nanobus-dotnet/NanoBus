using NanoBus.Abstractions;
using NanoBus.Examples.DemoConsole.Models;

namespace NanoBus.Examples.DemoConsole.Messages;

public class CreateSalesOrderCommand : IMessage
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<SalesOrderItem> OrderItems { get; set; } = [];
}