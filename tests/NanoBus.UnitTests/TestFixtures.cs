using System.Diagnostics.CodeAnalysis;
using NanoBus.Abstractions;

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
