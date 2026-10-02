using NanoBus.Abstractions;
using NanoBus.Examples.DemoSourceGen.Messages;

namespace NanoBus.Examples.DemoSourceGen.Handlers;

public class RetryHandler(ILogger<RetryHandler> logger) // : IMessageHandler<OrderConfirmedEvent> // (Uncomment to see retries)
{
    private readonly ILogger<RetryHandler> _logger = logger;

    public Task HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
    {
		try
		{
			throw new InvalidOperationException("Handler threw an exception");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in RetryHandler");
			throw;
		}
    }
}
