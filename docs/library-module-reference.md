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
- Retains `LastResponse`, the most recently completed response, including warnings/owned raw JSON. Use detailed results for per-request association under concurrency. Message/event resource lists can reuse an unobserved default-options wire list; diagnostics rebuild independently from owned JSON. Returned message events retain their validated typed message alongside the original raw snapshot.
- Implements `IDisposable`, releasing only internally owned HTTP clients. Reuse the wrapper; injected clients and upload streams remain caller-owned. Transport operations after disposal throw `ObjectDisposedException`, including Try methods.
- Ordinary HTTP timeout/cancellation failures return explicit Try diagnostics without unsolicited console output. HTTP request/response content, including password bootstrap, is disposed after materialization without disposing the reusable/injected client.
- Both production backends retain actual HTTP status/body; HTTP failure overrides success-shaped JSON. Status zero remains accepted for caller-created request delegates, not as a successful production curl result.
- Exposes static API key bootstrap helper:
  - `TryFetchApiKey(site, username, password, HttpClient?)`.
  - `FetchApiKeyJwt` / `TryFetchApiKeyJwt` (FL160): form-body JWT, optional profile and cancellation; requires JWT configured and no default Authorization on an injected client. `ApiKeyResult` retains explicit credentials/profile and sensitive request-local response.

## Resource Class Coverage

### Messages

Capabilities:

- Message lifecycle: `Delete`/`TryDelete`, `Edit`/`TryEdit`.
- Reactions: `AddEmoji`/`TryAddEmoji`, `RemoveEmoji`/`TryRemoveEmoji`.
- Sending: private recipients retain existing email/ID forms; channel sends require exactly one JSON string name or scalar ID.
- Retrieval: `Get`, `GetSingle` (+ `Try*` forms).
- `Get` can reuse an unobserved default-options wire message list after raw validation. Returned mutable messages remain independent of typed diagnostics; observed/custom-options envelopes retain authoritative raw deserialization.
- Rendering: `Render`/`TryRender`; separate `(content, populateUrlEmbedData)` overloads request asynchronous URL embeds at FL513 without changing the old wire request.
- State and flags: `UpdateFlags`, `MarkAllAsRead`, `MarkStreamAsRead`, `MarkTopicAsRead` (+ `Try*`).
- History and receipts: `GetEditHistory`, `GetReadReceipts` (+ `Try*`).
- `MarkAllAsReadBatch` / `TryMarkAllAsReadBatch`: one caller-controlled batch with complete/partial/unknown state and its response. Convenience `MarkAllAsRead` rejects known partial completion.
- `UpdateFlagsForNarrow` / `TryUpdateFlagsForNarrow` (FL155): one range with counts, nullable cursors, boundary booleans, ignored-channel diagnostics and its own response. Caller owns continuation; this replaces legacy mark-all/channel/topic-read APIs without removing them.
- `Report` / `TryReport` (FL382): server-advertised type (FL435), optional context up to 1000 Unicode code points; `"other"` requires nonempty context. Moderation channel must be configured; FL511 delivery errors remain failures.

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
- Explicit channel creation, subscription status and typed bulk/single subscription-setting updates now cover common administration without subscription-creation side effects. Creation reads actual response `id`; channel email reads `email`.
- `CreateWithId(name, subscribers, ChannelCreationOptions?)` / Try requires FL417; options include topic/retention/privacy/default settings and permission groups, with default push notifications at FL507.
- `GetSubscriptionStatus(userId, streamId)` / Try (FL12; bot targets at FL458), `UpdateSubscriptionSettings(changes)` / Try, `UpdateSubscriptionProperty(change)` / Try.
- Personal topic policies support None/Muted/Unmuted/Followed. Prefer the `/user_topics` workflow (FL170, Followed at FL219) over retained legacy topic muting; empty topics require FL334.
- `UpdateTopicVisibility(streamId, topic, TopicVisibilityPolicy)` / Try is the typed modern policy API.
- `GetUserChannels(userId)` / Try (FL440; bot targets FL458) returns actual integer `subscribed_channel_ids`, limited by metadata permissions.

### ChannelFolders

- `Create(name, description)` / Try (FL389) returns `channel_folder_id`. Description is required, though empty is valid.
- `GetAll(includeArchived?)` / Try returns `channel_folders`; list objects use `id`, not the creation-result key. Creator/time are nullable, and ordering metadata is optional for older servers.
- `Update(channelFolderId, name?, description?, isArchived?)` / Try edits or archives/unarchives. Null values leave settings unchanged.
- `Reorder(order)` / Try (FL414) sends the complete order, including archived folder IDs. Folder mutations require administrators; no delete endpoint is invented.

