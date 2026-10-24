using DlqWorker.Messages;
using NanoBus.Abstractions;

namespace DlqWorker.Handlers;

public class FailsEveryTimeHandler(ILogger<FailsEveryTimeHandler> logger) : IMessageHandler<OrderConfirmedEvent>
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
