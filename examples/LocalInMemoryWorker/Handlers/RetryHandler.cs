using LocalInMemoryWorker.Messages;
using NanoBus.Abstractions;

namespace LocalInMemoryWorker.Handlers;

// Uncomment for periodic failure and DLQ retries.
public class RetryHandler(ILogger<RetryHandler> logger) : IMessageHandler<OrderConfirmedEvent>
{
    private readonly ILogger<RetryHandler> _logger = logger;

    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken)
    {
		try
		{
            // Fail on average every 5th time. Comment out to show full DLQ retry scenario.
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
