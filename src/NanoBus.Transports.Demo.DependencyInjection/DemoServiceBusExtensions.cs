using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NanoBus.Abstractions;
using NanoBus.Core.DependencyInjection;

namespace NanoBus.Transports.Demo.DependencyInjection;

/// <summary>
/// Extensions for the demo transport.
/// </summary>
public static class DemoServiceBusExtensions
{
    /// <summary>
    /// Adds the demo transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Callback to late-bind bus configuration.</param>
    /// <param name="options">The transport options.</param>
    /// <returns></returns>
    public static IServiceCollection AddDemoServiceBus(this IServiceCollection services, Action<IServiceProvider, ITransport>? configure = null, TransportOptions? options = null)
    {
        services.AddServiceBus(configure);

        services.AddSingleton<ITransport, DemoTransport>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DemoTransport>>();
            return new DemoTransport(options, logger);
        });

        return services;
    }
}
