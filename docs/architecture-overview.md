# Architecture Overview

## Solution Structure

The solution contains three projects:

- `zulip-cs-lib` (class library): primary Zulip REST API wrapper.
- `zulip-cs-cli` (console app): command-line interface built on top of the library.
- `zulip-cs-lib.tests` (test project): automated tests for library and client behavior.

All projects multi-target: `net11.0;net10.0;net9.0;net8.0`, with .NET 11 as the primary target and default SDK.

## High-Level Component Diagram

```mermaid
flowchart LR
  CLI[zulip-cs-cli] --> Client[ZulipClient]
  Client --> Resources[Resource Classes\nMessages/Channels/Users/...]
  Resources --> Delegate[Request Delegate]
  Delegate --> Transport1[HttpClient backend]
  Delegate --> Transport2[curl subprocess backend]
  Transport1 --> Zulip[Zulip REST API]
  Transport2 --> Zulip
  Tests[zulip-cs-lib.tests] --> Client
  Tests --> Resources
```

## Core Design Patterns

### 1. Request Delegate Pattern

`ZulipClient` initializes resource classes with a shared delegate:

- Signature: `Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>>`
- Effect: resource classes stay transport-agnostic.
- Benefit: easy testability and dual transport support (`HttpClient` and `curl`).

### 2. Throwing + Try Method Pairing

Most API operations expose two variants:

- Throwing variant (e.g., `SendStream`, `GetAll`, `Create`).
- Non-throwing `Try` variant (e.g., `TrySendStream`, `TryGetAll`, `TryCreate`) returning tuples.

This supports both strict exception flow and explicit caller-managed failure handling.

### 3. Central Response Envelope

`ZulipResponse` acts as a common deserialization target and transport metadata carrier.

- API payload fields map via `JsonPropertyName`.
- Local diagnostic fields (`Success`, `CaughtException`, `HttpResponseCode`, etc.) are ignored from JSON.
- An owned `RawPayload` preserves the complete wire response; extension data and nonfatal unsupported-parameter warnings remain accessible.
- Resource-level validation distinguishes required fields/essential identities from unassigned CLR defaults, including directly injected delegates.
- Presence/registration/batch results retain their request-local response. `ZulipClient.LastResponse` follows completion order and is not a concurrency-safe per-call association.
- Curl uses `ProcessStartInfo.ArgumentList` and concurrent stdout/stderr draining rather than hand-built quoting.

## Authentication and Client Initialization

`ZulipClient` supports:

- Site/email/API key constructor.
- `zuliprc`-based constructor.
- Optional custom `HttpClient`.
- Optional `curl` command path backend.

Authentication uses Basic auth header from `email:api_key` for API calls.

## CLI Integration Boundary

The CLI is intentionally thin:

- Parses args with `System.CommandLine`.
- Resolves config (`--zuliprc` or discovery).
- Invokes `Try*` methods on `zulip-cs-lib` resources.
- Prints plain result messages or JSON payloads.

## Test Strategy

`zulip-cs-lib.tests` validates:

- `ZulipClient` transport and parsing behaviors.
- Resource method request/response semantics.
- INI parsing edge cases.

Tests use `Moq` and mocked `HttpMessageHandler` to avoid real network dependency in unit coverage.
Bounded loopback fixtures additionally verify real curl argument serialization. These require curl on PATH and skip explicitly when unavailable; no live Zulip mutations are used.

---
Updated on: 2026-11-13

Documentation version: 1.0.0
