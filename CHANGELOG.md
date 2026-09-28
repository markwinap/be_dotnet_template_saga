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
