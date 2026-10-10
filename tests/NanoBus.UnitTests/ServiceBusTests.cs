namespace NanoBus.UnitTests;

[ExcludeFromCodeCoverage]
public class ServiceBusTests
{
    private readonly Mock<ILogger<ServiceBus>> _mockLogger = new();
    private readonly Mock<ITransport> _transport = new();

    private readonly ServiceBus _bus;

    public ServiceBusTests()
    {
        _bus = new(_transport.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_InvokesTransportSendAsync()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };

        // Act
        await _bus.SendAsync(message, metadata, CancellationToken.None);

        // Assert
        _transport.Verify(x => x.SendAsync(message, metadata, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_InvokesTransportSendAsync()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };

        // Act
        await _bus.PublishAsync(message, metadata, CancellationToken.None);

        // Assert
        _transport.Verify(x => x.PublishAsync(message, metadata, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task StartAsync_InvokesTransportStartAsync()
    {
        // Arrange

        // Act
        await _bus.StartAsync(CancellationToken.None);

        // Assert
        _transport.Verify(x => x.StartAsync(CancellationToken.None), Times.Once);
    }


    [Fact]
    public async Task ReprocessDeadLetterQueueAsync_InvokesTransport()
    {
        await _bus.ReprocessDeadLetterQueueAsync(CancellationToken.None);

        _transport.Verify(x => x.ReprocessDeadLetterQueueAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetDeadLetterMessagesAsync_InvokesTransport()
    {
        await _bus.GetDeadLetterMessagesAsync(CancellationToken.None);

        _transport.Verify(x => x.GetDeadLetterMessagesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ReprocessDeadLetterMessageAsync_InvokesTransport()
    {
        await _bus.ReprocessDeadLetterMessageAsync("unique-id", CancellationToken.None);

        _transport.Verify(x => x.ReprocessDeadLetterMessageAsync(It.IsAny<string>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetFailedDeadLetterMessagesAsync_InvokesTransport()
    {
        await _bus.GetFailedDeadLetterMessagesAsync(CancellationToken.None);

        _transport.Verify(x => x.GetFailedDeadLetterMessagesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ClearDeadLetterQueueAsync_InvokesTransport()
    {
        await _bus.ClearDeadLetterQueueAsync(CancellationToken.None);

        _transport.Verify(x => x.ClearDeadLetterQueueAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendAsync_NullMetaDataDoesNotThrowException()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();

        // Act
        await _bus.SendAsync(message, null, CancellationToken.None);

        // Assert
        _transport.Verify(x => x.SendAsync(message, It.IsAny<MessageMetadata>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_NullMetaDataDoesNotThrowException()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();

        // Act
        await _bus.PublishAsync(message, null, CancellationToken.None);

        // Assert
        _transport.Verify(x => x.PublishAsync(message, It.IsAny<MessageMetadata>(), CancellationToken.None), Times.Once);
    }


    [Fact]
    public async Task StartAsync_AlreadyStartedDoesNotInvokeTransportStartAsyncTwice()
    {
        // Arrange

        // Act
        await _bus.StartAsync(CancellationToken.None);
        await _bus.StartAsync(CancellationToken.None);

        // Assert
        _transport.Verify(x => x.StartAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task StopAsync_AlreadyStoppedDoesNotInvokeTransportStopAsync()
    {
        // Arrange

        // Act
        await _bus.StopAsync(CancellationToken.None);
        await _bus.StopAsync(CancellationToken.None);

        // Assert
        _transport.Verify(x => x.StopAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task StopAsync_InvokesTransportStopAsync()
    {
        // Arrange

        // Act
        await _bus.StartAsync(CancellationToken.None);
        await _bus.StopAsync(CancellationToken.None);

        // Assert
        _transport.Verify(x => x.StopAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task StopAsync_AlreadyStartedDoesNotInvokeTransportStopAsync()
    {
        // Arrange

        // Act
        await _bus.StartAsync(CancellationToken.None);
        await _bus.StopAsync(CancellationToken.None);

        // Assert
        _transport.Verify(x => x.StopAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Ctor_NullLoggerIsValid()
    {
        // Arrange
        var bus = new ServiceBus(_transport.Object, null);

        // Act

        // Assert
        Assert.NotNull(bus);
    }

    [Fact]
    public void Extension_AddServiceBusDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<ITransport>().Object);

        // Act
        services.AddServiceBus();
        var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IServiceBus>();

        // Assert
        bus.ShouldNotBeNull();
    }

    [Fact]
    public void Extension_AddServiceBus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<ITransport>().Object);

        // Act
        services.AddServiceBus((sp, t) =>
        {
            using var scope = sp.CreateScope();
            t.RegisterHandler(new TestMessageHandler());
        });

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IServiceBus>();

        // Assert
        bus.ShouldNotBeNull();
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
