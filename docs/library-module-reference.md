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
- Exposes static API key bootstrap helper:
  - `TryFetchApiKey(site, username, password, HttpClient?)`.

## Resource Class Coverage

### Messages

Capabilities:

- Message lifecycle: `Delete`/`TryDelete`, `Edit`/`TryEdit`.
- Reactions: `AddEmoji`/`TryAddEmoji`, `RemoveEmoji`/`TryRemoveEmoji`.
- Sending: private and stream send with overloads for names/IDs.
- Retrieval: `Get`, `GetSingle` (+ `Try*` forms).
- Rendering: `Render`/`TryRender`.
- State and flags: `UpdateFlags`, `MarkAllAsRead`, `MarkStreamAsRead`, `MarkTopicAsRead` (+ `Try*`).
- History and receipts: `GetEditHistory`, `GetReadReceipts` (+ `Try*`).

Exposed enums:

- `EditPropagateMode` (`One`, `Later`, `All`)
- `GetAnchorMode` (`Newest`, `Oldest`, `FirstUnread`, `Id`)
- `FlagOperation` (`Add`, `Remove`)

### Channels

Capabilities:

- Read/list: `GetAll`, `GetById`, `GetIdByName`, `GetSubscriptions`, `GetTopics`, `GetSubscribers`.
- Membership: `Subscribe`, `Unsubscribe`.
- Management: `Update`, `Archive`, `DeleteTopic`.
- Defaults and metadata: `AddDefaultChannel`, `RemoveDefaultChannel`, `GetEmailAddress`, `UpdateTopicMuting`.

### Users

Capabilities:

- Identity: `GetOwnUser`, `GetUser`, `GetUserByEmail`, `GetAll`.
- Presence/status: `GetPresence`, `GetStatus`, `UpdateOwnStatus`, `SetTypingStatus`.
- Admin operations: `Create`, `Update`, `Deactivate`, `Reactivate`.
- Preferences: `MuteUser`, `UnmuteUser`, `GetAlertWords`, `AddAlertWords`.
- Groups: `GetGroups`, `CreateGroup`, `GetGroupMembers`.

### Server

- `GetSettings` / `TryGetSettings` for Zulip server version + feature level discovery.

### Events

- Queue lifecycle: `RegisterQueue`, `GetEvents`, `DeleteQueue` (+ `Try*`).

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

### NavigationViews

- `GetAll`, `Create`, `Update`, `Delete` (+ `Try*`).

### Narrow

Utility type for message query filters.

- `NarrowOperator` enum supports stream/topic/search and many advanced operators.
- Serialization helpers: `ToJson()`, `ToJsonArray(...)`.

## Model Layer Overview (`Models/`)

Main object groups:

- Messaging: `MessageObject`, `MessageHistoryObject`.
- Streams/topics/subscriptions: `StreamObject`, `TopicObject`, `SubscriptionObject`.
- Users/presence/status/groups: `UserObject`, `UserModels`, `UserStatusObject`, `PresenceInfo`, `UserGroupObject`.
- Async/eventing: `EventObject`.
- Scheduled content: `ScheduledMessageObject`, `DraftObject`, `ReminderObject`, `SavedSnippetObject`.
- Org and config: `InviteObject`, `OrganizationObjects`, `MiscObjects`, `NavigationViewObject`.

## Error Handling Contract

For `Try*` methods:

- `success == true`: payload members are valid.
- `success == false`: `details` contains user-readable failure reason.

For throwing variants:

- Throw `Exception` with details derived from `ZulipResponse.GetFailureMessage()` when operation fails.

---
Generated on: 2026-03-05  
Published version: 0.0.1-beta.1
