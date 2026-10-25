using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NanoBus.Abstractions;
using NanoBus.Core;

namespace NanoBus.Transports.Demo;

/// <summary>
/// Extensions for the demo transport.
/// </summary>
public static class DemoServiceBusExtensions
{
    /// <summary>
    /// Adds the demo transport.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="options">The transport options to configure.</param>
    /// <param name="configure">Callback to late-bind bus configuration.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddDemoServiceBus(
        this IServiceCollection services,
        Action<TransportOptions>? options = null,
        Action<IServiceProvider, ITransport>? configure = null
        )
    {
        services.AddServiceBus(configure);

        services.AddSingleton<ITransport, DemoTransport>(sp =>
        {
            var logger = sp.GetService<ILogger<DemoTransport>>();
            var transportOptions = new TransportOptions();
            options?.Invoke(transportOptions);
            return new DemoTransport(transportOptions, logger);
        });

        return services;
    }
}
