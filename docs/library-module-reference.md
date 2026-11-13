# Library and Module Reference

## Project Modules

### `zulip-cs-lib`

Primary reusable API client library.

Key files:

- `ZulipClient.cs`: client initialization, auth, transport, resource wiring.
- `ZulipResponse.cs`: shared response envelope for deserialization and transport metadata.
- `IniParser.cs`: parser for Zulip-style INI config files.
- `Resources/*.cs`: API capability partitions.
- `Models/*.cs`: DTO/model types used by resources and responses.

### `zulip-cs-cli`

Console application exposing selected `Messages` operations for day-to-day usage and automation.

### `zulip-cs-lib.tests`

Unit tests for client/resource functionality and parser behavior.

## `ZulipClient` Responsibilities

- Validates connection/auth parameters (`site`, `email`, `apiKey`).
- Builds Basic auth header from `email:apiKey`.
- Chooses request transport:
  - `HttpClient`
  - `curl` subprocess
- Initializes resource instances with a shared request delegate.
- Retains `LastResponse`, the most recently completed response, including warnings/owned raw JSON. Use detailed results for per-request association under concurrency.
- Exposes static API key bootstrap helper:
  - `TryFetchApiKey(site, username, password, HttpClient?)`.

## Resource Class Coverage

### Messages

Capabilities:

- Message lifecycle: `Delete`/`TryDelete`, `Edit`/`TryEdit`.
- Reactions: `AddEmoji`/`TryAddEmoji`, `RemoveEmoji`/`TryRemoveEmoji`.
- Sending: private recipients retain existing email/ID forms; channel sends require exactly one JSON string name or scalar ID.
- Retrieval: `Get`, `GetSingle` (+ `Try*` forms).
- Rendering: `Render`/`TryRender`.
- State and flags: `UpdateFlags`, `MarkAllAsRead`, `MarkStreamAsRead`, `MarkTopicAsRead` (+ `Try*`).
- History and receipts: `GetEditHistory`, `GetReadReceipts` (+ `Try*`).
- `MarkAllAsReadBatch` / `TryMarkAllAsReadBatch`: one caller-controlled batch with complete/partial/unknown state and its response. Convenience `MarkAllAsRead` rejects known partial completion.

Exposed enums:

- `EditPropagateMode` (`One`, `Later`, `All`)
- `GetAnchorMode` (`Newest`, `Oldest`, `FirstUnread`, `Id`)
- `FlagOperation` (`Add`, `Remove`)

### Channels

Capabilities:

- Read/list: `GetAll`, `GetById`, `GetIdByName`, `GetSubscriptions`, `GetTopics`, `GetSubscribers`.
- Membership: `Subscribe`, `Unsubscribe`.
- Management: `Update`, `Archive`, `DeleteTopic`.
- `DeleteTopicBatch` / `TryDeleteTopicBatch`: one caller-controlled topic deletion batch. `Complete == null` means unspecified legacy completion, not proof that all work finished.
- Defaults and metadata: `AddDefaultChannel`, `RemoveDefaultChannel`, `GetEmailAddress`, `UpdateTopicMuting`.

### Users

Capabilities:

- Identity: `GetOwnUser`, `GetUser`, `GetUserByEmail`, `GetAll`.
- Presence/status: `GetPresenceDetails` (`PresenceResult` with modern timestamps/server clock/response), legacy `GetPresence`, `GetStatus`, `UpdateOwnStatus`, `SetTypingStatus`.
- Admin operations: `Create`, `Update`, `Deactivate`, `Reactivate`.
- Preferences: `MuteUser`, `UnmuteUser`, `GetAlertWords`, `AddAlertWords`.
- Groups: `GetGroups`, `CreateGroup`, `GetGroupMembers`.
- `GetOwnUser` reads the flat profile; group membership reads integer `members`, with an optional `directMemberOnly` overload. The legacy presence adapter fails explicitly on modern-only data rather than synthesizing status.

### Server

- `GetSettings` / `TryGetSettings` for Zulip server version + feature level discovery.

### Events

