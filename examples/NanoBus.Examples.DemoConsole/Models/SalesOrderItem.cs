namespace NanoBus.Examples.DemoConsole.Models;

public class SalesOrderItem
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductOfferId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
