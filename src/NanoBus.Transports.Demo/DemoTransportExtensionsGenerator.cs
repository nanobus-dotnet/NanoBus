using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NanoBus.Transports.Demo;


/// <summary>
/// Generates the demo transport extensions.
/// </summary>
[ExcludeFromCodeCoverage]
[Generator]
public class DemoTransportExtensionsGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // If this is enabled, a debugger prompt will be displayed when the solution is build.
        //if (!System.Diagnostics.Debugger.IsAttached)
        //    System.Diagnostics.Debugger.Launch();

        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(
                "DemoTransportExtensions.g.cs",
                SourceText.From("""
                // Auto-generated code
                #nullable enable
                
                using System;
                using Microsoft.Extensions.DependencyInjection;
                using NanoBus.Abstractions;
                using NanoBus.Core;
                using NanoBus.Transports.Demo;
                
                public static class DemoTransportExtensions
                {
                    public static IServiceCollection AddDemoServiceBus(this IServiceCollection services, Action<TransportOptions>? options = null)
                    {
                        services.RegisterMessageHandlerTypes();

                        services.AddDemoServiceBus(options, (sp, transport) =>
                        {
                            // Wait until the bus is available before adding message handlers.
                            sp.AddMessageHandlersTo(transport);
                        });
                        
                        return services;
                    }
                }
                """, Encoding.UTF8));
        });
    }
}