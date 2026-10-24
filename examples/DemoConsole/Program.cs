using DemoConsole;
using DemoConsole.Handlers;
using NanoBus.Core;
using NanoBus.Transports.Demo;

var transport = new DemoTransport();
transport.RegisterHandler(new CreateSalesOrderHandler());
transport.RegisterHandler(new OrderConfirmedHandler());
// transport.RegisterHandler(new RetryHandler()); // demonstrates exception handling
var bus = new ServiceBus(transport);

foreach (var order in SampleData.GetSalesOrders())
{
    var createOrderCommand = SampleData.GetCreateSalesOrderCommand(order.Id);
    await bus.SendAsync(createOrderCommand);
}
