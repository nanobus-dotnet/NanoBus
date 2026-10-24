using LocalInMemoryWorker.Models;
using NanoBus.Abstractions;

namespace LocalInMemoryWorker.Messages;

public class CreateSalesOrderCommand : IMessage
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<SalesOrderItem> OrderItems { get; set; } = [];
}