### Users

Capabilities:

- Identity: `GetOwnUser`, `GetUser`, `GetUserByEmail`, `GetAll`.
- Email lookup and presence identities are encoded as logical path segments, including reserved characters and Unicode.
- Presence/status: `GetPresenceDetails` (`PresenceResult` with modern timestamps/server clock/response), legacy `GetPresence`, `GetStatus`, `UpdateOwnStatus`, `SetTypingStatus`.
- Admin operations: `Create`, `Update`, `Deactivate`, `Reactivate`.
- Preferences: `MuteUser`, `UnmuteUser`, `GetAlertWords`, `AddAlertWords`, `RemoveAlertWords` (returns remaining words).
- Groups: `GetGroups`, `CreateGroup`, `GetGroupMembers`.
- `GetOwnUser` reads the flat profile; group membership reads integer `members`, with an optional `directMemberOnly` overload. The legacy presence adapter fails explicitly on modern-only data rather than synthesizing status.
- `UpdateOwnPresence` / `TryUpdateOwnPresence` (FL263): active/idle publication with modern incremental revision; optional history window requires FL288. Ping-only omits fetched data/clock. `GetRealmPresence` / Try retains the read endpoint's legacy email-keyed format.
- `SetMessageEditTyping(messageId, isTyping)` / Try publishes start/stop using `/messages/{message_id}/typing` (FL361).
- Group administration includes identity-returning creation, update, membership update/status, subgroup listing/update and deactivation. Creation uses `group_id`; governance options reuse `GroupSettingValue` and the documented replacement `new`/optional `old` shape.
- Group pairs: `CreateGroupWithId`, `UpdateGroup`, `UpdateGroupMembers`, `GetGroupMembershipStatus`, `GetGroupSubgroups`, `UpdateGroupSubgroups`, `DeactivateGroup`. Original group creation signatures project validated identities.
- Credential pairs: `RegenerateOwnApiKey`, `GetBotApiKey` (FL463), `RegenerateBotApiKey`. Own regeneration returns a new key but leaves this client's authentication unchanged; construct a new client. Bot calls require owner/admin permission. Malformed results do not undo rotation. Do not log credential responses or LastResponse.
- `GroupOptions` supports initial subgroups (FL311) and explicit governance. `GroupUpdateOptions` supports replacement `GroupSettingUpdate` values and reactivation (FL386). Combined subgroup/member updates require FL311; subgroup/member-status endpoints FL127; deactivation FL290. Bot callers require FL496 on the documented group endpoints that permit them.
- `UpdateSettings(UserSettingsOptions)` / Try (FL80; EnterSends FL81) supports timezone/language, 24-hour time, enter-to-send and selected desktop/email/push/digest preferences. Nullable options distinguish omission from false/empty; password/identity changes are excluded.
- `UpdateProfileData(ProfileDataInput[])` / Try sends JSON object-array `data`; values are strings, null or user-ID integer arrays. `RemoveProfileData(fieldIds)` / Try sends an integer-array `data` on DELETE. Empty arrays are valid; null values remove a field.
- `UploadAvatar(stream, filename, contentType?, cancellationToken)` / Try posts multipart field `file`, returning required `avatar_url`. `DeleteAvatar` / Try returns the restored `avatar_url`; deletion is idempotent from FL443. Shared UploadTimeout/stream ownership applies, and restrictions are server-enforced.

### Server

- `GetSettings` / `TryGetSettings` for Zulip server version + feature level discovery.

### Events

- Queue lifecycle: `RegisterQueue`, `GetEvents`, `DeleteQueue` (+ `Try*`).
- Detailed `Register(RegistrationOptions?)` / `TryRegister` retain queue/cursor, typed initial users/channels/presences, feature/timeout/capability metadata and complete owned `RawInitialState`.
- `RegistrationResult.LastEventId64` and `ZulipResponse.LastEventId64` retain Int64 cursors; `GetEvents`/`TryGetEvents` accept them directly through additive long overloads. Existing int overloads/properties remain available; legacy int getters throw `OverflowException` outside their range.
- `TryRegisterQueue` retains its int tuple and returns explicit overflow failure with the allocated queue ID and cleanup guidance; detailed `Register` is the lossless alternative. Delete the returned unused queue after such a legacy failure. Registration JSON preserves the logical cursor name `LastEventId` and honors caller naming policies (for example, Web defaults use `lastEventId`); response wire JSON remains `last_event_id`.
- Options include independent event/fetch filters, capabilities, subscriber inclusion, slim presence and queue inactivity timeout. Initial fields depend on fetch filters; message bodies are fetched separately. HTTP timeout/state-store/retry policy remains caller-owned.
- Common event payloads are typed by kind; other fields, including reused names on unknown future kinds, survive in owned extension data.

