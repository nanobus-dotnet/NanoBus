using System.Threading;
using System.Threading.Tasks;
using NanoBus.Abstractions;

namespace NanoBus.Benchmarks;

public class NanoBusTestMessageHandler : IMessageHandler<TestMessage>
{
    public ValueTask HandleAsync(TestMessage message, MessageContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

public class TestMessage : IMessage
{
    public string Value { get; set; } = string.Empty;
}