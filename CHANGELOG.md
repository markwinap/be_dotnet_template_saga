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

### Planned

- Integration tests for SAGA compensation paths.
- Terraform / Azure provisioning examples.
- OpenTelemetry tracing exporters and example Grafana dashboards.
