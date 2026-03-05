# Developer Onboarding

This guide is intended for developers joining `zulip-cs` and needing to become productive quickly.

## 1. Prerequisites

- .NET SDK capable of building `net8.0`, `net9.0`, and `net10.0` targets.
- Access to a Zulip server and API credentials (for manual integration checks).
- Git and a shell environment (PowerShell/bash).

## 2. Repository Orientation

Top-level:

- `zulip-cs.sln`: solution containing all projects.
- `src/zulip-cs-lib`: library with API resource wrappers.
- `src/zulip-cs-cli`: command-line interface using the library.
- `src/zulip-cs-lib.tests`: test suite.
- `secrets/zuliprc`: local credential file example location.

## 3. Build and Test Cycle

Run all operations from `src/`:

```bash
dotnet restore
dotnet build
dotnet test
```

Run one test class:

```bash
dotnet test --filter "FullyQualifiedName~zulip_set_lib.tests.MessageTests"
```

Run one test by full name:

```bash
dotnet test --filter "FullyQualifiedName=zulip_set_lib.tests.MessageTests.Message_SendPrivate_EmailSingle"
```

## 4. Configuration Model (`zuliprc`)

`zulip-cs-lib` and `zulip-cs-cli` can load credentials from a Zulip INI file.

Example:

```ini
[api]
email=user@example.com
key=your_api_key_here
site=https://zulip.example.com
```

CLI lookup behavior:

- Uses `--zuliprc` if provided.
- Otherwise searches upward from the executable directory for `zuliprc`.
- At each parent level also checks `secrets/zuliprc`.

## 5. Development Workflow

1. Identify the resource area (e.g., `Messages`, `Channels`, `Users`).
2. Add or update methods in the corresponding `Resources/*.cs` file.
3. Follow the existing method pairing pattern:
   - Throwing method (`GetX`, `CreateX`, etc.).
   - `TryX` counterpart returning `(bool success, string details, ...)`.
4. Add/adjust model properties under `Models/` when API shape changes.
5. Extend tests in `zulip-cs-lib.tests` for both success and failure paths.
6. Validate with build + tests.

## 6. Coding Conventions in This Repository

- `System.Text.Json` is the JSON serializer in use.
- Public APIs are async and return `Task`-based results.
- Resource classes do not own HTTP clients directly; they receive a request delegate from `ZulipClient`.
- API/transport failures in `Try*` methods should surface as tuple failure details, not exceptions.

## 7. Suggested First Tasks for New Contributors

- Add one small endpoint to an existing resource class.
- Add tests for both success and error handling.
- Add/update corresponding documentation in this `docs/` folder.

---
Generated on: 2026-03-05  
Published version: 0.0.1-beta.1
