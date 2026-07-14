---
applyTo: "**/OrderService.Tests/**/*.cs"
---

# .NET Testing Instructions

When writing or modifying tests in this repository:

- Use xUnit, Moq, and FluentAssertions for unit tests.
- Name test classes `{HandlerName}Tests` and place them in `OrderService.Tests`.
- Name test methods `Should{ExpectedBehavior}_When{Condition}` or follow the existing `Handle_Should{Expected}_When{Condition}` convention.
- Mock domain interfaces in `OrderService.Domain.Interfaces` for unit tests.
- Add integration tests for infrastructure concerns only and reuse `IntegrationTestEnvironment`.
- Ensure integration tests gracefully skip or fail fast when Docker is unavailable.
- Run `dotnet test OrderService.slnx -c Release` before committing.

