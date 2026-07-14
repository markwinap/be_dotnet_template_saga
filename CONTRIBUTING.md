# Contributing to Order Service Template

Thanks for taking the time to contribute.

## Getting started

- Read the [README](README.md) and [AGENTS.md](AGENTS.md) before making changes.
- Open a GitHub issue for large features or design changes before writing code.
- Fork the repository and create a feature branch from `main`.

## Pull request process

1. Branch naming: `feature/<short-name>`, `bugfix/<short-name>`, or `docs/<short-name>`.
2. Keep the Clean Architecture boundaries strict:
   - `Domain` has no external dependencies.
   - `Application` references `Domain` only.
   - `Infrastructure` implements interfaces defined in inner layers.
   - `Api` is the composition root and entry point.
3. Prefer Minimal APIs for new endpoints.
4. Add or update tests for all new command/query handlers.
5. Keep JWT validation enabled by default.
6. Update `README.md`, OpenAPI examples, and `CHANGELOG.md` if request/response contracts change.
7. Ensure `dotnet build` and `dotnet test` pass locally.
8. Open a pull request with a clear description and link to the related issue.

## Code style

- Follow the existing .editorconfig and C# conventions.
- Keep methods small and focused.
- Add XML documentation on public APIs when `GenerateDocumentationFile` is enabled.
- Use `sealed` records for commands, queries, and DTOs.

## Running the project locally

```bash
docker-compose up -d
dotnet build OrderService.slnx
dotnet test OrderService.slnx
```

## Questions

Open a [GitHub Discussion](https://github.com/markwinap/be_dotnet_template_saga/discussions) or an issue for help.
