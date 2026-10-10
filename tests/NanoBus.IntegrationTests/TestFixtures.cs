namespace NanoBus.IntegrationTests;

[ExcludeFromCodeCoverage]
public class TestMessage : IMessage
{
    public string Value { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public class TestMessage2 : IMessage
{
    public int Id { get; set; }
}

[ExcludeFromCodeCoverage]
public class TestMessageHandler : IMessageHandler<TestMessage>
{
    public int HandledCount { get; private set; }
    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken)
    {
        HandledCount++;
        return ValueTask.CompletedTask;
    }
}

[ExcludeFromCodeCoverage]
public class TestMessageHandler2 : IMessageHandler<TestMessage>
{
    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

[ExcludeFromCodeCoverage]
public class TestMessage2Handler : IMessageHandler<TestMessage2>
{
    public ValueTask HandleAsync(TestMessage2 message, MessageContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

[ExcludeFromCodeCoverage]
public class AlwaysFailsHandler : IMessageHandler<TestMessage>
{
    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken = default)
    {
        throw new Exception("Failed");
    }
}

[ExcludeFromCodeCoverage]
public class FailsOnceHandler : IMessageHandler<TestMessage>
{
    public int Attempts { get; private set; } = 0;
    public int FailedAttempts { get; private set; } = 0;

    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken = default)
    {
        if (Attempts++ == 0)
        {
            FailedAttempts = 1;
            throw new Exception("Failed");
        }

        return ValueTask.CompletedTask;
    }
}


[ExcludeFromCodeCoverage]
public class MarkDeliveredHandler : IMessageHandler<TestMessage>
{
    public bool Delivered { get; private set; } = false;

    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken = default)
    {
        Delivered = true;
        return ValueTask.CompletedTask;
    }
}

[ExcludeFromCodeCoverage]
public class SupplyContextHandler : IMessageHandler<TestMessage>
{
    public MessageContext? MessageContext { get; private set; }
    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken = default)
    {
        MessageContext = context;
        return ValueTask.CompletedTask;
    }
}

[ExcludeFromCodeCoverage]
public class TestWaitHandler : IMessageHandler<TestMessage>
{
    private TaskCompletionSource<bool> _handleTcs = new();
    public int HandledCount { get; private set; }
    public int FailCount { get; private set; }

    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken = default)
    {
        HandledCount++;
        _handleTcs.TrySetResult(true);
        return ValueTask.CompletedTask;
    }

    public async Task WaitHandledAsync(TimeSpan? timeout = null)
    {
        try
        {
            await _handleTcs.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Handler was not invoked within the timeout period.");
        }
        finally
        {
            // Reset for next wait
            _handleTcs = new TaskCompletionSource<bool>();
        }
    }
}



[ExcludeFromCodeCoverage]
public static class Helpers
{
    private static TimeSpan GetBackoffDelay(int attemptNumber)
    {
        var secondsDelay = Math.Min((int)Math.Pow(2, attemptNumber), 15);
        return TimeSpan.FromSeconds(secondsDelay);
    }

    public static TimeSpan GetTotalBackoffDelay(int maxAttempts)
    {
        var totalDelay = TimeSpan.FromSeconds(0);

        do
        {
            totalDelay += GetBackoffDelay(maxAttempts);
        }
        while (--maxAttempts >= 0);

        return totalDelay;
    }
}