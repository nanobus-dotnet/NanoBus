namespace NanoBus.IntegrationTests.Transports;

[ExcludeFromCodeCoverage]
public class MessageChannelRouterTests
{
    private readonly IMessage _message = new TestMessage { Value = "channel-manager-tests" };
    private readonly MessageContext _messageContext;

    public MessageChannelRouterTests()
    {
        _messageContext = new MessageContext(null!, "msg-1", "correlation-1", null);
    }

    [Fact]
    public void Constructor_WithDefaultCapacity_ShouldSetCapacityTo100()
    {
        // Act
        var router = new MessageChannelRouter();

        // Assert
        router.BoundedChannelCapacity.ShouldBe(100);
    }

    [Fact]
    public void Constructor_WithCustomCapacity_ShouldSetCapacityToCustomValue()
    {
        // Act
        var router = new MessageChannelRouter(50);

        // Assert
        router.BoundedChannelCapacity.ShouldBe(50);
    }


    [Fact]
    public async Task EnqueueMessageAsync_WithNewMessageType_ShouldReturnTrue()
    {
        // Arrange
        var router = new MessageChannelRouter();

        // Act
        var result = await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }


    [Fact]
    public async Task EnqueueMessageAsync_WithClosedChannel_ShouldReturnFalse()
    {
        // Arrange
        var router = new MessageChannelRouter();

        // Act

        _ = await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);

        router.CompleteAll();

        var result = await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task EnqueueMessageAsync_WithMultipleMessages_ShouldEnqueueAll()
    {
        // Arrange
        var router = new MessageChannelRouter();
        var messages = new[] { _message, _message, _message };

        // Act
        var results = new List<bool>();
        foreach (var msg in messages)
        {
            var isEnqueued = await router.EnqueueMessageAsync("TestType", msg, MessageMethod.Send, _messageContext, CancellationToken.None);
            results.Add(isEnqueued);
        }

        // Assert
        results.ShouldAllBe(x => true);
    }

    [Fact]
    public async Task EnqueueMessageAsync_WithDifferentMessageTypes_ShouldCreateSeparateChannels()
    {
        // Arrange
        var router = new MessageChannelRouter();


        // Act
        await router.EnqueueMessageAsync("Type1", _message, MessageMethod.Send, _messageContext, CancellationToken.None);
        await router.EnqueueMessageAsync("Type2", _message, MessageMethod.Publish, _messageContext, CancellationToken.None);

        // Assert
        var type1Messages = new List<(IMessage Message, MessageMethod Method, MessageContext Context, CancellationToken Token)>();
        var type2Messages = new List<(IMessage Message, MessageMethod Method, MessageContext Context, CancellationToken Token)>();

        await foreach (var msg in router.ReadAllAsync("Type1", CancellationToken.None).Take(1))
        {
            type1Messages.Add(msg);
        }

        await foreach (var msg in router.ReadAllAsync("Type2", CancellationToken.None).Take(1))
        {
            type2Messages.Add(msg);
        }

        type1Messages.Count.ShouldBe(1);
        type2Messages.Count.ShouldBe(1);
        type1Messages[0].Method.ShouldBe(MessageMethod.Send);
        type2Messages[0].Method.ShouldBe(MessageMethod.Publish);
    }