- Queue lifecycle: `RegisterQueue`, `GetEvents`, `DeleteQueue` (+ `Try*`).
- Detailed `Register(RegistrationOptions?)` / `TryRegister` retain queue/cursor, typed initial users/channels/presences, feature/timeout/capability metadata and complete owned `RawInitialState`.
- Options include independent event/fetch filters, capabilities, subscriber inclusion, slim presence and queue inactivity timeout. Initial fields depend on fetch filters; message bodies are fetched separately. HTTP timeout/state-store/retry policy remains caller-owned.
- Common event payloads are typed by kind; other fields, including reused names on unknown future kinds, survive in owned extension data.

### ScheduledMessages

- `GetAll`, `Create`, `Edit`, `Delete` (+ `Try*`).

### Drafts

- `GetAll`, `Create`, `Edit`, `Delete` (+ `Try*`).

### Invitations

- `GetAll`, `Send`, `CreateLink`, `Resend`, `Revoke`, `RevokeLink` (+ `Try*`).

### Organization

- Linkifiers: `GetLinkifiers`, `AddLinkifier`, `UpdateLinkifier`, `RemoveLinkifier`.
- Emoji: `GetCustomEmoji`, `DeactivateCustomEmoji`.
- Profile fields: `GetProfileFields`, `CreateProfileField`.

### SavedSnippets

- `GetAll`, `Create`, `Edit`, `Delete` (+ `Try*`).

### Reminders

- `GetAll`, `Create`, `Delete` (+ `Try*`).
- `CreateWithId(messageId, scheduledDeliveryTimestamp, note?)` / `TryCreateWithId` return the actual `reminder_id`. Existing creation calls are projections. Reads preserve target, recipients/content and failure state.

### NavigationViews

- `GetAll`, `Create`, `Update`, `Delete` (+ `Try*`).
- `Create(fragment, isPinned, name?)`, `Update(fragment, name?, isPinned?)`, `Delete(fragment)` use fragment identity. Update requires at least one changed field.
- Name-only creation/numeric update/delete are obsolete and fail without HTTP. Read `Fragment`/`IsPinned`, not obsolete `Id`. Slash-delimited fragments encode reserved characters and reject empty/dot traversal segments.

### Narrow

Utility type for message query filters.

- `NarrowOperator` enum supports stream/topic/search and many advanced operators.
- Serialization helpers: `ToJson()`, `ToJsonArray(...)`.
- `Channels` accepts `public` or `archived`; its omitted operand defaults to `public`. Invalid operands throw `ArgumentException`.

## Model Layer Overview (`Models/`)

Main object groups:

- Messaging: `MessageObject`, `MessageHistoryObject`, `MessageReactionObject`, with reactions and edit/move metadata.
- Streams/topics/subscriptions: `StreamObject`, `TopicObject`, `SubscriptionObject`.
- Users/presence/status/groups: `UserObject`, `UserStatusObject`, `PresenceInfo`, `PresenceData`, `UserGroupObject`, `GroupSettingValue`.
- Async/eventing: `EventObject`.
- Scheduled content: `ScheduledMessageObject`, `DraftObject`.
- Misc resources: `SavedSnippetObject`, `ReminderObject`, `NavigationViewObject` (defined in `MiscObjects.cs`).
- Org and config: `InviteObject`, `LinkifierObject`, `EmojiObject`, `ProfileFieldObject` (defined in `OrganizationObjects.cs`).

Channel/subscription models share archive/folder/topic/default-push/governance metadata. User and invitation models retain import/deletion and lifecycle fields. Named/anonymous permission groups round-trip unknown object fields and reject invalid variants. `StreamPostPolicy` is deprecated/inexact; authoritative group settings are not a substitute for evaluating realm permissions and channel access.

## Error Handling Contract

For `Try*` methods:

- `success == true`: required result fields and essential identities are present and correctly shaped. Real empty collections are valid; this is not exhaustive OpenAPI schema validation.
- `success == false`: `details` contains user-readable failure reason.
- `ignored_parameters_unsupported` is a nonfatal warning retained on responses/detailed results. `RawPayload`/`ExtensionData` preserve unknown response data.
- Batch Try success means the one request succeeded, not necessarily that all work completed. Inspect `Complete`; convenience methods reject known partial work and report unspecified legacy completion in Try details.

For throwing variants:

- Throw `Exception` with details derived from `ZulipResponse.GetFailureMessage()` when operation fails.

---
Updated on: 2026-11-13

Published version: 0.0.1-beta.4
