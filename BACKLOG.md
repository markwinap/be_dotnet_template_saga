# Wolverine / VSA Migration Backlog

Grouped action backlog for the migration from MediatR/Clean Architecture-by-layer to Wolverine with Vertical Slice Architecture.

## Completed in this migration

### Architecture & planning
- [x] Create `feature/wolverine-vsa-migration` branch from `origin/feature/order-rules-workflow-audit`.
- [x] Replace MediatR with Wolverine as the unified mediator and message bus.
- [x] Adopt Vertical Slice Architecture for order use cases.

### Project & packages
- [x] Remove MediatR and related packages.
- [x] Add WolverineFx 5.41.0 packages (core, HTTP, EF Core, FluentValidation, Azure Service Bus, SqlServer).
- [x] Align EF Core package versions with Wolverine dependencies.

### Vertical Slice restructure
- [x] Create `Features/Orders/*` folders in Application and API.
- [x] Move order commands/queries/handlers/validators to VSA folders.
- [x] Delete old flat `Commands`, `Queries`, `Validators`, and `Behaviors` folders.

### Handlers & middleware
- [x] Convert handlers to Wolverine convention-based static `Handle` methods.
- [x] Configure Wolverine FluentValidation middleware.
- [x] Implement Wolverine `AuditMiddleware` for mutating commands.
- [x] Move cache logic into query handlers.
- [x] Apply EF Core transactional middleware and remove manual `SaveChangesAsync` from handlers.

### Outbox & messaging
- [x] Remove custom outbox table/store/dispatcher and Azure SDK publisher.
- [x] Convert `OrderSubmittedEvent` to a Wolverine message.
- [x] Configure Wolverine Azure Service Bus transport for `orders.submitted`.
- [x] Configure durable local queues.

### Saga
- [x] Define `OrderProcessingSaga` state and saga messages.
- [x] Implement Wolverine saga handlers for inventory/payment/shipment.
- [x] Configure retry/backoff/jitter error policies via Wolverine.
- [x] Remove custom saga orchestrator and activities.

### API
- [x] Create Wolverine HTTP endpoint classes in `OrderService.Api/Features/Orders`.
- [x] Replace Minimal API route mapping with `MapWolverineEndpoints()`.
- [x] Preserve JWT auth and rate-limiting metadata.

### Database
- [x] Update `OrderDbContext` mappings (remove custom outbox, map saga state).
- [x] Generate EF Core migration `WolverineVsaMigration`.
- [x] Generate and review idempotent SQL script.

### Tests
- [x] Convert handler unit tests to Wolverine handler signatures.
- [x] Add Wolverine middleware tests for audit logging.
- [x] Update integration tests to remove obsolete outbox/ServiceBus publisher dependencies.

### Documentation
- [x] Update `README.md` (architecture, VSA, endpoints, workflow).
- [x] Update `CHANGELOG.md`.
- [x] Create `WOLVERINE.md` with best practices and feature list.
- [x] Create grouped backlog in `BACKLOG.md`.

## Follow-up / next steps

### Tests
- [ ] Add Wolverine saga state-transition unit tests.
- [ ] Add integration test that publishes/subscribes through Wolverine's durable outbox with containerized SQL Server.
- [ ] Add idempotency test for duplicate `SubmitOrderCommand` messages.
- [ ] Evaluate [Alba](https://jasperfx.github.io/alba) for full Wolverine HTTP endpoint integration tests.

### Operations
- [ ] Verify local end-to-end run with Docker Compose and Azure Service Bus Emulator:
  - create order → 201
  - submit order → 202, saga completes
  - query workflow-state and audit endpoints
- [ ] Decide production deployment recipe for Wolverine message-storage schema (auto-build vs. explicit `resources setup` step).
- [ ] Add Wolverine health-check / dashboard endpoints if needed.

### Architecture hardening
- [ ] Apply customer-scoping to `GET /api/orders/{id}/workflow-state` and `GET /api/orders/{id}/audit` if required by security policy.
- [ ] Review whether `CreateOrderCommand.CustomerId` should be removed from the public request contract since it is overridden from the JWT subject.
- [ ] Consider strong-typed IDs for saga messages if the domain model evolves.

### Documentation
- [ ] Update architecture diagrams in `README.md` to reflect VSA folder layout.
- [ ] Add a local testing guide section specific to Wolverine HTTP endpoints and saga observation.
