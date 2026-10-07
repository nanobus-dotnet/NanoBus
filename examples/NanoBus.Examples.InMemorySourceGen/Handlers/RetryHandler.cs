using NanoBus.Abstractions;
using NanoBus.Examples.InMemorySourceGen.Messages;

namespace NanoBus.Examples.InMemorySourceGen.Handlers;

public class RetryHandler(ILogger<RetryHandler> logger) : IMessageHandler<OrderConfirmedEvent>
{
    private readonly ILogger<RetryHandler> _logger = logger;

    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
    {
		try
		{
            // Fail on average every 5th time.
            if (new Random().Next(5) % 5 == 0)
                throw new InvalidOperationException("Handler threw an exception");

			return ValueTask.CompletedTask;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in RetryHandler");
			throw;
		}
    }
}
