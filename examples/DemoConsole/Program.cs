using DemoConsole;
using DemoConsole.Handlers;
using NanoBus.Core;
using NanoBus.Transports.Demo;

var transport = new DemoTransport();
var createOrderHandler = new CreateSalesOrderHandler();
var orderConfirmedHandler = new OrderConfirmedHandler();
transport.RegisterHandler(createOrderHandler);
transport.RegisterHandler(orderConfirmedHandler);
var bus = new ServiceBus(transport);

foreach (var order in SampleData.GetSalesOrders())
{
    var createOrderCommand = SampleData.GetCreateSalesOrderCommand(order.Id);
    await bus.SendAsync(createOrderCommand);
}
