using NanoBus.Abstractions;
using NanoBus.Examples.DemoInjection.Messages;

namespace NanoBus.Examples.DemoInjection.Handlers;

public interface IRetryHandler : IMessageHandler<OrderConfirmedEvent> { }

public class RetryHandler(ILogger<RetryHandler> logger) : IRetryHandler
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