### ScheduledMessages

- `GetAll`, `Create`, `Edit`, `Delete` (+ `Try*`).

### Drafts

- `GetAll`, `Create`, `Edit`, `Delete` (+ `Try*`).
- `CreateWithIds` / `TryCreateWithIds` accept raw JSON or `IReadOnlyList<DraftInput>` and return ordered draft IDs (FL87). Typed drafts use stream/private/empty wire types and omit IDs/unspecified timestamps. Existing create methods project validated identity results. Published draft web UI support is pending.

### Invitations

- `GetAll`, `Send`, `CreateLink`, `Resend`, `Revoke`, `RevokeLink` (+ `Try*`).

### Organization

- Linkifiers: `GetLinkifiers`, `AddLinkifier`, `UpdateLinkifier`, `RemoveLinkifier`.
- Emoji: `GetCustomEmoji`, `DeactivateCustomEmoji`.
- Profile fields: `GetProfileFields`, `CreateProfileField`.
- `CreateProfileFieldWithId` / Try returns actual `id` and accepts `ProfileFieldOptions`: structured `FieldData`, summary display (FL146), required (FL244), user-editable (FL296), matching (FL455). Requires an organization administrator.
- `UpdateProfileField(fieldId, name?, hint?, ProfileFieldOptions?)` / Try supports partial updates (FL252); `DeleteProfileField` and `ReorderProfileFields(order)` / Try complete lifecycle/order. Include all fields in reorder; paragraph-summary support requires FL476.
- `ReorderLinkifiers(orderedLinkifierIds)` / Try (FL202) sends the full JSON ID order.
- `UploadCustomEmoji(emojiName, stream, filename, contentType?, cancellationToken)` / Try posts multipart field `filename` at an encoded logical-name path. Success requires only the envelope, not an invented uploaded-file URL. Permissions/size are server-configured; shared bounded upload/ownership applies.
- `GetAllowedDomains`, `AddAllowedDomain(domain, allowSubdomains)`, `UpdateAllowedDomain`, `DeleteAllowedDomain` / Try use current domain strings (creation shape FL63; owner-only mutations FL143). Creation returns the real `new_domain: [id, domain]` via `OrganizationAllowedDomainCreationResult`.
- `GetExports`, `CreateExport(OrganizationExportType?)`, `DeleteExport`, `GetExportConsents` / Try expose queued IDs (FL182), current pending/failed/deleted export metadata and consent state (FL295; visibility metadata FL430). Explicit Public/FullWithConsent/FullWithoutConsent types and current string metadata require FL449. FullWithoutConsent also requires server-enabled owner_full_content_access. Prior-server metadata is optional; no download/poll/retry is automatic.
- `UpdateUserSettingsDefaults(RealmUserSettingsDefaultsOptions)` / Try (FL96; 24-hour-time FL99) changes selected notification/display defaults for future new members only. Its dedicated nullable options exclude timezone/language/profile/identity.

### BotStorage

- `Get(keys?)` / Try uses an optional JSON `keys` query and requires a string-valued `storage` object. Null retrieves all storage, and actual empty objects are valid.
- `Update(IReadOnlyDictionary<string,string>)` / Try sends JSON object-form `storage`; values must be strings, not arbitrary JSON or null.
- `Delete(keys)` / Try always sends an explicit JSON key array. Null fails; an empty list is a valid explicit no-op, never an implicit delete-all.
- Bot-account-only from FL494; account restrictions are server-enforced. No undocumented bot CRUD or implicit storage migration is introduced.

### SavedSnippets

- `GetAll`, `Create`, `Edit`, `Delete` (+ `Try*`).
- `CreateWithId` / `TryCreateWithId` returns `saved_snippet_id` (FL297); existing create signatures remain validated projections.

### Reminders

