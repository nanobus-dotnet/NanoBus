namespace NanoBus.IntegrationTests.Transports;

[ExcludeFromCodeCoverage]
public class DemoTransportTests
{
    private readonly DemoTransport _transport;

    public DemoTransportTests()
    {
        _transport = new DemoTransport(new TransportOptions(), new NullLogger<DemoTransport>());
    }

    [Fact]
    public void TransportCtor_NullOptionsAndLoggerAreValid()
    {
        // Arrange
        // Act
        var transport = new DemoTransport();

        // Assert
        Assert.NotNull(transport);
    }


    [Fact]
    public async Task PublishAsync_NullMetadataConstructsValidMessageContext()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new TestMessageHandler();
        _transport.RegisterHandler(handler);

        // Act
        await _transport.PublishAsync(message, cancellationToken: CancellationToken.None);

        // Assert
    }


    [Fact]
    public async Task StartAsync()
    {
        // Arrange

        // Act
        await _transport.StartAsync(CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task StopAsync()
    {
        // Arrange

        // Act
        await _transport.StopAsync(CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task GetDlqMessagesAsync()
    {
        // Arrange

        // Act
        await _transport.GetDeadLetterMessagesAsync(CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task GetFailedDlqMessagesAsync()
    {
        // Arrange

        // Act
        await _transport.GetFailedDeadLetterMessagesAsync(CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task ReprocessDlqAsync()
    {
        // Arrange

        // Act
        await _transport.ReprocessDeadLetterQueueAsync(CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task ReprocessDlqItemAsync()
    {
        // Arrange

        // Act
        await _transport.ReprocessDeadLetterMessageAsync(string.Empty, CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task RetryFailedDlqMessagesAsync()
    {
        // Arrange

        // Act
        await _transport.ProcessDeadLetterQueueAsync(CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task ClearDeadLetterQueueAsync()
    {
        // Arrange

        // Act
        await _transport.ClearDeadLetterQueueAsync(CancellationToken.None);

        // Assert
    }

    [Fact]
    public void Extension_AddDemoServiceBusDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDemoServiceBus();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IServiceBus>();

        // Assert
        bus.ShouldNotBeNull();
    }

    [Fact]
    public void Extension_AddDemoServiceBus()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDemoServiceBus(options => options.MaxDeliveryAttempts = 5, (sp, t) =>
        {
            using var scope = sp.CreateScope();
            t.RegisterHandler(new TestMessageHandler());
        });

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IServiceBus>();

        // Assert
        bus.ShouldNotBeNull();
    }
}
