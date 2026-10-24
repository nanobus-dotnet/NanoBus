using ClientLibrary.Messages;
using Microsoft.Extensions.Logging;
using NanoBus.Abstractions;

namespace ClientLibrary.Handlers;

public class FailsEveryTimeHandler(ILogger<FailsEveryTimeHandler> logger) // : IMessageHandler<OrderConfirmedEvent> // uncomment to show DLQ exception handling
{
    private readonly ILogger<FailsEveryTimeHandler> _logger = logger;

    public ValueTask HandleAsync(OrderConfirmedEvent message, MessageContext context, CancellationToken cancellationToken)
    {
		try
		{
			throw new InvalidOperationException("FailsEveryTimeHandler threw an exception");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in FailsEveryTimeHandler");
			throw;
		}
    }
}
