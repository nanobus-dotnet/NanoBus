namespace NanoBus.IntegrationTests.Transports;

/// <summary>
/// Unit tests for InMemoryTransport.
/// </summary>
[ExcludeFromCodeCoverage]
public class InMemoryTransportTests : IAsyncLifetime
{
    private InMemoryTransport _transport = null!;
    private InMemoryDeadLetterQueue _dlq = null!;
    private MessageChannelRouter _messageChannelRouter = null!;

    private readonly ILogger<InMemoryTransport> _transportLogger = NullLogger<InMemoryTransport>.Instance;

    public async Task InitializeAsync()
    {
        _messageChannelRouter = new MessageChannelRouter();
        _dlq = new InMemoryDeadLetterQueue(new NullLogger<InMemoryDeadLetterQueue>()) { MaxDeliveryAttempts = 3 };
        _transport = new InMemoryTransport(_messageChannelRouter, _dlq, new InMemoryTransportOptions 
        { 
            MaxDeliveryAttempts = 3, 
            MaxChannelCapacity = 100, 
            DeadLetterPollingDelay = TimeSpan.FromMilliseconds(100)
        }, _transportLogger);

    }

    public async Task DisposeAsync()
    {
        await _transport.StopAsync();
    }


    [Fact]
    public void Constructor_SetsDefaultOptions_WhenNotProvided()
    {
        // Arrange & Act
        var transport = new InMemoryTransport(_messageChannelRouter, _dlq);

        // Assert
        transport.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_UsesProvidedOptions()
    {
        // Arrange
        var options = new InMemoryTransportOptions 
        { 
            MaxChannelCapacity = 500, 
            MaxDeliveryAttempts = 3, 
            DeadLetterPollingDelay = TimeSpan.FromSeconds(1) 
        };

        // Act
        var transport = new InMemoryTransport(_messageChannelRouter ,_dlq, options, _transportLogger);

        // Assert
        transport.ShouldNotBeNull();
    }


    [Fact]
    public void RegisterHandler_RegistersHandlerSuccessfully()
    {
        // Arrange
        var handler = new TestMessageHandler();

        // Act
        _transport.RegisterHandler(handler);

        // Assert - no exception
        true.ShouldBeTrue();
    }

    [Fact]
    public void RegisterHandler_AllowsMultipleHandlersForPublish()
    {
        // Arrange
        var handler1 = new TestMessageHandler();
        var handler2 = new TestMessageHandler2();

        // Act
        _transport.RegisterHandler(handler1);
        _transport.RegisterHandler(handler2);

        // Assert - no exception
        true.ShouldBeTrue();
    }


    [Fact]
    public async Task PublishAsync_ThrowsInvalidOperationException_WhenNoHandlersRegistered()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _transport.PublishAsync(message).AsTask());
    }

    [Fact]
    public async Task PublishAsync_Delivers_ToAllHandlers()
    {
        // Arrange
        var handler1 = new MarkDeliveredHandler();
        var handler2 = new MarkDeliveredHandler();
        _transport.RegisterHandler(handler1);
        _transport.RegisterHandler(handler2);
        await _transport.StartAsync();

        var testMessage = new TestMessage { Value = "delivery-test" };

        // Act
        await _transport.PublishAsync(testMessage);
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert
        handler1.Delivered.ShouldBeTrue();
        handler2.Delivered.ShouldBeTrue();
    }


    [Fact]
    public async Task PublishAsync_WithMetadata_PassesMetadataToHandler()
    {
        // Arrange
        var metadata = new MessageMetadata 
        { 
            MessageId = "msg-123", 
            CorrelationId = "correlation-456", 
            Headers = new Dictionary<string, string>
            {
                { "test-header", "header-value" }
            }
        };

        var message = new TestMessage { Value = "metadata-test" };
        var handler = new SupplyContextHandler();
        await _transport.StartAsync();
        _transport.RegisterHandler(handler);

        // Act
        await _transport.SendAsync(message, metadata);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.

        // Assert -- Message metadata got passed to the handler.
        handler.MessageContext!.MessageId.ShouldBe(metadata.MessageId);
        handler.MessageContext!.CorrelationId.ShouldBe(metadata.CorrelationId);
        handler.MessageContext!.Headers.ShouldBeEquivalentTo(metadata.Headers);
    }


    [Fact]
    public async Task SendAsync_ThrowsInvalidOperationException_WhenNoHandlersRegistered()
    {
        // Arrange
        var message = new TestMessage { Value = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _transport.SendAsync(message).AsTask());
    }

    [Fact]
    public async Task StartAsync_StartsBackgroundTasks()
    {
        // Arrange
        var handler = new FailsOnceHandler();
        _transport.RegisterHandler(handler);

        // Act
        await _transport.StartAsync();

        // Assert - register a handler and send a message
        var message = new TestMessage { Value = "startup-test" };

        await _transport.PublishAsync(message);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(2)); // Wait for redelivery delay attempts.

        // Assert
        handler.Attempts.ShouldBeGreaterThan(1);
        handler.FailedAttempts.ShouldBe(1);

        // Cleanup
        await _transport.StopAsync();
    }

    [Fact]
    public async Task SendAsync_MoreThanOneHandlerSucceeds()
    {
        // Arrange
        var handler1 = new TestMessageHandler();
        var handler2 = new TestMessageHandler();
        _transport.RegisterHandler(handler1);
        _transport.RegisterHandler(handler2);
        await _transport.StartAsync();

        // Act
        await _transport.SendAsync(new TestMessage { Value = "two-receivers" });

        // Assert -- Handlers didn't fail.
        var dlqMessages = await _transport.GetDeadLetterMessagesAsync();
        dlqMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task NullDeadLetterQueue_ShouldNotThrowException()
    {
        // Arrange
        var transport = new InMemoryTransport(_messageChannelRouter, null!);

        // Act
        await transport.StartAsync();

        // Assert
        true.ShouldBeTrue();
    }

    [InlineData(0)]
    [InlineData(1)]
    [Theory]
    public async Task ClearDeadLetterQueueAsync_ShouldClearFailures(int maxRetries)
    {
        // Arrange
        var handler = new AlwaysFailsHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();

        await _transport.SendAsync(new TestMessage { Value = "dlq-clear" });
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(maxRetries)); // Wait for redelivery delay attempts.

        // Act
        await _transport.ClearDeadLetterQueueAsync();

        // Assert
        var failedMessages = await _transport.GetDeadLetterMessagesAsync();
        failedMessages.ShouldBeEmpty();
    }

    [InlineData(1)]
    [InlineData(2)]
    [Theory]
    public async Task ReprocessDeadLetterMessageAsync_Send_ShouldSucceed(int maxDeliveryAttempts)
    {
        // Arrange
        var transport = new InMemoryTransport(_messageChannelRouter, new InMemoryDeadLetterQueue() { MaxDeliveryAttempts = maxDeliveryAttempts });
        var handler = new AlwaysFailsHandler();
        transport.RegisterHandler(handler);
        await transport.StartAsync();

        // Act
        await transport.SendAsync(new TestMessage { Value = "dlq-clear" });
        await Task.Delay(TimeSpan.FromSeconds(1)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts)); // Wait for redelivery delay attempts.
        var failedMessage = (await transport.GetFailedDeadLetterMessagesAsync()).First();
        await transport.ReprocessDeadLetterMessageAsync(failedMessage.UniqueId);

        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts)); // Wait for redelivery delay attempts.

        // Assert -- DeliveryAttempt count should get set back to maxDeliveryAttempts + 1.
        failedMessage.DeliveryAttempts.ShouldBe(maxDeliveryAttempts + 1);
    }

    [InlineData(1)]
    [InlineData(2)]
    [Theory]
    public async Task ReprocessDeadLetterMessageAsync_Publish_ShouldSucceed(int maxDeliveryAttempts)
    {
        // Arrange
        var transport = new InMemoryTransport(_messageChannelRouter, new InMemoryDeadLetterQueue() { MaxDeliveryAttempts = maxDeliveryAttempts });
        var handler = new AlwaysFailsHandler();
        transport.RegisterHandler(handler);
        await transport.StartAsync();

        // Act
        await transport.PublishAsync(new TestMessage { Value = "dlq-clear" });
        await Task.Delay(TimeSpan.FromSeconds(1)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts)); // Wait for redelivery delay attempts.
        var failedMessage = (await transport.GetFailedDeadLetterMessagesAsync()).First();
        await transport.ReprocessDeadLetterMessageAsync(failedMessage.UniqueId);

        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts)); // Wait for redelivery delay attempts.

        // Assert -- DeliveryAttempt count should get set back to maxDeliveryAttempts + 1.
        failedMessage.DeliveryAttempts.ShouldBe(maxDeliveryAttempts + 1);
    }


    [Fact]
    public async Task StopAsync_StopsBackgroundTasks()
    {
        // Arrange
        var handler = new TestMessageHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();

        // Act
        await _transport.StopAsync();

        // Assert - should not be able to publish after stop (channel completed)
        var message = new TestMessage { Value = "after-stop" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _transport.PublishAsync(message).AsTask());
    }

    [Fact]
    public async Task StopAsync_ThrowsIfCancellationRequested()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _transport.StopAsync(cts.Token));
    }

    [Fact]
    public async Task PublishAsync_EnqueuesMessageToDLQ_WhenHandlerThrows()
    {
        // Arrange
        var handler = new AlwaysFailsHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();

        var message = new TestMessage { Value = "dlq-test" };

        // Act
        await _transport.PublishAsync(message);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(3)); // Wait for redelivery delay attempts.

        // Assert - Message content should be the same.
        var dlqMessages = await _transport.GetDeadLetterMessagesAsync();
        dlqMessages.ShouldHaveSingleItem();
        ((TestMessage)dlqMessages.First().Message).Value.ShouldBe("dlq-test");
    }

    [Fact]
    public async Task SendAsync_EnqueuesMessageToDLQ_WhenHandlerThrows()
    {
        // Arrange
        var handler = new AlwaysFailsHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();
        var message = new TestMessage { Value = "send-dlq-test" };

        // Act
        await _transport.SendAsync(message);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(1)); // Wait for redelivery delay attempts.

        // Assert - Should have a single failed message.
        var dlqMessages = await _transport.GetDeadLetterMessagesAsync();
        dlqMessages.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetDeadLetterMessagesAsync_ReturnsAllMessages()
    {
        // Arrange
        var spyHandler = new AlwaysFailsHandler();
        _transport.RegisterHandler(spyHandler);
        await _transport.StartAsync();
        var msg1 = new TestMessage { Value = "msg1" };
        var msg2 = new TestMessage { Value = "msg2" };

        // Act
        await _transport.PublishAsync(msg1);
        await _transport.PublishAsync(msg2);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.

        // Assert - Should have two failed messages.
        var dlqMessages = await _transport.GetDeadLetterMessagesAsync();
        dlqMessages.Count().ShouldBe(2);
    }


    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [Theory]
    public async Task GetFailedDeadLetterMessagesAsync_ReturnsOnlyFailedMessages(int maxDeliveryAttempts)
    {
        // Arrange
        _dlq.MaxDeliveryAttempts = maxDeliveryAttempts;
        var throwHandler = new AlwaysFailsHandler();

        _transport.RegisterHandler(throwHandler);
        await _transport.StartAsync();
        var msg1 = new TestMessage { Value = "fail-once" };

        // Act - Publish message (fails and stays failed).
        await _transport.PublishAsync(msg1);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts)); // Wait for redelivery delay attempts.

        // Assert - Should have failed messages.
        var failedMessages = await _transport.GetFailedDeadLetterMessagesAsync();
        failedMessages.ShouldNotBeEmpty();
    }


    [Fact]
    public async Task RetryFailedMessagesAsync_RetriesMessageAfterBackoffDelay()
    {
        // Arrange
        var handler = new FailsOnceHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();
        var msg = new TestMessage { Value = "retry-test" };

        // Act
        await _transport.PublishAsync(msg);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(1)); // Wait for the redelivery delay attempt 1.

        // Assert
        handler.Attempts.ShouldBeEquivalentTo(2); // Handler called twice: initial + retry.
    }


    [Fact]
    public async Task RetryFailedMessagesAsync_RemovesMessageAfterSuccessfulRetry()
    {
        // Arrange
        var handler = new FailsOnceHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();

        var msg = new TestMessage { Value = "retry-success" };

        // Act
        await _transport.PublishAsync(msg);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(1)); // Wait for the redelivery delay attempt 1.

        // Assert
        var failedMessages = await _transport.GetFailedDeadLetterMessagesAsync();
        failedMessages.ShouldBeEmpty(); // Should be removed after successful retry.
    }

    [Fact]
    public async Task RetryFailedMessagesAsync_KeepsMessageIfMaxDeliveryAttemptsExceeded()
    {
        // Arrange
        var handler = new AlwaysFailsHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();

        var msg = new TestMessage { Value = "max-attempts-test" };

        // Act
        await _transport.PublishAsync(msg);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(3)); // Wait for the redelivery delay attempt 3.

        // Assert
        var failedMessages = await _transport.GetFailedDeadLetterMessagesAsync();
        var failedMsg = Assert.Single(failedMessages);
        Assert.True(failedMsg.DeliveryAttempts > 1, "Should have attempted retries");
    }

    [Fact]
    public async Task RetryFailedMessagesAsync_UpdatesDeliveryAttemptsOnRetry()
    {
        // Arrange
        var handler = new AlwaysFailsHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();

        var msg = new TestMessage { Value = "delivery-attempts" };
        await Task.Delay(100);

        // Act
        await _transport.PublishAsync(msg);
        await Task.Delay(TimeSpan.FromMilliseconds(10)); // Allow handlers and/or DLQ polling to run.
        await Task.Delay(Helpers.GetTotalBackoffDelay(3)); // Wait for the redelivery delay attempt 3.

        // Assert
        var failedMessages = await _transport.GetFailedDeadLetterMessagesAsync();
        var failedMsg = Assert.Single(failedMessages);
        Assert.True(failedMsg.DeliveryAttempts >= 3, "DeliveryAttempts should increment with each retry");
    }


    [Fact]
    public async Task DeliverMessagesAsync_RemovesCompletedTaskFromDictionary_AllowsReprocessingOfSameType()
    {
        // Arrange
        var handler = new TestWaitHandler();
        _transport.RegisterHandler(handler);
        await _transport.StartAsync();

        // Act - Send first message
        await _transport.SendAsync(new TestMessage { Value = "first" });
        await handler.WaitHandledAsync(timeout: TimeSpan.FromSeconds(2));

        // Allow ContinueWith cleanup to execute
        await Task.Delay(100);

        // Send second message of same type
        await _transport.SendAsync(new TestMessage { Value = "second" });
        await handler.WaitHandledAsync(timeout: TimeSpan.FromSeconds(2));

        // Assert
        handler.HandledCount.ShouldBe(2);
    }

    [Fact]
    public async Task ProcessDeadLetterQueueAsync_RetriesSendMessage_AndRemovesFromDLQ()
    {
        // Arrange
        var manager = new MessageChannelRouter();
        var dlq = new InMemoryDeadLetterQueue();
        var handler = new TestMessageHandler();
        var transport = new InMemoryTransport(manager, dlq, new InMemoryTransportOptions());
        transport.RegisterHandler(handler);
        await transport.StartAsync();

        var message = new TestMessage { Value = "retry-send" };

        await dlq.EnqueueAsync(
            message,
            typeof(TestMessage).FullName!,
            MessageMethod.Send,
            new MessageContext(null!),
            DateTime.UtcNow,
            1,
            new Exception("Original failure"),
            CancellationToken.None,
            CancellationToken.None);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await transport.ProcessDeadLetterQueueAsync(cts.Token);

        // Assert
        var remaining = await dlq.GetAllFailedMessagesAsync(CancellationToken.None);
        remaining.ShouldBeEmpty();
    }


    [InlineData(1)]
    [InlineData(2)]
    [Theory]
    public async Task ReprocessDeadLetterQueueAsync_SendShouldInvoke_DlqGetAllMessagesAsync(int maxDeliveryAttempts)
    {
        // Arrange
        var manager = new MessageChannelRouter();
        var dlq = new InMemoryDeadLetterQueue() { MaxDeliveryAttempts = maxDeliveryAttempts };

        var transport = new InMemoryTransport(manager, dlq, new InMemoryTransportOptions());
        var handler = new AlwaysFailsHandler();
        transport.RegisterHandler(handler);
        await transport.StartAsync();
        await transport.SendAsync(new TestMessage { Value = "test-dlq" });
        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts - 1)); // Wait for redelivery delay attempts.

        var firstMessage = (await dlq.GetAllMessagesAsync(CancellationToken.None)).ToList().First();

        // Act
        await transport.ReprocessDeadLetterQueueAsync();
        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts - 1)); // Wait for redelivery delay attempts.

        var secondMessage = (await dlq.GetAllMessagesAsync(CancellationToken.None)).ToList().First();

        // Assert
        firstMessage.Context.MessageId.ShouldBe(secondMessage.Context.MessageId); // Verify it is the same message.
        firstMessage.DeliveryAttempts.ShouldBe(secondMessage.DeliveryAttempts); // First message delivery attempts should equal second delivery attempts.
    }


    [InlineData(1)]
    [InlineData(2)]
    [Theory]
    public async Task ReprocessDeadLetterQueueAsync_PublishShouldInvoke_DlqGetAllMessagesAsync(int maxDeliveryAttempts)
    {
        // Arrange
        var manager = new MessageChannelRouter();
        var dlq = new InMemoryDeadLetterQueue() { MaxDeliveryAttempts = maxDeliveryAttempts };

        var transport = new InMemoryTransport(manager, dlq, new InMemoryTransportOptions());
        var handler = new AlwaysFailsHandler();
        transport.RegisterHandler(handler);
        await transport.StartAsync();
        await transport.PublishAsync(new TestMessage { Value = "test-dlq" });
        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts - 1)); // Wait for redelivery delay attempts.

        var firstMessage = (await dlq.GetAllMessagesAsync(CancellationToken.None)).ToList().First();

        // Act
        await transport.ReprocessDeadLetterQueueAsync();
        await Task.Delay(Helpers.GetTotalBackoffDelay(maxDeliveryAttempts - 1)); // Wait for redelivery delay attempts.

        var secondMessage = (await dlq.GetAllMessagesAsync(CancellationToken.None)).ToList().First();

        // Assert
        firstMessage.Context.MessageId.ShouldBe(secondMessage.Context.MessageId); // Verify it is the same message.
        firstMessage.DeliveryAttempts.ShouldBe(secondMessage.DeliveryAttempts); // First message delivery attempts should equal second delivery attempts.
    }
}
