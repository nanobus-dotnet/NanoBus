using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

/// <summary>
/// Generates message handler extensions.
/// </summary>
[Generator]
public class MessageHandlerExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // If this is enabled, a debugger prompt will be displayed when the solution is built.
        //if (!System.Diagnostics.Debugger.IsAttached)
        //    System.Diagnostics.Debugger.Launch();

        var handlerProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (syntax, _) => IsHandler(syntax),
                transform: (ctx, _) => GetHandlerInfo(ctx)
            )
            .Where(x => x != null)
            .Collect();

        context.RegisterSourceOutput(handlerProvider, (ctx, handlers) =>
        {
            GenerateRegistrationExtensions(ctx, [.. handlers.Cast<HandlerInfo>()]);
        });
    }

    /// <summary>
    /// Determines if a class is IMessage handler and is not abstract.
    /// </summary>
    private static bool IsHandler(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax cds
            && !cds.Modifiers.Any(m => m.Text == "abstract")
            && cds.BaseList?.Types.Any(t =>
            t.Type.ToString().StartsWith("IMessageHandler<")) == true;
    }

    /// <summary>
    /// Gets the metadata of the IMessageHandler.
    /// </summary>
    private static HandlerInfo? GetHandlerInfo(GeneratorSyntaxContext context)
    {
        var cds = (ClassDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(cds) is not INamedTypeSymbol symbol)
            return null;

        // Make sure it's IMessageHandler.
        var handlerInterface = symbol.Interfaces
            .FirstOrDefault(i => i.Name == "IMessageHandler" && i.IsGenericType);

        if (handlerInterface is null)
            return null;
        
        // Check the message type.
        var messageType = handlerInterface.TypeArguments[0];

        // Return the handler metadata.
        return new HandlerInfo
        {
            HandlerClassName = symbol.Name,
            HandlerNamespace = symbol.ContainingNamespace.ToDisplayString(),
            MessageTypeName = messageType.Name,
            MessageTypeNamespace = messageType.ContainingNamespace.ToDisplayString(),
            FullyQualifiedHandler = symbol.ToDisplayString(),
            FullyQualifiedMessage = messageType.ToDisplayString()
        };
    }

    /// <summary>
    /// Generates the extension class and methods.
    /// </summary>
    private static void GenerateRegistrationExtensions(SourceProductionContext context, List<HandlerInfo> handlers)
    {
        if (!handlers.Any())
            return;

        var source = new StringBuilder();
        source.AppendLine("""
            // Auto-generated code
            #nullable enable
    
            using System;
            using Microsoft.Extensions.DependencyInjection;
            using NanoBus.Abstractions;

            public static class MessageHandlerExtensions
            {
            """);

        source.AppendLine("""
            /// <summary>
            /// Adds the handlers to the service bus.
            // </summary>
            public static IServiceProvider AddMessageHandlersTo(this IServiceProvider serviceProvider, ITransport transport)
            {
                using var scope = serviceProvider.CreateScope();
        """);

        foreach (var handler in handlers.OrderBy(h => h.HandlerClassName))
        {
            source.AppendLine($$"""
                    // Register {{handler.HandlerClassName}} for {{handler.MessageTypeName}}.
                    var handler_{{handler.HandlerClassName}} = scope.ServiceProvider.GetRequiredService<{{handler.FullyQualifiedHandler}}>();
                    transport.RegisterHandler<{{handler.FullyQualifiedMessage}}>(handler_{{handler.HandlerClassName}});
            """);
        }

        source.AppendLine("""
            return serviceProvider;
        }

    """);

        source.AppendLine("""
        /// <summary>
        /// Registers all the handler types in the DI container.
        /// </summary>
        public static IServiceCollection RegisterMessageHandlerTypes(this IServiceCollection services)
        {
    """);

        foreach (var handler in handlers.OrderBy(h => h.HandlerClassName))
        {
            source.AppendLine($$"""
                // Add {{handler.HandlerClassName}} to the container.
                services.AddScoped<{{handler.FullyQualifiedHandler}}>();
        """);
        }

        source.AppendLine("""
            return services;
        }
    }
    """);

        context.AddSource("MessageHandlerExtensions.g.cs", source.ToString());
    }


    /// <summary>
    /// Stores metadata about a message handler.
    /// </summary>
    private record HandlerInfo
    {
        public string HandlerClassName { get; set; } = "";
        public string HandlerNamespace { get; set; } = "";
        public string MessageTypeName { get; set; } = "";
        public string MessageTypeNamespace { get; set; } = "";
        public string FullyQualifiedHandler { get; set; } = "";
        public string FullyQualifiedMessage { get; set; } = "";
    }
}