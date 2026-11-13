# Functionality Summary

This document provides a high-level summary of what `zulip-cs` currently does.

## Core Library (`zulip-cs-lib`)

Primary functional areas:

- Message operations (send/edit/delete/fetch/render/reactions/flags/read state/history/receipts).
- Channel operations (list/manage/archive/subscriptions/topics/default channels/topic muting).
- User operations (lookup/admin lifecycle/status/presence/typing/muting/alert words/groups).
- Server introspection (Zulip version + feature level).
- Event queue management (detailed initial state/capability registration, typed common/owned unknown events, poll, delete).
- Scheduled messages, drafts, invitations, reminders, saved snippets, navigation views.
- Organization-level metadata (linkifiers, custom emoji, profile fields).
- Modern presence timestamps/server clock, fragment-based navigation/pin state, reminder identity/note/content/failure and current message/channel/group/user/invitation metadata.
- Explicit caller-controlled partial batch outcomes and compatibility diagnostics; required result fields/essential identities are validated without replacing missing data with successful defaults.
- Ordered draft IDs/typed draft inputs, saved-snippet IDs and configurable profile-field creation IDs.
- One-shot narrow flag updates with explicit progress/cursors (FL155), URL-embed rendering (FL513), incremental presence publication (FL263), realm presence, alert-word removal and message-edit typing (FL361).
- Explicit channel creation, modern personal topic policies, subscription status/settings and group identity/governance/membership/subgroup/deactivation workflows.
- Streaming file uploads through both backends, caller-owned streams, bounded cancellation/timeout and attachment list/delete with current message references.

## CLI (`zulip-cs-cli`)

Current CLI command focus is message-centric workflows:

- Send private and stream messages.
- Edit/delete/fetch messages.
- Add/remove emoji reactions.
- Render markdown content.
- Update message flags.
- Read-state commands and read receipts.

## Quality and Testing

- Unit tests cover client transport behavior, resources, response parsing, and INI parsing.
- `Try*` and throwing method patterns are exercised by tests to preserve error-handling contracts.
- Curl argument/form parity is checked through the production argument builder and bounded loopback child-process fixtures. Real-process tests require curl on PATH (and are explicitly skipped when unavailable); missing-executable failure is independently covered.

From `src`, with the SDK selected by `global.json` and dependencies already restored:

```powershell
dotnet build ..\zulip-cs.sln --no-restore
dotnet test ..\zulip-cs.sln --no-restore
```

## Functional Boundaries

- The library is a wrapper around Zulip REST API endpoints and does not implement an offline store.
- CLI output is terminal-oriented text/JSON and is not a full interactive TUI.
- Authentication in normal flows is API key based (Basic auth header).
- Batch continuation, event state management and HTTP timeout/retry policy remain caller-owned. Unknown legacy completion is not proof of completion.
- Navigation numeric/name-only signatures are obsolete no-request failures. Channel sending has one destination; CLI `--streams` is a literal name rather than CSV.
- Optional metadata/options must be gated by actual server capabilities. No development-clone feature level is asserted as deployed, and no new minimum-server policy is imposed.
- The 2026-11-13 common-workflow update targets documented contracts through live FL513; it does not promise all absent endpoints, every send/fetch option, folders, broad settings/authentication, or specialized video/mobile/bouncer capabilities. Live verification is unauthenticated metadata/documentation only.

---
Updated on: 2026-11-13

Published version: 0.0.1-beta.4
