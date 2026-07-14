# AGENTS.md

This repository contains a production-ready .NET 10 backend scaffold.

Agent conventions:
- Keep architecture boundaries strict (Domain -> Application -> Infrastructure -> API).
- Prefer Minimal APIs for new endpoints.
- Keep JWT validation enabled by default.
- Treat SAGA and Service Bus handlers as infrastructure concerns.
- Add tests for all command/query handlers.
- Keep Swagger/OpenAPI examples up to date when request/response contracts change.
- Keep local Service Bus development aligned to Azure Service Bus Emulator settings in `docker-compose.yml` and `docker/servicebus/config.json`.
- When adding endpoints, update `README.md` endpoint list and local testing guidance.
