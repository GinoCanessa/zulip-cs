# Changelog

Notable changes to `zulip-cs-lib` and `zulip-cs-cli` are recorded here.
Versions follow [Semantic Versioning](https://semver.org/).

## 1.0.0

First stable release, following the `0.0.1-beta.5` prerelease line.

### Added

- Streaming file, avatar, and emoji uploads through HttpClient and curl, with
  caller-owned streams, cancellation, bounded upload timeouts, and attachment
  management. Files also support temporary URLs and one-shot thumbnail checks.
- Detailed event registration with initial state, fetch filters, capabilities,
  typed common events, preserved unknown payloads, and lossless 64-bit cursors.
- Explicit channel creation, personal topic visibility, subscription settings
  and status, user-channel lookup, and channel-folder lifecycle and ordering.
- Group creation identities, governance settings, membership and subgroup
  operations, and deactivation.
- Typed draft inputs and ordered creation IDs; saved-snippet and profile-field
  creation identities and profile-field configuration.
- Narrow-based message flag updates with explicit range progress, URL-embed
  rendering, and moderation reports.
- Incremental presence publication, detailed modern presence results, realm
  presence, alert-word removal, and message-edit typing.
- Selected personal settings, profile data, organization defaults, profile-field
  lifecycle, linkifier ordering, allowed domains, exports and consent metadata,
  and documented bot-account string storage.
- JWT API-key bootstrap and explicit own/bot API-key retrieval and regeneration.
- Request-local diagnostics on detailed results, raw and unknown response data,
  unsupported-parameter warnings, and explicit partial batch outcomes.

### Changed

- Added `net11.0` as the primary target while retaining `net10.0`, `net9.0`, and
  `net8.0`. Development and CI select the .NET 11 SDK; the library still has no
  external NuGet dependencies.
- Required response fields and essential identities now fail explicitly when
  missing, null, or malformed instead of becoming success-shaped defaults.
- `ZulipClient` implements `IDisposable` and disposes only internally owned
  HttpClient instances. Injected clients and upload streams remain caller-owned.
- Message and event processing avoid redundant materialization while keeping
  returned mutable models independent of typed response diagnostics.
- GitHub Release publication now validates the exact tagged source across Linux,
  Windows, and macOS, packs one immutable candidate, and uses NuGet trusted
  publishing. Tag pushes and manual validation runs do not publish.

### Fixed

- Corrected user, creation, invitation, reminder, navigation, subscription,
  presence, and mutation request/response contracts.
- Both transports retain actual HTTP status and response bodies; failed HTTP
  statuses take precedence over success-shaped JSON. Ordinary HTTP timeouts and
  cancellation return explicit Try failure details and refresh `LastResponse`.
- Request-local HTTP objects are disposed, and network failures no longer write
  unsolicited console diagnostics. Curl uploads preserve early HTTP errors.
- Email identities and logical path segments are safely encoded; event cursors
  no longer lose values outside the 32-bit range.
- JSON converters preserve caller serialization policies and reference context.
- CLI enum/filter options reject invalid values before loading credentials;
  omitted propagation mode defaults to `one`. Configuration discovery includes
  the filesystem root, and INI values retain later delimiter characters.

### Migration and compatibility

- Navigation views use fragment identity. Legacy numeric update/delete and
  name-only creation overloads are obsolete and fail without sending a request.
- Use `GetPresenceDetails` for modern presence payloads. The legacy dictionary
  adapter fails explicitly when server-provided client entries are unavailable.
- Use detailed `Events.Register`, `LastEventId64`, and long polling overloads for
  lossless cursors. Legacy int projections reject out-of-range values.
- Inspect batch completion and narrow-update boundaries before continuing.
  Convenience batch methods reject known partial work; mutation failures do not
  imply rollback and must not be blindly retried.
- Channel sending has exactly one destination. CLI `--streams` is a literal
  channel name, not a comma-separated destination list.
- Calls after client disposal throw `ObjectDisposedException`, including Try
  methods. API-key regeneration returns new credentials but does not update an
  existing client.
- Deprecated `StreamPostPolicy` is coarse metadata, not authorization. Use
  authoritative permission-group settings together with realm and channel access.
- API coverage follows documented contracts through feature level 513, verified
  on 2026-11-13. This is neither a global minimum-server guarantee nor
  authenticated production certification. Gate newer options by server feature
  level and permissions; retries, batch continuation, and event state remain
  caller-owned. Ordinary curl requests have no library-configured deadline.

See the [README](README.md#compatibility-scope) for feature requirements and
examples, and the [release process](docs/releasing.md) for publishing prerequisites.
