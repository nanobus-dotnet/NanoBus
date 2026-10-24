using DemoConsole.Messages;
using NanoBus.Abstractions;

namespace DemoConsole.Handlers;

public class RetryHandler : IMessageHandler<OrderConfirmedEvent>
{
    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken)
    {
		try
		{
			throw new InvalidOperationException("RetryHandler threw an exception");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"------ {ex.Message} {ex.StackTrace} ------");
			throw;
		}
    }
}
