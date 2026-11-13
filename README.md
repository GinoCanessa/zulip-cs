# Early Development Warning

Note that this package is in the EARLY development stages and should not be used in production.

# Zulip C# Library

[![Tests](https://github.com/GinoCanessa/zulip-cs/actions/workflows/build-and-test.yaml/badge.svg)](https://github.com/GinoCanessa/zulip-cs/actions/workflows/build-and-test.yaml)
[![Publish dotnet library](https://img.shields.io/nuget/v/zulip-cs-lib.svg)](https://github.com/GinoCanessa/zulip-cs/actions/workflows/nuget-library.yaml)

`zulip-cs-lib` is a C# library for the [Zulip](http://zulip.com) REST API. Its primary target is **net11.0**, with continued support for **net10.0**, **net9.0**, and **net8.0**. It uses **System.Text.Json** for serialization and takes as few dependencies as possible.

Development and CI use the **.NET 11 SDK** by default, selected by `global.json`.

Code was developed using several existing projects for guidance and inspiration:
* [swift-zulip-api](https://github.com/zulip/swift-zulip-api)
* [zulip-csharp](https://github.com/zulip/zulip-csharp)
* [zulip-js](https://github.com/zulip/zulip-js)

## Releases

Publishing a GitHub Release for a `v<version>` tag triggers NuGet trusted
publishing after the tagged source passes cross-platform tests. Pushing a tag
alone does not publish. See the [release process](docs/releasing.md) for
one-time configuration, versioning, and validation-only manual runs.

## Getting Started

Create a `ZulipClient` by providing your Zulip site URL, email, and API key:

```csharp
using zulip_cs_lib;
using zulip_cs_lib.Models;
using zulip_cs_lib.Resources;

// Provide credentials directly
var client = new ZulipClient("https://zulip.example.com", "user@example.com", "your_api_key");

// Or load them from a zuliprc INI file
var client = new ZulipClient("path/to/zuliprc");
```

You can also supply your own `HttpClient` or use a `curl` subprocess as the HTTP backend:

```csharp
// Custom HttpClient
var client = new ZulipClient("https://zulip.example.com", "user@example.com", "your_api_key", httpClient);

// curl backend
var client = new ZulipClient("https://zulip.example.com", "user@example.com", "your_api_key", "curl");

// zuliprc + custom HttpClient or curl
var client = new ZulipClient("path/to/zuliprc", httpClient);
var client = new ZulipClient("path/to/zuliprc", "curl");
```

## API Overview

All Zulip API operations are accessed through resource properties on `ZulipClient`:

| Property | Description |
|---|---|
| `Messages` | Send, edit, delete, fetch, render messages; emoji reactions; flags; read state. |
| `Channels` | List, create, update, archive channels; manage subscriptions and topics. |
| `Users` | Get, create, update, deactivate users; presence; status; alert words; user groups. |
| `Server` | Get server settings (version, feature level). |
| `Events` | Register queues with initial state/capabilities, poll payload-bearing events, delete queues. |
| `ScheduledMessages` | Create, edit, delete, and list scheduled messages. |
| `Drafts` | Create, edit, delete, and list drafts. |
| `Invitations` | Send invitations, create invitation links, resend, revoke. |
| `Organization` | Linkifiers, custom emoji, custom profile fields. |
| `SavedSnippets` | Create, edit, delete, and list saved snippets. |
| `Reminders` | Create with identity/optional note, delete, and list reminder targets/content/failure state. |
| `NavigationViews` | Create, update, delete, and list fragment-identified views and pin state. |

### Throwing vs Try Pattern

Every public method is available in two styles:

- **Throwing** — returns the result directly, throws an `Exception` on failure.
- **Try** — returns a value tuple with `(bool success, string details, ...)` — never throws on API errors.

```csharp
// Throwing — simple, throws on failure
ulong id = await client.Messages.SendStream("Hello!", "greetings", "general");

// Try — caller decides how to handle failure
(bool success, string? details, ulong id) = await client.Messages.TrySendStream("Hello!", "greetings", "general");
if (!success)
{
    Console.WriteLine($"Failed: {details}");
}
```

Required response fields and essential identities are validated: missing/null/malformed data fails explicitly, while actual empty collections remain valid. Successful responses can still carry nonfatal `ignored_parameters_unsupported` warnings. Inspect `client.LastResponse?.IgnoredParametersUnsupported`, `RawPayload`, and `ExtensionData` for diagnostics and unmodeled data.

`LastResponse` is the **most recently completed** request, not a per-call association under concurrency. `PresenceResult`, `RegistrationResult`, and `BatchOperationResult` carry their own `Response`; prefer these for request-local diagnostics. Do not retry an applied mutation just because a compatibility warning was returned.

### Examples

**Send a direct message:**

```csharp
ulong id = await client.Messages.SendPrivate("Hello!", "user1@example.com", "user2@example.com");
```

**Fetch messages with a narrow:**

```csharp
using zulip_cs_lib.Resources;

var narrows = new[]
{
    new Narrow(NarrowOperator.Channel, "general"),
    new Narrow(NarrowOperator.Topic, "greetings"),
};

List<MessageObject> messages = await client.Messages.Get(
    Messages.GetAnchorMode.Newest,
    numBefore: 20,
    numAfter: 0,
    narrow: narrows);
```

**Manage channels:**

```csharp
// List all channels
List<StreamObject> channels = await client.Channels.GetAll();

// Get subscriptions
List<SubscriptionObject> subs = await client.Channels.GetSubscriptions();

// Get topics in a channel
List<TopicObject> topics = await client.Channels.GetTopics(streamId: 42);
```

**Register an event queue with initial state:**

```csharp
RegistrationResult registration = await client.Events.Register(new RegistrationOptions
{
    EventTypes = ["message", "update_message", "delete_message"],
    FetchEventTypes = ["realm", "realm_user", "subscription", "stream", "presence"],
    SlimPresence = true,
    ClientCapabilities = new Dictionary<string, bool> { ["bulk_message_deletion"] = true }
});
try
{
    List<EventObject> events = await client.Events.GetEvents(registration.QueueId, registration.LastEventId);
    // Apply initial state/events before advancing the polling cursor.
}
finally
{
    await client.Events.DeleteQueue(registration.QueueId);
}
```

`RawInitialState` owns the complete registration response. Typed initial fields depend on fetch filters; registration does not fetch message bodies. Timeout/capability metadata is retained, but the library does not configure HTTP timeouts, maintain a state store or retry polling automatically. Common message/update/delete/flag events expose typed fields; unmodeled fields and unknown event kinds are retained in `EventObject.ExtensionData`, even when they reuse a typed field name. `RegisterQueue` remains a queue/cursor-only projection.

**User management:**

```csharp
UserObject me = await client.Users.GetOwnUser();
List<UserObject> allUsers = await client.Users.GetAll();
PresenceResult presence = await client.Users.GetPresenceDetails("user@example.com");
long? lastActive = presence.Presence.ActiveTimestamp;
double? serverClock = presence.ServerTimestamp;
```

`GetPresence` remains a legacy dictionary adapter using actual server-provided client entries. On modern-only payloads it fails with guidance to use `GetPresenceDetails`; no status is synthesized from local wall-clock time. `GetGroupMembers(groupId, directMemberOnly: true)` exposes direct-only filtering without changing the original overload.

**Manage fragment-based navigation views and reminders:**

```csharp
await client.NavigationViews.Create("narrow/is/starred", isPinned: true, name: "Important");
await client.NavigationViews.Update("narrow/is/starred", isPinned: false);
await client.NavigationViews.Delete("narrow/is/starred");
int reminderId = await client.Reminders.CreateWithId(
    messageId: 100,
    scheduledDeliveryTimestamp: DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds(),
    note: "Follow up");
```

Navigation identity is `Fragment`, not a numeric ID. Name-only creation and numeric update/delete signatures are obsolete and fail explicitly **without a request**; migrate to fragment overloads. Fragments preserve slash-separated segments and encode reserved characters; empty/dot traversal segments are rejected. Reminder reads retain `reminder_target_message_id`, content/rendered content, recipients and failure state.

**Inspect one batch rather than assuming completion:**

```csharp
BatchOperationResult batch = await client.Channels.DeleteTopicBatch(42, "old topic");
// Complete == false: work was applied but remains; the caller may request another batch.
// Complete == null: a legacy response did not specify completion.
// Complete == true: this operation is complete.
```

`Messages.MarkAllAsReadBatch` has the same one-shot contract. No internal continuation loop is run. Existing `DeleteTopic`/`MarkAllAsRead` convenience methods fail/throw for **known partial** work; missing legacy completion retains success with explicit Try details and unknown state on batch results. A failure reporting partial work is not a rollback.

**Fetch an API key from username/password:**

```csharp
(bool success, string details, string apiKey, string email) =
    await ZulipClient.TryFetchApiKey("https://zulip.example.com", "user@example.com", "password");
```

### Narrow Builder

The `Narrow` class helps build Zulip message query filters:

```csharp
// Single narrow
var narrow = new Narrow(NarrowOperator.Channel, "general");

// Archived channels (Channels without an operand defaults to public)
var archived = new Narrow(NarrowOperator.Channels, "archived");

// Negated narrow
var narrow = new Narrow(NarrowOperator.IsStarred, negated: true);

// Serialize to JSON for the API
string json = Narrow.ToJsonArray(
    new Narrow(NarrowOperator.Channel, "general"),
    new Narrow(NarrowOperator.Topic, "greetings"));
```

Available operators: `Channel`, `Channels`, `Topic`, `Dm`, `DmIncluding`, `Sender`, `Search`, `Near`, `Id`, `HasAttachment`, `HasLink`, `HasImage`, `HasReaction`, `IsAlerted`, `IsMentioned`, `IsDm`, `IsStarred`, `IsUnread`, `IsFollowed`, `IsMuted`, `With`, `Mentions`.

### Current Model Metadata

Message/history models retain reactions and edit/move metadata. Channel/subscription models retain archive/folder/topic/default-push settings and permission groups; user/group/invitation models retain import/deletion, governance and lifecycle metadata. `GroupSettingValue` represents either a named integer `GroupId` or anonymous `DirectMembers`/`DirectSubgroups`, preserving unmodeled object fields.

`StreamPostPolicy` is deprecated and an **inexact superset**, not authorization. Use authoritative channel group settings such as `CanSendMessageGroup`, together with realm permissions and channel access; do not infer posting rights from the coarse policy alone. Optional metadata varies by server feature level; read `Server.GetSettings` and compatibility warnings before relying on newer features.

## Architecture

`ZulipClient` authenticates via Basic auth (email + API key) and delegates HTTP execution to either `HttpClient` or a `curl` subprocess. API resource classes (e.g., `Messages`, `Channels`) receive a request delegate from `ZulipClient` rather than making HTTP calls directly, enabling the dual-backend design and straightforward testing via mocked `HttpMessageHandler`.

# CLI

`zulip-cs-cli` is a command-line client for the Zulip REST API, built on top of `zulip-cs-lib`.

## Configuration

The CLI reads credentials from a `zuliprc` INI file. You can specify the path explicitly with the `--zuliprc` global option, or let the CLI find it automatically — it searches upward from the executable directory for a file named `zuliprc` (also checking a `secrets/` subdirectory at each level).

Example `zuliprc` file:

```ini
[api]
email=user@example.com
key=your_api_key_here
site=https://zulip.example.com
```

## Usage

```
zulip-cs-cli [command] [options]
```

### Global Options

| Option | Description |
|---|---|
| `--zuliprc <path>` | Path to the zuliprc configuration file. |

### Commands

#### Sending Messages

| Command | Description |
|---|---|
| `send-pm` | Send a direct (private) message. |
| `send-stream` | Send a message to a stream/channel. |

**`send-pm`** — Send a direct message by email addresses or user IDs.

```
zulip-cs-cli send-pm --message "Hello!" --emails "user1@example.com,user2@example.com"
zulip-cs-cli send-pm --message "Hello!" --user-ids "123,456"
```

**`send-stream`** — Send a message to exactly one stream by literal name or ID. Numeric/JSON-looking names remain names; commas in `--streams` are part of the name, not a destination list.

```
zulip-cs-cli send-stream --message "Hello!" --topic "greetings" --streams "general"
zulip-cs-cli send-stream --message "Hello!" --topic "greetings" --stream-ids "42"
```

#### Managing Messages

| Command | Description |
|---|---|
| `edit-message` | Edit an existing message. |
| `delete-message` | Delete a message. |
| `get-message` | Fetch a single message by ID. |
| `get-messages` | Fetch multiple messages. |
| `get-edit-history` | Get the edit history of a message. |

**`edit-message`** — Edit content, topic, or move a message to another stream.

```
zulip-cs-cli edit-message --message-id 123 --content "Updated text"
zulip-cs-cli edit-message --message-id 123 --topic "new topic" --propagate-mode all
```

Options: `--message-id` (required), `--content`, `--topic`, `--move-to-stream-id`, `--propagate-mode` (one | later | all, default: one).

**`delete-message`**

```
zulip-cs-cli delete-message --message-id 123
```

**`get-message`** — Fetch a single message. Output is JSON.

```
zulip-cs-cli get-message --message-id 123
zulip-cs-cli get-message --message-id 123 --apply-markdown true
```

**`get-messages`** — Fetch multiple messages. Output is JSON.

```
zulip-cs-cli get-messages --anchor newest --num-before 10
zulip-cs-cli get-messages --anchor oldest --num-after 5
zulip-cs-cli get-messages --anchor 12345 --num-before 3 --num-after 3
```

Options: `--anchor` (newest | oldest | first_unread | message ID, default: newest), `--num-before`, `--num-after`, `--apply-markdown`.

#### Emoji Reactions

| Command | Description |
|---|---|
| `add-emoji` | Add an emoji reaction to a message. |
| `remove-emoji` | Remove an emoji reaction from a message. |

```
zulip-cs-cli add-emoji --message-id 123 --emoji-name "thumbs_up"
zulip-cs-cli remove-emoji --message-id 123 --emoji-name "thumbs_up"
```

Both commands accept optional `--emoji-code` and `--reaction-type` options.

#### Message Flags

| Command | Description |
|---|---|
| `update-flags` | Update personal message flags (e.g., read, starred). |

```
zulip-cs-cli update-flags --message-ids "123,456" --op add --flag starred
zulip-cs-cli update-flags --message-ids "123,456" --op remove --flag read
```

#### Rendering

| Command | Description |
|---|---|
| `render-message` | Render message content to HTML. |

```
zulip-cs-cli render-message --content "**bold** and *italic*"
```

#### Read State

| Command | Description |
|---|---|
| `mark-all-read` | Mark all messages as read. |
| `mark-stream-read` | Mark all messages in a stream as read. |
| `mark-topic-read` | Mark all messages in a topic as read. |
| `get-read-receipts` | Get read receipts for a message. |

```
zulip-cs-cli mark-all-read
zulip-cs-cli mark-stream-read --stream-id 42
zulip-cs-cli mark-topic-read --stream-id 42 --topic "greetings"
zulip-cs-cli get-read-receipts --message-id 123
```