- `GetAll`, `Create`, `Delete` (+ `Try*`).
- `CreateWithId(messageId, scheduledDeliveryTimestamp, note?)` / `TryCreateWithId` return the actual `reminder_id`. Existing creation calls are projections. Reads preserve target, recipients/content and failure state.

### NavigationViews

- `GetAll`, `Create`, `Update`, `Delete` (+ `Try*`).
- `Create(fragment, isPinned, name?)`, `Update(fragment, name?, isPinned?)`, `Delete(fragment)` use fragment identity. Update requires at least one changed field.
- Name-only creation/numeric update/delete are obsolete and fail without HTTP. Read `Fragment`/`IsPinned`, not obsolete `Id`. Slash-delimited fragments encode reserved characters and reject empty/dot traversal segments.

### Files

- `Upload` / `TryUpload` accepts a readable caller-owned stream, filename, optional content type and cancellation token; returns `FileUploadResult` with required server-relative URL (FL272), optional filename (FL285) and request-local diagnostics.
- `GetAll` / `TryGetAll` returns `AttachmentListResult` with current attachment metadata, space usage and diagnostics. Current `message_ids` shape requires FL472; timestamps are in seconds (FL443).
- `Delete` / `TryDelete` removes an attachment by ID.
- `GetTemporaryUrl(pathId)` / Try (FL1) returns an immediately usable unauthenticated URL, generally valid for 60 seconds; never persist it. Input is a complete logical path_id, not a URL.
- `CheckThumbnailStatus(pathId)` / Try (FL479) checks once; false is valid not-ready state. The caller owns polling. Logical path segments are encoded without treating reserved filename characters as URL syntax.
- Multipart uses the focused upload delegate, not ordinary scalar/JSON form encoding. HttpClient and curl consume the same streaming multipart bytes, including nonseekable streams, quoted/Unicode filenames and binary data. Input streams are never disposed by the client; positions are not restored and canceled reads can leave them indeterminate.
- `ZulipClient.UploadTimeout` defaults to 100 seconds; valid values are positive and at most 2,147,483,646 milliseconds. Cancellation/timeout and HTTP/subprocess/parse errors remain explicit; curl cleanup can add five seconds. An injected HttpClient may impose its own shorter timeout; cancellation-ignoring caller streams may outlive cleanup. No temporary payload files or automatic upload retries/resumption.
- Curl uploads require functional nonblocking stdin (`--upload-file .`); verified with Windows curl 8.22.0. On early process exit, cooperative source reads are canceled and actual status/body are retained independently of the writer; interrupted 2xx uploads remain explicit failures.

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
- Scheduled content: `ScheduledMessageObject`, `DraftObject`, identity-free `DraftInput`.
- Misc resources: `SavedSnippetObject`, `ReminderObject`, `NavigationViewObject` (defined in `MiscObjects.cs`).
- Org and config: `InviteObject`, `LinkifierObject`, `EmojiObject`, `ProfileFieldObject` (defined in `OrganizationObjects.cs`).
- Administration and files: `ChannelCreationOptions`, `GroupOptions`, `SubscriptionPropertyChange`, `AttachmentObject`; result types retain request-local diagnostics.

Channel/subscription models share archive/folder/topic/default-push/governance metadata. User and invitation models retain import/deletion and lifecycle fields. Named/anonymous permission groups round-trip unknown object fields and reject invalid variants. `StreamPostPolicy` is deprecated/inexact; authoritative group settings are not a substitute for evaluating realm permissions and channel access.

## Error Handling Contract

For `Try*` methods:

- `success == true`: required result fields and essential identities are present and correctly shaped. Real empty collections are valid; this is not exhaustive OpenAPI schema validation.
- `success == false`: `details` contains user-readable failure reason.
- `ignored_parameters_unsupported` is a nonfatal warning retained on responses/detailed results. `RawPayload`/`ExtensionData` preserve unknown response data.
- Batch Try success means the one request succeeded, not necessarily that all work completed. Inspect `Complete`; convenience methods reject known partial work and report unspecified legacy completion in Try details.
- Narrow flag results carry explicit range counts/cursors/boundaries. Nonnegative counts must satisfy updated <= processed; cursors are null exactly for empty ranges. Malformed progress explicitly warns that the update may have been applied. No internal continuation is performed.
- A malformed success after a mutation does not imply rollback. Inspect the retained response before retrying an operation with side effects.

For throwing variants:

- Throw `Exception` with details derived from `ZulipResponse.GetFailureMessage()` when operation fails.

---
Updated on: 2026-11-13

Documentation version: 1.0.0
