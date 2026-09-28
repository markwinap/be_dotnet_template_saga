# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-10

### Added

- .NET 10 backend scaffold with Clean Architecture, DDD, and CQRS using MediatR.
- Minimal API endpoints for orders, auth, and health.
- JWT Bearer authentication and `/api/auth/dev-token` for local development.
- EF Core with SQL Server and `EnsureCreatedAsync` startup bootstrap.
- Redis cache-aside behavior via MediatR pipeline.
- Azure Service Bus publisher abstraction and SAGA orchestration skeleton.
- Polly resilience handlers.
- OpenTelemetry metrics and Prometheus `/metrics` endpoint.
- Docker Compose stack with SQL Server, Redis, and Service Bus Emulator.
- Kubernetes manifests in `.k8s/base/`.
- xUnit/Moq/FluentAssertions tests, including container-backed integration tests.
- GitHub Actions CI workflow and Azure Pipelines YAML.

## [Unreleased]

### Migrated

- Replaced MediatR with **Wolverine** as the unified in-process mediator and durable message bus.
- Reorganized the Application and API projects into **Vertical Slice Architecture** feature folders for order use cases.
- Replaced custom `OutboxMessage` table + `OutboxDispatcher` with Wolverine's transactional outbox and durable inbox.
- Replaced custom Azure Service Bus SDK publisher with Wolverine's Azure Service Bus transport.
- Replaced custom `OrderProcessingSaga` orchestrator and activity interfaces with a Wolverine `Saga` and activity messages.
- Replaced MediatR pipeline behaviors with Wolverine FluentValidation middleware and a custom Wolverine `AuditMiddleware`.
- Replaced Minimal API route registration with **Wolverine HTTP endpoints** (`[WolverineGet]`, `[WolverinePost]`, `[WolverineDelete]`).
- Added EF Core migration `WolverineVsaMigration` to drop the custom outbox table and align `OrderWorkflowExecutions` with the saga state document.

### Added

- Wolverine packages: `WolverineFx`, `WolverineFx.Http`, `WolverineFx.EntityFrameworkCore`, `WolverineFx.FluentValidation`, `WolverineFx.AzureServiceBus`, `WolverineFx.SqlServer`.
- Wolverine saga state persistence via EF Core in `OrderWorkflowExecutions`.
- Wolverine error-handling policy with exponential backoff and full jitter, falling back to a dead-letter queue.
- `WOLVERINE.md` documenting Wolverine practices, feature mapping, and references.
- `BACKLOG.md` with completed and follow-up actions grouped by area.
- Alba-based HTTP endpoint integration tests covering JWT auth, create order, and submit order flows.
- Wolverine message-bus integration tests that dispatch commands through the real handler pipeline and verify database side effects.
- Saga state-transition unit tests and a saga-start integration test.

## [1.0.0] - 2026-07-10

### Added

- .NET 10 backend scaffold with Clean Architecture, DDD, and CQRS using MediatR.
- Minimal API endpoints for orders, auth, and health.
- JWT Bearer authentication and `/api/auth/dev-token` for local development.
- EF Core with SQL Server and `EnsureCreatedAsync` startup bootstrap.
- Redis cache-aside behavior via MediatR pipeline.
- Azure Service Bus publisher abstraction and SAGA orchestration skeleton.
- Polly resilience handlers.
- OpenTelemetry metrics and Prometheus `/metrics` endpoint.
- Docker Compose stack with SQL Server, Redis, and Service Bus Emulator.
- Kubernetes manifests in `.k8s/base/`.
- xUnit/Moq/FluentAssertions tests, including container-backed integration tests.
- GitHub Actions CI workflow and Azure Pipelines YAML.

## [1.1.0] - 2026-09-28

### Added

- Six configurable order-domain business rules: submission rate limit, business-hours window, minimum order total, maximum item quantity, daily submission cap, and draft expiration.
- Durable order-submission workflow with transactional outbox, idempotency by message ID, and activity-based SAGA steps (ReserveInventory, ProcessPayment, CreateShipment).
- Separate workflow execution state in `OrderWorkflowExecutions` to keep business and technical concerns orthogonal.
- Immutable audit tables `audit.AuditLog` and `audit.OrderStateTrace` with integrity hash and append-only semantics.
- `AuditBehavior` MediatR pipeline to record every mutating command, plus `HttpAuditContext` for actor/correlation metadata.
- New endpoints: `GET /api/orders/{id}/workflow-state` and `GET /api/orders/{id}/audit`.
- EF Core migration `AddOrderRulesWorkflowAudit` adding outbox, audit, and workflow tables.
- Unit and integration tests covering the new rules, policy, audit behavior, outbox, and audit persistence.

### Planned

- Integration tests for SAGA compensation paths.
- Terraform / Azure provisioning examples.
- OpenTelemetry tracing exporters and example Grafana dashboards.
