# NanoBus

Compile-time message routing for .NET. Zero reflection, zero runtime surprises.

**NanoBus** is a message bus for .NET services that value compile-time safety, cold startup performance, and AOT compatibility. Handlers are auto-registered via source generation—no runtime magic, no surprising reflection costs.

This is useful if you're working on serverless functions (cold starts matter), shipping containers (startup speed counts), or just prefer knowing exactly what your code does at runtime.


## Features

- **Compile-time handler registration** via source generation
- **Zero reflection** at runtime
- **Native AOT compatible** (.NET 8+)
- **Automatic DLQ + retry** with exponential backoff
- **Graceful shutdown** with cancellation tokens

## Quick Start

Want to see it in action? Check out the Learning Path below—it walks you through real examples.


## Performance

Here's what you can expect with NanoBus on .NET 8.0:

| Scenario | Mean Latency | Memory per Call |
|----------|--------------|-----------------|
| `PublishAsync` (enqueue only) | **1.44 μs** | **1022 B** |

**What's being measured:**
- Time to enqueue a message into the in-memory transport
- Handler execution happens in the background, so it's not included here
- One handler registered, no competing publishers

**Keep in mind:**
- This is for in-process messaging within a single deployment
- If you're looking to compare with MassTransit or NServiceBus, those are built for distributed systems across multiple machines—different problem to solve
- NanoBus shines when you want compile-time safety and AOT compatibility in a single application

Want to run the benchmarks yourself?

```bash
dotnet run -p Benchmarks/NanoBus.Benchmarks -c Release

```


## Learning Path

**New to NanoBus?** Start here:

1. **[Examples/DemoConsole](./Examples/DemoConsole)** — Understand the mechanics
   - Manual handler registration so you see how it all fits together
   - Explore `IMessageHandler<T>`, `IBus`, `MessageContext`

2. **[Examples/DemoWorker](./Examples/DemoWorker)** — Same as DemoConsole, but with real infrastructure
   - Proper dependency injection
   - `BackgroundService` hosting
   - Graceful shutdown and cancellation tokens
 
3. **[Examples/InMemoryWorker](./Examples/InMemoryWorker)** — What a production setup looks like
   - Realistic message types and schemas
   - Error handling and logging
   - Using the dead-letter queue


## How NanoBus Compares

| Feature | NanoBus | MassTransit | NServiceBus |
|---------|---------|-------------|------------|
| **Handler Registration** | Source-generated | Runtime reflection | Runtime reflection |
| **AOT/Native Binary** | ✓ Yes | ✗ No | ✗ No |
| **Runtime Reflection** | ✓ None | ✗ Heavy | ✗ Heavy |
| **Serverless-Ready** | ✓ Yes | ◐ Possible | ◐ Possible |
| **Learning Curve** | ✓ Easy | ✗ Steep | ✗ Steep |

A few things to know:
- NanoBus generates handler registration code at compile-time; it doesn't automatically route messages across services.
- Message publication and delivery happen in-process.
- If you need cross-service messaging, MassTransit and NServiceBus do that well—NanoBus doesn't (yet).

## Compare Setup with other frameworks
### NanoBus

```csharp
// Step 0: NuGet packages
// Install-Package NanoBus.Transports.InMemory

// Program.cs — absolutely nothing. Zero ceremony.
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInMemoryServiceBus();
var host = builder.Build();
await host.RunAsync();

// Handler (simple, auto-discovered)
public class OrderSubmittedHandler : IMessageHandler<OrderSubmitted>
{
    public ValueTask HandleAsync(OrderSubmitted message, MessageContext context, CancellationToken cancellationToken = default)
    {
        // Handle
    }
}
```

### NServiceBus (with In-Memory Transport)
```csharp
// Step 0: NuGet packages
// Install-Package NServiceBus
// Install-Package NServiceBus.Transport.InMemory

// Program.cs
var config = new EndpointConfiguration("Orders");
var transport = config.UseTransport<InMemoryTransport>();

// Step 2: Configure polling interval
transport.PollingTime(TimeSpan.FromSeconds(1));

// Step 3: Still need routing (even in-process)
transport.Routing().RouteToEndpoint(typeof(OrderPlaced), "Orders");

// Step 4: Reflection-based handler discovery
config.Conventions().DefiningCommandsAs(t => 
    t.Namespace == "Messages" && t.Name.EndsWith("Command"));

var endpoint = await Endpoint.Start(config);

// Handler (same ceremony)
public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        // Handle
    }
}
```

**MassTransit (with In-Memory Transport)**
```csharp
// Step 0: NuGet packages
// Install-Package MassTransit

// Program.cs
var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;

services.AddMassTransit(x =>
{
    // Step 1: Add consumers with reflection scanning
    x.AddConsumers(typeof(Program).Assembly);
    
    // Step 2: Configure in-memory transport
    x.UsingInMemory((context, cfg) =>
    {
        // Step 3: Auto-configure all endpoints
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
await host.RunAsync();

// Consumer (interface-based)
public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        // Handle
    }
}
```


## What NanoBus Is Not

- **Not distributed yet.** Right now it's in-process only. Distributed transports (RabbitMQ, Azure Service Bus) could happen if there's demand. You can use NanoBus as the handler layer today and bridge to a broker later if needed.

- **Not a saga engine.** No built-in support for long-running workflows or distributed transactions.

- **Not trying to replace MassTransit or NServiceBus.** Those are built for complex, distributed systems. NanoBus solves a different problem: fast, simple, compile-time-safe messaging inside a single application.


## Do I Need Reflection?

NanoBus is AOT-ready by default. That said, if you're using reflection-based tools (dynamic proxies, Moq, etc.), you can still use NanoBus—just don't compile with `PublishAot=true`. The core library itself has zero reflection.

---
## NanoBus Code Structure
The rough structure of NanoBus code is the following:
- `NanoBus.Abstractions` - Core abstractions necessary for all NanoBus implementations.
- `NanoBus.Core` - Core implementations.
- `NanoBus.Transports.Demo` - Simple implementation for demoing/learning NanoBus.
- `NanoBus.Transports.InMemory` - In-memory implementation of NanoBus.

---

## Getting Started

1. Install via NuGet:

```bash
dotnet add package NanoBus.Transports.InMemory
```

```bash
dotnet run --project Examples/DemoConsole
```

Questions or ideas? See [CONTRIBUTING.md](./CONTRIBUTING.md), [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md), and [CONTRIBUTORS.md](./CONTRIBUTORS.md).

