using NanoBus.Abstractions;

namespace LocalInMemoryWorker.Messages;

public class OrderConfirmedEvent : IMessage
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}
