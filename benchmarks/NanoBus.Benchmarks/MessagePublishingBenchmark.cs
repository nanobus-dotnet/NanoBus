using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NanoBus.Abstractions;
using NanoBus.Core;
using NanoBus.Transports.InMemory;

namespace NanoBus.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class MessagePublishingBenchmark
{
    private IServiceBus _nanoBus = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        var transport = new InMemoryTransport(new MessageChannelRouter(), new InMemoryDeadLetterQueue());
        transport.RegisterHandler(new NanoBusTestMessageHandler());
        _nanoBus = new ServiceBus(transport);
        await _nanoBus.StartAsync();

    }

    [Benchmark]
    public async Task NanoBus_PublishMessage()
    {
        await _nanoBus.PublishAsync(new TestMessage { Value = "test" });

    }

    [Benchmark]
    public async Task NanoBus_SendMessage()
    {
        await _nanoBus.SendAsync(new TestMessage { Value = "test" });
    }


}