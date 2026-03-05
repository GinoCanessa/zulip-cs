# zulip-cs Developer Documentation

This folder contains onboarding and technical reference documentation for contributors working on `zulip-cs`.

## Documentation Map

1. [Developer Onboarding](./developer-onboarding.md)
2. [Functionality Summary](./functionality-summary.md)
3. [Architecture Overview](./architecture-overview.md)
4. [Process Flows](./process-flows.md)
5. [Library and Module Reference](./library-module-reference.md)
6. [CLI Command Reference](./cli-command-reference.md)
7. [Dependencies and Build/Test](./dependencies-and-build.md)

## Audience

- New developers onboarding to this repository.
- Maintainers implementing or extending Zulip REST API coverage.
- Contributors updating CLI behavior or tests.

## What This Project Contains

- `zulip-cs-lib`: core C# Zulip API client library.
- `zulip-cs-cli`: command-line utility built on top of the library.
- `zulip-cs-lib.tests`: xUnit + Moq test project for library/client behavior.

## Quick Start

From the solution directory (`src/`):

```bash
dotnet restore
dotnet build
dotnet test
```

---
Generated on: 2026-03-05  
Published version: 0.0.1-beta.1
