# Architecture Decision Records

| ADR | Title | Status | Key Decision | Date |
|-----|-------|--------|--------------|------|
| [ADR-001](./ADR-001.md) | ADR-001: SendAsync Warns on Multiple Handlers | Accepted | Warn but send to first handler registered | 2026.10.01 |
| [ADR-002](./ADR-002.md) | Source Generation for Handler Routing | Accepted | Compile-time handler discovery via `MessageHandlerExtensionsGenerator`; zero runtime reflection | 2026.10.02 |
| [ADR-003](./ADR-003.md) | Notification-Driven Message Delivery Loop | Accepted | No polling; bounded channels; related [ADR-006](./ADR-006.md) | 2026.10.05 |
| [ADR-004](./ADR-004.md) | DLQ with Exponential Backoff | Accepted | Exponential backoff (1s → 60s cap); 1s polling; ~100 max retries before discard | 2026.10.05 |
| [ADR-005](./ADR-005.md) | Async/ValueTask in Hot Paths | Accepted | `PublishAsync`, `SendAsync`, `HandleAsync` return `ValueTask`; background loops return `Task` | 2026.10.05 |
| [ADR-006](./ADR-006.md) | Bounded Channels (100 capacity) | Accepted | Per-type channels; 100-message default; backpressure via `WaitToWriteAsync`; related [ADR-003](./ADR-003.md)  | 2026.10.05 |
| [ADR-007](./ADR-007.md) | Notification-Driven Delivery | Accepted | Channel notifications wake delivery loop; per-type processing tasks; CPU-efficient | 2026.10.05 |
| [ADR-008](./ADR-008.md) | At-Least-Once Delivery | Accepted | Use At-Least-Once delivery vs. Exactly-Once | 2026.10.05 |
| [ADR-009](./ADR-009.md) | Four-Package Project Structure | Accepted | `Abstractions`, `Core`, `InMemoryTransport`, `DemoTransport` | 2026.10.11 |
| [ADR-010](./ADR-010.md) | Source Generation From External Assemblies | Accepted | Publish NanoBus.Core as separate NuGet assembly (`NanoBusCore`) | 2026.10.19 |