namespace NanoBus.UnitTests;


[ExcludeFromCodeCoverage]

public class MessageHandlerRegistryTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly MessageHandlerRegistry _registry;

    public MessageHandlerRegistryTests()
    {
        _mockLogger = new Mock<ILogger>();
        _registry = new MessageHandlerRegistry(_mockLogger.Object);
    }

    public class TestMessage : IMessage { }
    public class AnotherTestMessage : IMessage { }

    [Fact]
    public void RegisterHandler_WithValidHandler_ShouldRegisterSuccessfully()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler<TestMessage>>();

        // Act
        _registry.RegisterHandler(handlerMock.Object);

        // Assert
        _registry.HandlerExists(typeof(TestMessage).FullName!).ShouldBeTrue();
    }


    [Fact]
    public void RegisterHandler_ShouldLogInformation()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler<TestMessage>>();

        // Act
        _registry.RegisterHandler(handlerMock.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Registered handler type")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RegisterHandler_WithMultipleHandlers_ShouldRegisterAll()
    {
        // Arrange
        var handler1 = new Mock<IMessageHandler<TestMessage>>();
        var handler2 = new Mock<IMessageHandler<TestMessage>>();

        // Act
        _registry.RegisterHandler(handler1.Object);
        _registry.RegisterHandler(handler2.Object);

        // Assert
        var handlers = _registry.GetHandlers(typeof(TestMessage).FullName!);
        handlers.Count.ShouldBe(2);
    }

    [Fact]
    public async Task RegisterHandler_ShouldWrapHandlerCorrectly()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler<TestMessage>>();
        var message = new TestMessage();
        var context = new MessageContext(null!, "msg-1", "correlation-1", null);
        var cancellationToken = CancellationToken.None;

        handlerMock
            .Setup(x => x.HandleAsync(message, context, cancellationToken))
            .Returns(new ValueTask())
            .Verifiable();

        _registry.RegisterHandler(handlerMock.Object);

        // Act
        var handlers = _registry.GetHandlers(typeof(TestMessage).FullName!);
        var handler = handlers[0];
        await handler(message, context, cancellationToken);

        // Assert
        handlerMock.Verify(x => x.HandleAsync(message, context, cancellationToken), Times.Once);
    }

    [Fact]
    public void HandlerExists_WithRegisteredHandler_ShouldReturnTrue()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler<TestMessage>>();
        _registry.RegisterHandler(handlerMock.Object);

        // Act
        var result = _registry.HandlerExists(typeof(TestMessage).FullName!);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HandlerExists_WithUnregisteredHandler_ShouldReturnFalse()
    {
        // Act
        var result = _registry.HandlerExists(typeof(TestMessage).FullName!);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HandlerExists_WithDifferentMessageType_ShouldReturnFalse()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler<TestMessage>>();
        _registry.RegisterHandler(handlerMock.Object);

        // Act
        var result = _registry.HandlerExists(typeof(AnotherTestMessage).FullName!);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetHandlers_WithRegisteredHandlers_ShouldReturnHandlers()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler<TestMessage>>();
        _registry.RegisterHandler(handlerMock.Object);

        // Act
        var handlers = _registry.GetHandlers(typeof(TestMessage).FullName!);

        // Assert
        handlers.ShouldNotBeNull();
        handlers.Count.ShouldBe(1);
    }

    [Fact]
    public void GetHandlers_WithUnregisteredType_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            _registry.GetHandlers(typeof(TestMessage).FullName!));

        ex.Message.ShouldContain("No handlers registered for type");
    }

    [Fact]
    public void GetHandlers_WithUnregisteredType_ShouldLogError()
    {
        // Act
        Should.Throw<InvalidOperationException>(() =>
            _registry.GetHandlers(typeof(TestMessage).FullName!));

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No handlers registered for type")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetHandlers_ShouldReturnIReadOnlyList()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler<TestMessage>>();
        _registry.RegisterHandler(handlerMock.Object);

        // Act
        var handlers = _registry.GetHandlers(typeof(TestMessage).FullName!);

        // Assert
        handlers.ShouldBeAssignableTo<IReadOnlyList<Func<IMessage, MessageContext, CancellationToken, Task>>>();
    }

    [Fact]
    public void RegisterHandler_MultipleDifferentTypes_ShouldMaintainSeparateCollections()
    {
        // Arrange
        var handler1 = new Mock<IMessageHandler<TestMessage>>();
        var handler2 = new Mock<IMessageHandler<AnotherTestMessage>>();

        // Act
        _registry.RegisterHandler(handler1.Object);
        _registry.RegisterHandler(handler2.Object);

        // Assert
        _registry.HandlerExists(typeof(TestMessage).FullName!).ShouldBeTrue();
        _registry.HandlerExists(typeof(AnotherTestMessage).FullName!).ShouldBeTrue();
        _registry.GetHandlers(typeof(TestMessage).FullName!).Count.ShouldBe(1);
        _registry.GetHandlers(typeof(AnotherTestMessage).FullName!).Count.ShouldBe(1);
    }

    [Fact]
    public void RegisterHandler_WithNullLogger_ShouldUseNullLogger()
    {
        // Arrange
        var registry = new MessageHandlerRegistry(null);
        var handlerMock = new Mock<IMessageHandler<TestMessage>>();

        // Act & Assert (should not throw)
        Should.NotThrow(() => registry.RegisterHandler(handlerMock.Object));
        registry.HandlerExists(typeof(TestMessage).FullName!).ShouldBeTrue();
    }
}

