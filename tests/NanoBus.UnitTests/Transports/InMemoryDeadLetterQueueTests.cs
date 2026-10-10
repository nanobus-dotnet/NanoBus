namespace NanoBus.UnitTests.Transports;

[ExcludeFromCodeCoverage]
public class InMemoryDeadLetterQueueTests
{
    private readonly Mock<ILogger<InMemoryDeadLetterQueue>> _mockLogger = new();
    private readonly InMemoryDeadLetterQueue _dlq;

    public InMemoryDeadLetterQueueTests()
    {
        _dlq = new InMemoryDeadLetterQueue(_mockLogger.Object);
    }

    [InlineData(-1)]
    [InlineData(0)]
    [Theory]
    public async Task EnqueueAsync_DeliveryAttemptLessThanOrEqualZero_ShouldLogWarning(int deliveryAttempt)
    {
        // Arrange & Act
        await _dlq.EnqueueAsync(
            new TestMessage { Value = "test-enqueue" }, 
            string.Empty, 
            MessageMethod.Send, 
            new MessageContext(null!), 
            DateTime.UtcNow, 
            deliveryAttempt, 
            new Exception(), 
            CancellationToken.None,
            CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Equals("Delivery attempt must be at least 1. Setting to 1.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    [InlineData(1)]
    [InlineData(2)]
    [Theory]
    public async Task EnqueueAsync_DeliveryAttemptGreaterThanOrEqualOne_ShouldNotLogWarning(int deliveryAttempt)
    {
        // Arrange & Act
        await _dlq.EnqueueAsync(
            new TestMessage { Value = "test-enqueue" },
            string.Empty,
            MessageMethod.Send,
            new MessageContext(null!),
            DateTime.UtcNow,
            deliveryAttempt,
            new Exception(),
            CancellationToken.None,
            CancellationToken.None);


        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Equals("Delivery attempt must be at least 1. Setting to 1.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}

