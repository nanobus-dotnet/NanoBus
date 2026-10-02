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
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "corr-1", Headers = [] };

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
        await _bus.StartAsync();
        await _bus.StopAsync(CancellationToken.None);

        // Assert
        _transport.Verify(x => x.StopAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task StopAsync_AlreadyStartedDoesNotInvokeTransportStopAsync()
    {
        // Arrange

        // Act
        await _bus.StartAsync();
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
}
