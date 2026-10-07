using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NanoBus.Abstractions;
using NanoBus.Core;

namespace NanoBus.Transports.InMemory;

/// <summary>
/// Extensions for the demo transport.
/// </summary>
public static class InMemoryServiceBusExtensions
{
    /// <summary>
    /// Adds the demo transport.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="options">The transport options to configure.</param>
    /// <param name="configure">Callback to late-bind bus configuration.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddInMemoryServiceBus(this IServiceCollection services, 
        Action<InMemoryTransportOptions>? options = null, 
        Action<IServiceProvider, ITransport>? configure = null)
    {
        services.AddServiceBus(configure);

        services.AddSingleton<ITransport, InMemoryTransport>(sp =>
        {
            var logger = sp.GetService<ILogger<InMemoryTransport>>();
            var transportOptions = new InMemoryTransportOptions();
            options?.Invoke(transportOptions);
            return new InMemoryTransport(new InMemoryDeadLetterQueue { MaxRetries = transportOptions.MaxRetries }, transportOptions, logger);
        });

        return services;
    }
}