    [Fact]
    public async Task ReadAllAsync_NoMessageRegistered_ShouldThrowException()
    {
        // Arrange
        var router = new MessageChannelRouter();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => router.ReadAllAsync("Type1", CancellationToken.None));
    }

    [Fact]
    public async Task EnqueueMessageAsync_PreservesMessageMethod()
    {
        // Arrange
        var router = new MessageChannelRouter();

        // Act - Send
        await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);

        var sendMessage = await router.ReadAllAsync("TestType", CancellationToken.None).FirstAsync();

        // Act - Publish
        await router.EnqueueMessageAsync("TestType2", _message, MessageMethod.Publish, _messageContext, CancellationToken.None);

        var publishMessage = await router.ReadAllAsync("TestType2", CancellationToken.None).FirstAsync();

        // Assert - All passed
        sendMessage.Method.ShouldBe(MessageMethod.Send);
        publishMessage.Method.ShouldBe(MessageMethod.Publish);
    }


    [Fact]
    public async Task ReadAllAsync_WithEmptyChannel_ShouldWaitForMessages()
    {
        // Arrange
        var router = new MessageChannelRouter();

        var readTask = Task.Run(async () =>
        {
            var messages = new List<(IMessage, MessageMethod, MessageContext, CancellationToken)>();
            await foreach (var msg in router.ReadAllAsync("TestType", CancellationToken.None).Take(1))
            {
                messages.Add(msg);
            }
            return messages;
        });

        // Act
        await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);

        var result = await readTask;

        // Assert
        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ReadAllAsync_WithMultipleMessages_ShouldReadInOrder()
    {
        // Arrange
        var router = new MessageChannelRouter();

        var messageContexts = new[]
        {
            new MessageContext(null!, "msg-1", "correlation-1", null),
            new MessageContext(null!, "msg-2", "correlation-1", null),
            new MessageContext(null!, "msg-3", "correlation-1", null)
        };

        // Act
        foreach (var ctx in messageContexts)
        {
            await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, ctx, CancellationToken.None);
        }

        var readMessages = new List<MessageContext>();
        await foreach (var msg in router.ReadAllAsync("TestType", CancellationToken.None).Take(3))
        {
            readMessages.Add(msg.Context);
        }

        // Assert
        readMessages.Count.ShouldBe(3);
        readMessages[0].MessageId.ShouldBe("msg-1");
        readMessages[1].MessageId.ShouldBe("msg-2");
        readMessages[2].MessageId.ShouldBe("msg-3");
    }

    [Fact]
    public async Task ReadAllAsync_ShouldIncludeMessageAndContext()
    {
        // Arrange
        var router = new MessageChannelRouter();
        var context = new MessageContext(null!, "test-id", "correlation-id", null);

        // Act
        await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, context, CancellationToken.None);
        var (message, method, returnedContext, _) = await router.ReadAllAsync("TestType", CancellationToken.None).FirstAsync();

        // Assert
        message.ShouldBe(_message);
        method.ShouldBe(MessageMethod.Send);
        returnedContext.MessageId.ShouldBe("test-id");
        returnedContext.CorrelationId.ShouldBe("correlation-id");
    }


    [Fact]
    public async Task GetPendingMessageTypesAsync_ShouldReturnNotificationAfterEnqueue()
    {
        // Arrange
        var router = new MessageChannelRouter();
        var pendingTypes = new List<string>();

        var notificationTask = Task.Run(async () =>
        {
            await foreach (var type in router.GetPendingMessageTypesAsync(CancellationToken.None).Take(1))
            {
                pendingTypes.Add(type);
            }
        });

        // Act
        await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);
        await notificationTask;

        // Assert
        pendingTypes.ShouldContain("TestType");
    }

    [Fact]
    public async Task GetPendingMessageTypesAsync_WithMultipleTypes_ShouldReturnAllNotifications()
    {
        // Arrange
        var router = new MessageChannelRouter();
        var pendingTypes = new List<string>();

        var notificationTask = Task.Run(async () =>
        {
            await foreach (var type in router.GetPendingMessageTypesAsync(CancellationToken.None).Take(3))
            {
                pendingTypes.Add(type);
            }
        });

        // Act
        await router.EnqueueMessageAsync("Type1", _message, MessageMethod.Send, _messageContext, CancellationToken.None);
        await router.EnqueueMessageAsync("Type2", _message, MessageMethod.Send, _messageContext, CancellationToken.None);
        await router.EnqueueMessageAsync("Type3", _message, MessageMethod.Send, _messageContext, CancellationToken.None);

        await notificationTask;

        // Assert
        pendingTypes.Count.ShouldBe(3);
        pendingTypes.ShouldContain("Type1");
        pendingTypes.ShouldContain("Type2");
        pendingTypes.ShouldContain("Type3");
    }

    [Fact]
    public async Task GetPendingMessageTypesAsync_WithSameTypeTwice_ShouldReturnBothNotifications()
    {
        // Arrange
        var router = new MessageChannelRouter();
        var pendingTypes = new List<string>();

        var notificationTask = Task.Run(async () =>
        {
            await foreach (var type in router.GetPendingMessageTypesAsync(CancellationToken.None).Take(2))
            {
                pendingTypes.Add(type);
            }
        });

        // Act
        await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);
        await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);

        await notificationTask;

        // Assert
        pendingTypes.Count.ShouldBe(2);
        pendingTypes.ShouldAllBe(x => x == "TestType");
    }



    [Fact]
    public async Task CompleteAll_ShouldPreventEnqueue()
    {
        // Arrange
        var router = new MessageChannelRouter();

        // Act
        router.CompleteAll();

        // Assert
        await Assert.ThrowsAsync<ChannelClosedException>(async () => await router.EnqueueMessageAsync("TestType", _message,
            MessageMethod.Send, _messageContext, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAll_ShouldCompleteNotificationChannel()
    {
        // Arrange
        var router = new MessageChannelRouter();
        var notifications = new List<string>();

        var notificationTask = Task.Run(async () =>
        {
            await foreach (var type in router.GetPendingMessageTypesAsync(CancellationToken.None))
            {
                notifications.Add(type);
            }
        });

        // Act
        await router.EnqueueMessageAsync("TestType", _message, MessageMethod.Send, _messageContext, CancellationToken.None);
        router.CompleteAll();

        await notificationTask;

        // Assert
        notifications.Count.ShouldBe(1);
        notifications[0].ShouldBe("TestType");
    }

    [Fact]
    public async Task CompleteAll_ShouldCompleteAllTypeChannels()
    {
        // Arrange
        var router = new MessageChannelRouter();
        await router.EnqueueMessageAsync("Type1", _message, MessageMethod.Send, _messageContext, CancellationToken.None);
        await router.EnqueueMessageAsync("Type2", _message, MessageMethod.Send, _messageContext, CancellationToken.None);

        // Act
        router.CompleteAll();

        var type1Messages = new List<(IMessage, MessageMethod, MessageContext, CancellationToken)>();
        var type2Messages = new List<(IMessage, MessageMethod, MessageContext, CancellationToken)>();

        await foreach (var msg in router.ReadAllAsync("Type1", CancellationToken.None))
        {
            type1Messages.Add(msg);
        }

        await foreach (var msg in router.ReadAllAsync("Type2", CancellationToken.None))
        {
            type2Messages.Add(msg);
        }

        // Assert
        type1Messages.Count.ShouldBe(1);
        type2Messages.Count.ShouldBe(1);
    }
}
