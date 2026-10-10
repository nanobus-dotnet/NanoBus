namespace NanoBus.UnitTests;

[ExcludeFromCodeCoverage]
public class MessageContextTests
{
    [Fact]
    public async Task SendAsync_InvokesTransportSendAsync()
    {
        // Arrange
        var transport = new Mock<ITransport>();
        var context = new MessageContext(transport.Object, "msg-123", "correlation-123", []);
        var testMessage = new TestMessage { Value = "test123" };
        var metatdata = new MessageMetadata();

        // Act
        await context.SendAsync(testMessage, metatdata, CancellationToken.None);

        // Assert
        transport.Verify(x => x.SendAsync(testMessage, metatdata, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_InvokesTransportPublishAsync()
    {
        // Arrange
        var transport = new Mock<ITransport>();
        var context = new MessageContext(transport.Object, "msg-123", "correlation-123", []);
        var testMessage = new TestMessage { Value = "test123" };
        var metatdata = new MessageMetadata();

        // Act
        await context.PublishAsync(testMessage, metatdata, CancellationToken.None);

        // Assert
        transport.Verify(x => x.PublishAsync(testMessage, metatdata, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void NullCtorMessageId_CreatesDefaultMessageId()
    {
        // Arrange
        var transport = new Mock<ITransport>();
        var context = new MessageContext(transport.Object, null);

        // Act

        // Assert
        context.MessageId.ShouldNotBe(Guid.Empty.ToString());
    }

    [Fact]
    public void NullCtorCorrelationId_CreatesDefaultCorrelationId()
    {
        // Arrange
        var transport = new Mock<ITransport>();
        var context = new MessageContext(transport.Object, null, null);

        // Act

        // Assert
        context.CorrelationId.ShouldNotBe(Guid.Empty.ToString());
    }

    [Fact]
    public void NullCtorHeaders_CreatesDefaultHeaders()
    {
        // Arrange
        var transport = new Mock<ITransport>();
        var context = new MessageContext(transport.Object, null, null, null);

        // Act

        // Assert
        context.Headers.ShouldNotBeNull();
    }
}
