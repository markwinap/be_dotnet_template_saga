---
name: add-tests
description: Add unit and integration tests for a .NET OrderService handler or feature.
---

Add tests for the specified .NET OrderService handler or feature.

1. Create a unit test class in `OrderService.Tests` named `{HandlerName}Tests`.
2. Use xUnit, Moq, and FluentAssertions.
3. Mock the appropriate `IOrderRepository` or other domain interfaces.
4. Cover the happy path and at least one failure/edge case.
5. If infrastructure is being tested, add an integration test in `OrderService.Tests/Integration` reusing `IntegrationTestEnvironment`.
6. Run `dotnet test OrderService.slnx -c Release` to verify.

