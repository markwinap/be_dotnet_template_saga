---
applyTo: "**/*.cs,**/*.csproj,**/*.slnx"
---

# .NET Backend Instructions

When modifying .NET code in this repository:

- Keep Clean Architecture boundaries (Domain -> Application -> Infrastructure -> API).
- Prefer Minimal APIs and OpenAPI summaries.
- Add tests for all new command/query handlers.
- Keep SAGA and Service Bus handlers in Infrastructure.
- Update Swagger examples and `README.md` when request/response contracts change.
- Keep local Service Bus development aligned with Azure Service Bus Emulator settings.
