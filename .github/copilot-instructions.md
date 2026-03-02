# Copilot Instructions for zulip-cs

## Build & Test

All commands run from the `src/` directory (where `zulip-cs.sln` lives).

```bash
# Restore, build, and test
dotnet restore
dotnet build
dotnet test

# Run a single test by fully-qualified name
dotnet test --filter "FullyQualifiedName=zulip_set_lib.tests.MessageTests.Message_SendPrivate_EmailSingle"

# Run all tests in a single class
dotnet test --filter "FullyQualifiedName~zulip_set_lib.tests.IniParserTests"
```

## Architecture

The solution has three projects:

- **zulip-cs-lib** — Core library wrapping the Zulip REST API. `ZulipClient` is the entry point; it authenticates via Basic auth (email + API key) and delegates HTTP execution to either `HttpClient` or a `curl` subprocess. API resource classes (e.g., `Messages`) live under `Resources/` and receive a request delegate from `ZulipClient` rather than making HTTP calls directly.
- **zulip-cs-lib.tests** — xUnit tests using Moq to mock `HttpMessageHandler`. Shared test utilities are in `Utils.cs`.
- **zulip-cs-cli** — Console app using `System.CommandLine.DragonFruit` for argument parsing. Reads credentials from a `zuliprc` INI file (searched upward from the executable directory).

## Conventions

- **Target frameworks**: Multi-targets `net8.0`, `net9.0`, and `net10.0` with C# 14 (`LangVersion 14`).
- **Namespaces**: Use underscores matching project names — `zulip_cs_lib`, `zulip_cs_lib.Resources`, `zulip_set_lib.tests` (note: the test namespace uses `zulip_set_lib`, matching the `.csproj` filename typo).
- **Try pattern**: Public API methods come in pairs — a throwing version (e.g., `Messages.Delete()`) and a `Try` variant returning `(bool success, string details)` or `(bool success, string details, ulong messageId)` value tuples.
- **Request delegate pattern**: Resource classes accept a `Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>>` delegate instead of depending on `HttpClient` directly, enabling the `HttpClient`/`curl` dual-backend design.
- **JSON serialization**: Uses `System.Text.Json` throughout (not Newtonsoft). `ZulipResponse` uses `[JsonPropertyName]` attributes for Zulip API field mapping, and `[JsonIgnore]` for client-side metadata properties.
- **XML doc comments**: All public and internal members use `<summary>` and `<param>` XML documentation comments.
- **INI config**: `IniParser` is a custom INI file parser for `zuliprc` files — supports `=` and `:` delimiters and `#`/`;` comment prefixes.
