namespace NanoBus.UnitTests;

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
    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken)
    {
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
public static class MoqLoggerExtensions
{
    public static void VerifyMessageEquals<T>(this Mock<ILogger<T>> mockLogger,string message, LogLevel level, Func<Times> times) where T : class
    {
        mockLogger.Verify(
            l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals(message)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    public static void VerifyMessageContains<T>(this Mock<ILogger<T>> mockLogger, string message, LogLevel level, Func<Times> times) where T : class
    {
        mockLogger.Verify(
            l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}