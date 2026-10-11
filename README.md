# NanoBus

Compile-time message routing for .NET. Zero reflection, zero runtime surprises.


**NanoBus** is a message bus for .NET services that value **compile-time safety, cold startup performance, and AOT compatibility**. Handlers are auto-registered via source generation. Perfect for microservices, serverless, and tightly-coupled systems.

- **Targets serverless engineers** (Lambda cold starts matter)
- **Targets cloud-native teams** (containers, startup speed)
- **Targets AOT evangelists** (.NET 8+ Native AOT crowd)
- **Targets teams that hate "magic"** (explicit, visible registration)

## Features

- **Compile-time handler registration** via source generation
- **Zero reflection** at runtime
- **Native AOT compatible** (.NET 8+)
- **Automatic DLQ + retry** with exponential backoff
- **Graceful shutdown** with cancellation tokens

### Quick Start

See Learning Path examples for complete code.

### Learning Path

**New to NanoBus?** Start here:

1. **[Examples/DemoConsole](./Examples/DemoConsole)** — Understand the mechanics
   - Manual handler registration
   - Explore `IMessageHandler<T>`, `IServiceBus`, `MessageContext`

2. **[Examples/DemoWorker](./Examples/DemoWorker)** — DemoConsole converted to proper hosting (shows the framework mechanics with real infrastructure)
   - Proper dependency injection
   - `BackgroundService` hosting
   - Graceful shutdown, cancellation tokens
 
3. **[Examples/InMemoryWorker](./Examples/InMemoryWorker)** — Production-ready, shows best practices (realistic message types, error handling, logging)


## How NanoBus Compares

| Feature | NanoBus | MassTransit | NServiceBus |
|---------|---------|-------------|------------|
| **Handler Registration** | Source-generated | Runtime reflection | Runtime reflection |
| **AOT/Native Binary** | ✅ Yes | ❌ No | ❌ No |
| **Runtime Reflection** | ✅ None | ❌ Heavy | ❌ Heavy |
| **Serverless-Ready** | ✅ Yes | ⚠️ Possible | ⚠️ Possible |
| **Learning Curve** | ✅ Easy | ❌ Steep | ❌ Steep

*NanoBus generates handler registration code at compile-time; 
it does not automatically route messages across services. 
Message publication/delivery is in-process.*

*Benchmarks coming soon*
## What NanoBus Is Not
- **Not yet distributed.** Currently in-process only. 
  Distributed transport support (RabbitMQ, Azure Service Bus) 
  is planned based on demand. Use NanoBus as the handler layer 
  today, bridge to a broker later.
- **Not a saga engine.** No current built-in support for long-running workflows.
- **Not a replacement for MassTransit/NServiceBus.** Those are more feature-rich for complex, distributed scenarios.

NanoBus solves a different problem: **fast, simple, compile-time-safe messaging within a single deployment.**

### Reflection Required?

NanoBus is AOT-ready by default. If your project needs reflection-based features (dynamic proxies, Moq mocking, etc.), you can still use NanoBus—just don't compile with `PublishAot=true`. The core library has zero reflection.

---
## NanoBus Code Structure
The rough structure of NanoBus code is the following:
- `NanoBus.Abstractions` - Core abstractions necessary for all NanoBus implementations.
- `NanoBus.Core` - Core implementations.
- `NanoBus.Transports.Demo` - Simple implementation for demoing/learning NanoBus.
- `NanoBus.Transports.InMemory` - In-memory implementation of NanoBus.

---

## Getting Started

1. Install the NuGet package:

`dotnet add package NanoBus.Transports.InMemory`

2. Define a message and handler (see Quick Start above).
3. Run an example: 

`dotnet run --project Examples/DemoConsole`

*For details on contributing, see [CONTRIBUTING.md](./CONTRIBUTING.md)*

*For the code of conduct, see [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md)*

*For the list of contributors, see [CONTRIBUTORS.md](./CONTRIBUTORS.md)*
