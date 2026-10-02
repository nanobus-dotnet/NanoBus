using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NanoBus.Abstractions;

namespace NanoBus.Core.DependencyInjection;

/// <summary>
/// Core service bus extensions.
/// </summary>
public static class CoreServiceBusExtensions
{
    /// <summary>
    /// Adds the core service bus.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure">Callback to late-bind the service bus configuration.</param>
    /// <returns></returns>
    public static IServiceCollection AddServiceBus(this IServiceCollection services, Action<IServiceProvider, ITransport>? configure = null)
    {
        services.AddSingleton<IServiceBus>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ServiceBus>>();
            var transport = sp.GetRequiredService<ITransport>();
            var bus = new ServiceBus(transport, logger);
            configure?.Invoke(sp, transport);
            
            return bus;
        });

        return services;
    }
}
