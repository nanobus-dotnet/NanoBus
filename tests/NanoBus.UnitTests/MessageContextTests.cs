using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using NanoBus.Abstractions;

namespace NanoBus.UnitTests;

[ExcludeFromCodeCoverage]
public class MessageContextTests
{
    [Fact]
    public async Task SendAsync_InvokesTransportSendAsync()
    {
        // Arrange
        var transport = new Mock<ITransport>();
        var context = new MessageContext(transport.Object, "msg-123", "corr-123", []);
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
        var context = new MessageContext(transport.Object, "msg-123", "corr-123", []);
        var testMessage = new TestMessage { Value = "test123" };
        var metatdata = new MessageMetadata();

        // Act
        await context.PublishAsync(testMessage, metatdata, CancellationToken.None);

        // Assert
        transport.Verify(x => x.PublishAsync(testMessage, metatdata, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void NullCtorValues_CreatesDefaultOptions()
    {
        // Arrange
        var transport = new Mock<ITransport>();
        var context = new MessageContext(transport.Object, null, null, null);

        // Act

        // Assert
        using (new AssertionScope())
        {
            context.MessageId.Should().NotBe(Guid.Empty.ToString());
            context.CorrelationId.Should().NotBe(Guid.Empty.ToString());
            context.Headers.Should().NotBeNull();
        }
    }
}
