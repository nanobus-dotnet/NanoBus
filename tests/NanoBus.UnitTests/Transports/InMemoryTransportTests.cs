namespace NanoBus.UnitTests.Transports;

[ExcludeFromCodeCoverage]
public class InMemoryTransportTests
{
    private readonly Mock<ILogger<InMemoryTransport>> _mockLogger = new();
    private readonly Mock<IMessageChannelRouter> _mockRouter = new();
    private readonly Mock<IDeadLetterQueue> _mockDlq = new();
    private readonly InMemoryTransport _transport;

    public InMemoryTransportTests()
    {
        _transport = new InMemoryTransport(_mockRouter.Object, _mockDlq.Object, new InMemoryTransportOptions(), _mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_NoHandlerRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var message = new TestMessage { Value = "send-async-throws" };
        await _transport.StartAsync();

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => _transport.SendAsync(message).AsTask());

        // Assert
        ex.Message.ShouldContain("No handlers registered for type");
    }

    [Fact]
    public async Task SendAsync_NoHandlerRegistered_ShouldLogError()
    {
        // Arrange
        var message = new TestMessage { Value = "send-async-logs" };
        await _transport.StartAsync();

        // Act
        await Should.ThrowAsync<InvalidOperationException>(() => _transport.SendAsync(message).AsTask());

        // Assert
        _mockLogger.VerifyMessageContains("No handlers registered for type", LogLevel.Error, Times.Once);
    }

    [Fact]
    public async Task PublishAsync_NoHandlerRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var message = new TestMessage { Value = "publish-async-throws" };
        await _transport.StartAsync();

        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => _transport.PublishAsync(message).AsTask());

        // Assert
        ex.Message.ShouldContain("No handlers registered for type");
    }

    [Fact]
    public async Task PublishAsync_NoHandlerRegistered_ShouldLogError()
    {
        // Arrange
        var message = new TestMessage { Value = "publish-async-logs" };
        var router = new MessageChannelRouter();
        var dlq = new InMemoryDeadLetterQueue();
        var transport = new InMemoryTransport(router, dlq, new InMemoryTransportOptions(), _mockLogger.Object);
        await transport.StartAsync();

        // Act
        await Should.ThrowAsync<InvalidOperationException>(() => transport.PublishAsync(message).AsTask());
        await Task.Delay(TimeSpan.FromSeconds(1)); // Allow message loop to run.

        // Assert
        _mockLogger.VerifyMessageContains("No handlers registered for type", LogLevel.Error, Times.Once);
    }


    [Fact]
    public async Task SendAsync_MoreThanOneHandlerRegistered_ShouldLogWarning()
    {
        // Arrange
        var handler1 = new TestMessageHandler();
        var handler2 = new TestMessageHandler();
        var router = new MessageChannelRouter();
        var dlq = new InMemoryDeadLetterQueue();
        var transport = new InMemoryTransport(router, dlq, new InMemoryTransportOptions(), _mockLogger.Object);
        transport.RegisterHandler(handler1);
        transport.RegisterHandler(handler2);
        await transport.StartAsync();

        // Act
        await transport.SendAsync(new TestMessage { Value = "test-send" });
        await Task.Delay(TimeSpan.FromSeconds(1)); // Allow message loop to run.

        // Assert
        _mockLogger.VerifyMessageContains("More than one handler for sent message type", LogLevel.Warning, Times.Once);
    }

    [Fact]
    public async Task DeliverMessagesAsync_OnNonCancellationException_ShouldLogAndThrowFatalError()
    {
        // Arrange
        _mockRouter
            .Setup(cm => cm.GetPendingMessageTypesAsync(CancellationToken.None))
            .Throws(new InvalidOperationException("Channel broken"));

        // Act
        await _transport.StartAsync();

        // Assert
        _mockLogger.VerifyMessageEquals("Fatal error in the message delivery process.", LogLevel.Error, Times.Once);
    }

    [Fact]
    public async Task ProcessDeadLetterQueueAsync_OnMultipleSendHandlers_ShouldLogWarning()
    {
        // Arrange
        var message = new TestMessage();
        var manager = new MessageChannelRouter();
        var dlq = new InMemoryDeadLetterQueue();

        await dlq.EnqueueAsync(
            message,
            message.GetType().FullName!,
            MessageMethod.Send,
            new MessageContext(null!, "msg-1234", "correlation-22325", []),
            DateTime.UtcNow - TimeSpan.FromSeconds(30),
            1,
            new Exception("failed"),
            CancellationToken.None,
            CancellationToken.None
            );

        var transport = new InMemoryTransport(manager, dlq, new InMemoryTransportOptions(), _mockLogger.Object);
        var handler1 = new AlwaysFailsHandler();
        var handler2 = new AlwaysFailsHandler();
        transport.RegisterHandler(handler1);
        transport.RegisterHandler(handler2);
        await transport.StartAsync(CancellationToken.None);

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await transport.ProcessDeadLetterQueueAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(1)); // Allow message loop to run.

        // Assert
        _mockLogger.VerifyMessageContains("More than one handler for sent message type", LogLevel.Warning, Times.AtLeastOnce);
    }

    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null!)]
    [Theory]
    public async Task ReprocessDeadLetterMessageAsync_InvalidUniqueId_ShouldThrowException(string uniqueId)
    {
        // Arrange
        // Act
        var ex = await Should.ThrowAsync<ArgumentException>(() => _transport.ReprocessDeadLetterMessageAsync(uniqueId, CancellationToken.None).AsTask());

        // Assert
        ex.Message.ShouldBe("Unique Id must be valid.");
    }

    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null!)]
    [Theory]
    public async Task ReprocessDeadLetterMessageAsync_InvalidUniqueId_ShouldLogException(string uniqueId)
    {
        // Arrange
        // Act
        await Should.ThrowAsync<ArgumentException>(() => _transport.ReprocessDeadLetterMessageAsync(uniqueId, CancellationToken.None).AsTask());

        // Assert
        _mockLogger.VerifyMessageEquals("Unique Id must be valid.", LogLevel.Error, Times.Once);
    }


    [Fact]
    public async Task ReprocessDeadLetterMessageAsync_MissingUniqueId_ShouldThrowException()
    {
        // Arrange
        // Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => _transport.ReprocessDeadLetterMessageAsync("id-1234", CancellationToken.None).AsTask());

        // Assert
        ex.Message.ShouldContain("Message not found for Unique Id:");
    }

    [Fact]
    public async Task ReprocessDeadLetterMessageAsync_MissingUniqueId_ShouldLogException()
    {
        // Arrange
        // Act
        await Should.ThrowAsync<InvalidOperationException>(() => _transport.ReprocessDeadLetterMessageAsync("id-1234", CancellationToken.None).AsTask());

        // Assert
        _mockLogger.VerifyMessageContains("Message not found for Unique Id:", LogLevel.Error, Times.Once);
    }

    [Fact]
    public async Task ProcessDeadLetterQueueAsync_WhenTokenCancelledBetweenIterations_ExitsNormally()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var router = new MessageChannelRouter();
        var mockDLQ = new Mock<IDeadLetterQueue>();
        mockDLQ
            .Setup(x => x.GetMessagesForRetryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var transport = new InMemoryTransport(router, mockDLQ.Object);

        // Act
        var task = transport.ProcessDeadLetterQueueAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(1));
        cts.Cancel();
        await task;

        // Assert
        mockDLQ.Verify(x => x.GetMessagesForRetryAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

}
