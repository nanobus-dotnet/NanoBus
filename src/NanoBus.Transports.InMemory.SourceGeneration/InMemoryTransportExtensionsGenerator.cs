using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Generates the demo transport extensions.
/// </summary>
[Generator]
public class InMemoryTransportExtensionsGenerator : IIncrementalGenerator
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
                "InMemoryTransportExtensions.g.cs",
                SourceText.From("""
                    // Auto-generated code
                    #nullable enable
                    
                    using System;
                    using Microsoft.Extensions.DependencyInjection;
                    using NanoBus.Abstractions;
                    using NanoBus.Core;
                    using NanoBus.Transports.InMemory;
                    
                    public static class InMemoryTransportExtensions
                    {
                        public static IServiceCollection AddInMemoryServiceBus(this IServiceCollection services, Action<InMemoryTransportOptions>? options = null)
                        {
                            services.RegisterMessageHandlerTypes();

                            services.AddInMemoryServiceBus(options, (sp, transport) =>
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
