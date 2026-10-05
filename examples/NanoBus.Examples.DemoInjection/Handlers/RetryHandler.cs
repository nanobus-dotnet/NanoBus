using NanoBus.Abstractions;
using NanoBus.Examples.DemoInjection.Messages;

namespace NanoBus.Examples.DemoInjection.Handlers;

public class RetryHandler(ILogger<RetryHandler> logger) : IMessageHandler<OrderConfirmedEvent>
{
    private readonly ILogger<RetryHandler> _logger = logger;

    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken = default)
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
