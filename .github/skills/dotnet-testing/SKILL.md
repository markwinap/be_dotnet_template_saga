---
name: dotnet-testing
description: Guide for writing and maintaining tests for the .NET 10 OrderService backend template.
---

# .NET Testing Skill

Use this when working on tests for the OrderService .NET backend.

## Conventions

- xUnit, Moq, FluentAssertions for unit tests.
- Test classes named `{HandlerName}Tests` in `OrderService.Tests`.
- Mock domain interfaces in `OrderService.Domain.Interfaces`.
- Integration tests reuse `IntegrationTestEnvironment` and only cover infrastructure concerns.
- Container integration tests must fail fast or skip cleanly when Docker is unavailable.

## Steps

1. Read `.github/agents/net-testing.agent.md` for test-specific behavior.
2. For path-specific guidance, see `.github/instructions/dotnet-testing.instructions.md`.
3. Add unit tests for command/query handlers; add integration tests for infrastructure only.
4. Run `dotnet test OrderService.slnx -c Release` to verify.

