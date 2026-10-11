using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NanoBus.Transports.InMemory;


/// <summary>
/// Generates the demo transport extensions.
/// </summary>
[ExcludeFromCodeCoverage]
[Generator]
public class InMemoryTransportExtensionsGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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