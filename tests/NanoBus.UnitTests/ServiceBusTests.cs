using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moq;
using NanoBus.Abstractions;
using NanoBus.Core;

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
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "corr-1", Headers = [] };

        // Act
        await _bus.SendAsync(message, metadata, TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.SendAsync(message, metadata, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_InvokesTransportSendAsync()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "corr-1", Headers = [] };

        // Act
        await _bus.PublishAsync(message, metadata, TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.PublishAsync(message, metadata, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task StartAsync_InvokesTransportStartAsync()
    {
        // Arrange

        // Act
        await _bus.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.StartAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendAsync_NullMetaDataDoesNotThrowException()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();

        // Act
        await _bus.SendAsync(message, null, TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.SendAsync(message, It.IsAny<MessageMetadata>(), TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_NullMetaDataDoesNotThrowException()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();

        // Act
        await _bus.PublishAsync(message, null, TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.PublishAsync(message, It.IsAny<MessageMetadata>(), TestContext.Current.CancellationToken), Times.Once);
    }


    [Fact]
    public async Task StartAsync_AlreadyStartedDoesNotInvokeTransportStartAsyncTwice()
    {
        // Arrange

        // Act
        await _bus.StartAsync(TestContext.Current.CancellationToken);
        await _bus.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.StartAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task StopAsync_AlreadyStoppedDoesNotInvokeTransportStopAsync()
    {
        // Arrange

        // Act
        await _bus.StopAsync(TestContext.Current.CancellationToken);
        await _bus.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.StopAsync(TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task StopAsync_InvokesTransportStopAsync()
    {
        // Arrange

        // Act
        await _bus.StartAsync(TestContext.Current.CancellationToken);
        await _bus.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.StopAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task StopAsync_AlreadyStartedDoesNotInvokeTransportStopAsync()
    {
        // Arrange

        // Act
        await _bus.StartAsync(TestContext.Current.CancellationToken);
        await _bus.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        _transport.Verify(x => x.StopAsync(TestContext.Current.CancellationToken), Times.Once);
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
}
