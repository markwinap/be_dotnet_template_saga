---
name: add-cqrs-handler
description: Add a new CQRS command or query with a Minimal API endpoint and tests.
---

Create a new CQRS command or query for the OrderService .NET backend.

1. Add the request DTO in `OrderService.Application/Commands/{Name}` or `OrderService.Application/Queries/{Name}`.
2. Implement a handler with `IRequestHandler<TRequest, TResponse>`.
3. Add a FluentValidation validator if applicable.
4. Add unit tests in `OrderService.Tests`.
5. Wire the endpoint in `OrderService.Api/Program.cs` with OpenAPI summaries and examples.
6. Update `README.md` endpoint list.
7. Run `dotnet build OrderService.slnx -c Release` and `dotnet test OrderService.slnx -c Release`.
