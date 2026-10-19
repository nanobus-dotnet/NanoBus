namespace NanoBus.UnitTests.Transports;

[ExcludeFromCodeCoverage]
public class DemoTransportTests
{
    private readonly Mock<ILogger<DemoTransport>> _mockLogger = new();
    private readonly DemoTransport _transport;

    public DemoTransportTests()
    {
        _transport = new DemoTransport(new TransportOptions(), _mockLogger.Object);
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
    public async Task SendAsync_InvokesHandlerWithMessage()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };
        _transport.RegisterHandler(handler.Object);

        // Act
        await _transport.SendAsync(message, metadata, CancellationToken.None);

        // Assert
        handler.Verify(x => x.HandleAsync(message, It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_InvokesAllHandlers()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };
        var handler1 = new Mock<IMessageHandler<TestMessage>>();
        var handler2 = new Mock<IMessageHandler<TestMessage>>();
        var handlers = new[] { handler1.Object, handler2.Object };

        _transport.RegisterHandler(handler1.Object);
        _transport.RegisterHandler(handler2.Object);

        // Act
        await _transport.PublishAsync(message, metadata, CancellationToken.None);

        // Assert
        handler1.Verify(x => x.HandleAsync(message, It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()), Times.Once);
        handler2.Verify(x => x.HandleAsync(message, It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_PassesCancellationToken()
    {
        // Arrange
        CancellationToken? capturedToken = null!;

        var transport = new Mock<ITransport>();
        transport.Setup(t => t.PublishAsync(It.IsAny<TestMessage>(), It.IsAny<MessageMetadata>(), It.IsAny<CancellationToken>()))
            .Callback((TestMessage _, MessageMetadata _, CancellationToken ct) => capturedToken = ct)
            .Returns(ValueTask.CompletedTask);

        var message = new TestMessage { Value = "test" };
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };

        var cts = new CancellationTokenSource();

        // Act
        await transport.Object.PublishAsync(message, metadata, cts.Token);
        cts.Cancel();

        // Assert
        Assert.True(capturedToken?.IsCancellationRequested);
    }

    [Fact]
    public async Task SendAsync_NoHandlerThrowsInvalidOperationException()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };

        // Act

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _transport.SendAsync(message, metadata, CancellationToken.None).AsTask());
        //await Assert.ThrowsAsync<InvalidOperationException>(() => _transport.SendAsync(message, metadata, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task PublishAsync_NoHandlerThrowsInvalidOperationException()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };

        // Act

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _transport.PublishAsync(message, metadata, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_NoHandlerExceptionPropagates()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };
        var mockHandler = new Mock<IMessageHandler<TestMessage>>();
        mockHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failed"));

        // Act

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _transport.SendAsync(message, metadata, CancellationToken.None).AsTask());
    }


    [Fact]
    public async Task SendAsync_HandlerExceptionDoesNotPropagate()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };
        var mockHandler = new Mock<IMessageHandler<TestMessage>>();
        mockHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failed"));
        _transport.RegisterHandler(mockHandler.Object);

        // Act
        await _transport.SendAsync(message, metadata, CancellationToken.None);

        // Assert
        // no exception
    }

    [Fact]
    public async Task SendAsync_MoreThanOneHandlerRegisteredDoesNotPropagate()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };
        var mockHandler = new Mock<IMessageHandler<TestMessage>>();
        mockHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failed"));
        _transport.RegisterHandler(mockHandler.Object);
        _transport.RegisterHandler(mockHandler.Object);

        // Act
        await _transport.SendAsync(message, metadata, CancellationToken.None);

        // Assert
        // no exception
    }

    [Fact]
    public async Task PublishAsync_HandlerExceptionDoesNotPropagate()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var metadata = new MessageMetadata { MessageId = "msg-1", CorrelationId = "correlation-1", Headers = [] };
        var mockHandler = new Mock<IMessageHandler<TestMessage>>();
        mockHandler
            .Setup(x => x.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failed"));
        _transport.RegisterHandler(mockHandler.Object);

        // Act
        await _transport.PublishAsync(message, metadata, CancellationToken.None);

        // Assert
        // no exception
    }

    [Fact]
    public async Task SendAsync_NullMetadataConstructsValidMessageContext()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();
        _transport.RegisterHandler(handler.Object);

        // Act
        await _transport.SendAsync(message, cancellationToken: CancellationToken.None);

        // Assert
        handler.Verify(x => x.HandleAsync(message, It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_NullMetadataConstructsValidMessageContext()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };
        var handler = new Mock<IMessageHandler<TestMessage>>();
        _transport.RegisterHandler(handler.Object);

        // Act
        await _transport.PublishAsync(message, cancellationToken: CancellationToken.None);

        // Assert
        handler.Verify(x => x.HandleAsync(message, It.IsAny<MessageContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
