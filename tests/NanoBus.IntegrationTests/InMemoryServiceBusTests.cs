namespace NanoBus.IntegrationTests;

[ExcludeFromCodeCoverage]
public class InMemoryServiceBusTests
{
    [Fact]
    public void Extension_AddDemoServiceBusDefault()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInMemoryServiceBus();

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IServiceBus>();

        // Assert
        bus.ShouldNotBeNull();
    }

    [Fact]
    public void Extension_AddDemoServiceBus()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInMemoryServiceBus(options => options.MaxDeliveryAttempts = 5, (sp, t) =>
        {
            using var scope = sp.CreateScope();
            t.RegisterHandler(new TestMessageHandler());
        });

        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IServiceBus>();

        // Assert
        bus.ShouldNotBeNull();
    }
}
