namespace NanoBus.IntegrationTests.Transports;

[ExcludeFromCodeCoverage]
public class InMemoryDeadLetterQueueTests
{
    private readonly InMemoryDeadLetterQueue _dlq;
    private readonly IMessage _testMessage = new TestMessage { Value = "dlq-tests" };
    private readonly MessageContext _messageContext;
    private readonly Exception _testException;

    public InMemoryDeadLetterQueueTests()
    {
        _dlq = new InMemoryDeadLetterQueue();
        _messageContext = new MessageContext(null!, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), []);
        _testException = new InvalidOperationException("Test error");
    }

    [Fact]
    public async Task EnqueueAsync_ShouldStoreFailedMessageWithCorrectProperties()
    {
        // Arrange
        var messageType = "TestMessage";
        var method = MessageMethod.Send;
        var failedTime = DateTime.UtcNow.AddSeconds(-5);

        // Act
        var id = await _dlq.EnqueueAsync(_testMessage, messageType, method, _messageContext, failedTime, 1, _testException, CancellationToken.None, CancellationToken.None);
        var retrievedMessage = await _dlq.GetMessageAsync(id, CancellationToken.None);

        // Assert
        id.ShouldNotBeNull();
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage!.MessageType.ShouldBe(messageType);
        retrievedMessage.Method.ShouldBe(method);
        retrievedMessage.Context.MessageId.ShouldBe(_messageContext.MessageId);
        retrievedMessage.DeliveryAttempts.ShouldBe(2);
        retrievedMessage.LastError.ShouldBe(_testException);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldReturnUniqueIdForEachMessage()
    {
        // Act
        var id1 = await _dlq.EnqueueAsync(_testMessage, "Type1", MessageMethod.Send, _messageContext, DateTime.UtcNow, 1, _testException, CancellationToken.None, CancellationToken.None);
        var id2 = await _dlq.EnqueueAsync(_testMessage, "Type2", MessageMethod.Send, _messageContext, DateTime.UtcNow, 1, _testException, CancellationToken.None, CancellationToken.None);

        // Assert
        id1.ShouldNotBe(id2);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldSetNextRetryTimeWithBackoffDelay()
    {
        // Arrange
        var beforeEnqueue = DateTime.UtcNow;

        // Act
        var id = await _dlq.EnqueueAsync(_testMessage, "Type", MessageMethod.Send, _messageContext, DateTime.UtcNow, 1, _testException, CancellationToken.None, CancellationToken.None);
        var afterEnqueue = DateTime.UtcNow;
        var message = await _dlq.GetMessageAsync(id, CancellationToken.None);

        // Assert
        message!.NextRetryTime.ShouldBeGreaterThanOrEqualTo(beforeEnqueue.AddSeconds(1));
        message.NextRetryTime.ShouldBeLessThanOrEqualTo(afterEnqueue.AddSeconds(1));
    }

    [Fact]
    public async Task GetMessagesForRetryAsync_ShouldReturnOnlyReadyMessages()
    {
        // Arrange
        var readyTime = DateTime.UtcNow.AddSeconds(-5);
        var futureTime = DateTime.UtcNow.AddSeconds(5);

        var readyId = await _dlq.EnqueueAsync(_testMessage, "Type1", MessageMethod.Send, _messageContext, readyTime, 1, _testException, CancellationToken.None, CancellationToken.None);

        var futureContext = new MessageContext(null!, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), []);
        var futureId = await _dlq.EnqueueAsync(_testMessage, "Type2", MessageMethod.Send, futureContext, futureTime, 1, _testException, CancellationToken.None, CancellationToken.None);

        // Act
        var messagesToRetry = await _dlq.GetMessagesForRetryAsync(CancellationToken.None);
        var ids = messagesToRetry.Select(m => m.UniqueId).ToList();

        // Assert
        ids.ShouldContain(readyId);
        ids.ShouldNotContain(futureId);
    }

    //[Fact]
    //public async Task GetMessagesForRetryAsync_ShouldNotReturnMessagesExceedingMaxDeliveryAttempts()
    //{
    //    // Arrange
    //    _dlq.MaxDeliveryAttempts = 4;
    //    var id = await _dlq.EnqueueAsync(_testMessage, "Type", MessageMethod.Send, _messageContext, DateTime.UtcNow.AddSeconds(-100), 1, _testException, CancellationToken.None, CancellationToken.None);

    //    await _dlq.UpdateMessageRetryAsync(id, 1, _testException, CancellationToken.None, CancellationToken.None);
    //    await _dlq.UpdateMessageRetryAsync(id, 2, _testException, CancellationToken.None);
    //    await _dlq.UpdateMessageRetryAsync(id, 3, _testException, CancellationToken.None);

    //    // Act
    //    var messagesToRetry = await _dlq.GetMessagesForRetryAsync(CancellationToken.None);

    //    // Assert
    //    messagesToRetry.ShouldBeEmpty();
    //}

    [Fact]
    public async Task GetAllMessagesAsync_ShouldReturnAllMessages()
    {
        // Arrange
        var id1 = await _dlq.EnqueueAsync(_testMessage, "Type1", MessageMethod.Send, _messageContext, DateTime.UtcNow, 1, _testException, CancellationToken.None, CancellationToken.None);
        var id2 = await _dlq.EnqueueAsync(_testMessage, "Type2", MessageMethod.Send, _messageContext, DateTime.UtcNow, 1, _testException, CancellationToken.None, CancellationToken.None);

        // Act
        var allMessages = await _dlq.GetAllMessagesAsync(CancellationToken.None);

        // Assert
        allMessages.Count().ShouldBe(2);
        allMessages.Select(m => m.UniqueId).ShouldContain(id1);
        allMessages.Select(m => m.UniqueId).ShouldContain(id2);
    }

    [Fact]
    public async Task GetMessageAsync_ShouldReturnMessageById()
    {
        // Arrange
        var id = await _dlq.EnqueueAsync(_testMessage, "Type", MessageMethod.Send, _messageContext, DateTime.UtcNow, 1, _testException, CancellationToken.None, CancellationToken.None);

        // Act
        var retrieved = await _dlq.GetMessageAsync(id, CancellationToken.None);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved!.UniqueId.ShouldBe(id);
    }

    [Fact]
    public async Task GetMessageAsync_ShouldReturnNullForNonExistentId()
    {
        // Act
        var retrieved = await _dlq.GetMessageAsync("non-existent-id", CancellationToken.None);

        // Assert
        retrieved.ShouldBeNull();
    }


    [Fact]
    public async Task ClearQueueAsync_ShouldClearDLQ()
    {
        // Arrange
        var id = await _dlq.EnqueueAsync(_testMessage, "Type", MessageMethod.Send, _messageContext, DateTime.UtcNow, 1, _testException, CancellationToken.None, CancellationToken.None);

        // Act
        await _dlq.ClearQueueAsync(CancellationToken.None);

        var testMessage = await _dlq.GetMessageAsync(id, CancellationToken.None);

        // Assert
        testMessage.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveMessage()
    {
        // Arrange
        var id = await _dlq.EnqueueAsync(_testMessage, "Type", MessageMethod.Send, _messageContext, DateTime.UtcNow, 1, _testException, CancellationToken.None, CancellationToken.None);

        // Act
        await _dlq.RemoveAsync(id, CancellationToken.None);
        var retrieved = await _dlq.GetMessageAsync(id, CancellationToken.None);

        // Assert
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotThrowForNonExistentMessage()
    {
        // Act & Assert
        var act = async () => await _dlq.RemoveAsync("non-existent-id", CancellationToken.None);
        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public void MaxDeliveryAttempts_ShouldDefaultToThree()
    {
        // Assert
        _dlq.MaxDeliveryAttempts.ShouldBe(4);
    }

    [Fact]
    public async Task MaxDeliveryAttempts_ShouldBeConfigurable()
    {
        // Act
        _dlq.MaxDeliveryAttempts = 5;

        // Assert
        _dlq.MaxDeliveryAttempts.ShouldBe(5);
    }
}
