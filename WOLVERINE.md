# Wolverine Migration Notes

This document summarizes the migration from MediatR to **Wolverine** and the Wolverine-specific practices, features, and optimizations applied to the OrderService project.

## Why Wolverine?

Wolverine unifies in-process command/query handling and durable messaging in a single framework. For this project it replaces:

- MediatR command/query dispatch and `IRequestHandler<T>` handlers.
- The custom `OutboxMessage` table, `OutboxDispatcher`, and `IServiceBusPublisher` abstraction.
- The custom `OrderProcessingSaga` orchestrator and activity interfaces.
- MediatR pipeline behaviors for validation and caching.

## Best practices followed

### 1. Vertical Slice Architecture (VSA)

Application code is organized by feature/use case rather than technical layer. Each feature owns its message, handler, validator, and DTOs:

```text
OrderService.Application/Features/Orders/CreateOrder/
  CreateOrderCommand.cs
  CreateOrderHandler.cs
  CreateOrderValidator.cs
```

This keeps related code close together and makes each use case easy to understand and test in isolation.

### 2. Wolverine convention-based handlers

Handlers are plain static methods with a conventional `Handle` signature. No `IRequestHandler<T>` interfaces are needed:

```csharp
public static class CreateOrderHandler
{
    public static async Task<OrderDto> Handle(
        CreateOrderCommand command,
        IOrderRepository repository,
        ...)
    {
        // ...
    }
}
```

### 3. Avoid thin abstractions over Wolverine

Handlers invoke Wolverine `IMessageBus` directly for cross-cutting messaging. There is no custom mediator wrapper; the framework itself is the abstraction.

### 4. Concrete message types

All commands, queries, events, and saga messages are concrete records/classes. Wolverine routes and stores envelopes by concrete type, which is required for durable messaging and saga correlation.

### 5. Wolverine FluentValidation middleware

Validation is applied through Wolverine's built-in FluentValidation middleware (`opts.UseFluentValidation()`). Validators are discovered automatically and run before handlers for both message handlers and HTTP endpoints.

### 6. Custom middleware for audit logging

A custom Wolverine middleware writes to `audit.AuditLog` for every message implementing `IAuditableCommand`:

```csharp
opts.Policies.AddMiddleware(typeof(AuditMiddleware), chain =>
    chain.MessageType.IsAssignableTo(typeof(IAuditableCommand)));
```

The middleware runs after the handler, so it only records commands that passed validation and domain checks.

### 7. Error handling policies with backoff and jitter

Global retry policy uses exponential cooldown and full jitter to avoid thundering-herd retries after shared downstream failures:

```csharp
opts.Policies.OnException<Exception>()
    .RetryWithCooldown(250.Milliseconds(), 500.Milliseconds(), 1.Seconds())
    .WithFullJitter()
    .Then.MoveToErrorQueue();
```

## Wolverine features used

### Transactional outbox and durable inbox

`opts.PersistMessagesWithSqlServer(dbConnection)` configures SQL Server-backed durable messaging. Outgoing messages are persisted in the same EF Core transaction as business data (`UseEntityFrameworkCoreTransactions` + `AutoApplyTransactions`), then delivered asynchronously by Wolverine's durability agents. The durable inbox provides idempotent message handling.

### Sagas

`OrderProcessingSaga : Saga` stores its state in the existing `OrderWorkflowExecutions` table through EF Core saga persistence. The saga is started by `OrderSubmitted`, then cascades `ReserveInventory`, `ProcessPayment`, and `CreateShipment` activity messages. Activity handlers return success/failure messages that drive the saga to completion or terminal failure.

### Azure Service Bus transport

`opts.UseAzureServiceBus(serviceBusConnection)` and `PublishMessage<OrderSubmittedEvent>().ToAzureServiceBusTopic("orders.submitted")` replace the custom Azure SDK publisher. The transport is compatible with the local Azure Service Bus Emulator used by `docker-compose.yml`.

### Wolverine HTTP endpoints

API routes are implemented with `[WolverineGet]`, `[WolverinePost]`, and `[WolverineDelete]`. Each endpoint maps the HTTP request to a Wolverine command/query and dispatches it via `IMessageBus.InvokeAsync`, preserving proper status codes (`201 Created`, `202 Accepted`, `204 NoContent`).

### Local durable queues

`opts.Policies.UseDurableLocalQueues()` enrolls the saga activity messages in the durable inbox/outbox, so saga progress survives process restarts.

## Feature mapping from old to new

| Old component | Wolverine replacement |
|---|---|
| `MediatR` + `IRequestHandler<T>` | Wolverine convention-based handlers |
| `ValidationBehavior<T>` | `UseFluentValidation()` |
| `CachingBehavior<T>` | Cache logic inside query handlers |
| `AuditBehavior<T>` | Custom Wolverine `AuditMiddleware` |
| `OutboxMessage` + `OutboxDispatcher` | Wolverine transactional outbox |
| `IServiceBusPublisher` | Wolverine Azure Service Bus transport |
| `OrderProcessingSaga` + activities | Wolverine `Saga` + activity messages |
| Minimal API route mapping | Wolverine HTTP endpoint classes |

## Testing

- **Unit tests** invoke handler static methods directly with mocks.
- **Saga state-transition tests** exercise the saga logic without Wolverine infrastructure.
- **Custom middleware tests** verify audit logging with a fake `Envelope`.
- **Integration tests** use the real Wolverine runtime via an Alba host:
  - HTTP endpoint tests for `POST /api/orders` and `POST /api/orders/{id}/submit`, including JWT auth and database persistence.
  - Wolverine handler pipeline tests that dispatch commands through `IMessageBus` and assert database side effects.
  - Saga start test that verifies `OrderSubmitted` creates a saga state row.
  - Existing container tests for EF Core, Redis, and audit persistence.

Tests disable the Azure Service Bus transport with the `Wolverine:UseAzureServiceBus=false` configuration switch so they run without external messaging infrastructure.

### Follow-up test opportunities

- A full end-to-end integration test that lets Wolverine's durable local queues process all cascading saga messages and verifies the saga reaches `Completed`.
- Idempotency test that sends the same `OrderSubmitted` message twice and confirms no duplicate saga state.
- Failure-path tests for validation errors and business rule violations through the HTTP endpoint layer.

## References

- [Wolverine for MediatR Users](https://wolverinefx.net/introduction/from-mediatr.html)
- [Wolverine Best Practices](https://wolverinefx.net/introduction/best-practices)
- [Migrating to Wolverine](https://wolverinefx.net/guide/migrating-to-wolverine.html)
- [Idempotency Tutorial](https://wolverinefx.net/tutorials/idempotency.html)
- [Vertical Slice Architecture](https://wolverinefx.net/tutorials/vertical-slice-architecture.html)
- [Error Handling](https://wolverinefx.net/guide/handlers/error-handling.html)
- [Fluent Validation Middleware](https://wolverinefx.net/guide/handlers/fluent-validation.html)
- [HTTP Endpoints](https://wolverinefx.net/guide/http/endpoints.html)
- [EF Core Integration](https://wolverinefx.net/guide/durability/efcore.html)
- [Sagas](https://wolverinefx.net/guide/durability/sagas.html)
