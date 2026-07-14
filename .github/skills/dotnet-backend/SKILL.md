---
name: dotnet-backend
description: Guide for making changes to the .NET 10 OrderService backend template.
---

# .NET Backend Skill

Use this when working on the OrderService .NET backend.

## Conventions

- Clean Architecture (Domain -> Application -> Infrastructure -> API).
- Minimal APIs, MediatR CQRS, FluentValidation.
- EF Core, Redis cache, Azure Service Bus, SAGA orchestration.
- JWT validation enabled by default.

## Steps

1. Read `AGENTS.md` for cross-tool conventions.
2. For path-specific guidance, see `.github/instructions/dotnet.instructions.md`.
3. Make changes, add tests, update Swagger examples.
4. Run `dotnet build OrderService.slnx -c Release` and `dotnet test OrderService.slnx -c Release`.
