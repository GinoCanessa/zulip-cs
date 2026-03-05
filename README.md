# Early Development Warning

Note that this package is in the EARLY development stages and should not be used in production.

# Zulip C# Library

`zulip-cs-lib` is a C# library for the [Zulip](http://zulip.com) REST API. It targets **net8.0**, **net9.0**, and **net10.0**, uses **System.Text.Json** for serialization, and takes as few dependencies as possible.

Code was developed using several existing projects for guidance and inspiration:
* [swift-zulip-api](https://github.com/zulip/swift-zulip-api)
* [zulip-csharp](https://github.com/zulip/zulip-csharp)
* [zulip-js](https://github.com/zulip/zulip-js)

## Getting Started

Create a `ZulipClient` by providing your Zulip site URL, email, and API key:

```csharp
using zulip_cs_lib;

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
var client = new ZulipClient("https://zulip.example.com", "user@example.com", "your_api_key", "/usr/bin/curl");

// zuliprc + custom HttpClient or curl
var client = new ZulipClient("path/to/zuliprc", httpClient);
var client = new ZulipClient("path/to/zuliprc", "/usr/bin/curl");
```

## API Overview

All Zulip API operations are accessed through resource properties on `ZulipClient`:

| Property | Description |
|---|---|
| `Messages` | Send, edit, delete, fetch, render messages; emoji reactions; flags; read state. |
| `Channels` | List, create, update, archive channels; manage subscriptions and topics. |
| `Users` | Get, create, update, deactivate users; presence; status; alert words; user groups. |
| `Server` | Get server settings (version, feature level). |
| `Events` | Register event queues, poll for events, delete queues. |
| `ScheduledMessages` | Create, edit, delete, and list scheduled messages. |
| `Drafts` | Create, edit, delete, and list drafts. |
| `Invitations` | Send invitations, create invitation links, resend, revoke. |
| `Organization` | Linkifiers, custom emoji, custom profile fields. |
| `SavedSnippets` | Create, edit, delete, and list saved snippets. |
| `Reminders` | Create, delete, and list reminders. |
| `NavigationViews` | Create, update, delete, and list navigation views. |

### Throwing vs Try Pattern

Every public method is available in two styles:

- **Throwing** — returns the result directly, throws an `Exception` on failure.
- **Try** — returns a value tuple with `(bool success, string details, ...)` — never throws on API errors.

```csharp
// Throwing — simple, throws on failure
ulong id = await client.Messages.SendStream("Hello!", "greetings", "general");

// Try — caller decides how to handle failure
(bool success, string details, ulong id) = await client.Messages.TrySendStream("Hello!", "greetings", "general");
if (!success)
{
    Console.WriteLine($"Failed: {details}");
}
```

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
    GetAnchorMode.Newest,
    numBefore: 20,
    numAfter: 0,
    narrows: narrows);
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

**Register an event queue:**

```csharp
(string queueId, int lastEventId) = await client.Events.RegisterQueue();

// Poll for events
List<EventObject> events = await client.Events.GetEvents(queueId, lastEventId);

// Clean up
await client.Events.DeleteQueue(queueId);
```

**User management:**

```csharp
UserObject me = await client.Users.GetOwnUser();
List<UserObject> allUsers = await client.Users.GetAll();
```

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

// Negated narrow
var narrow = new Narrow(NarrowOperator.IsStarred, negated: true);

// Serialize to JSON for the API
string json = Narrow.ToJsonArray(
    new Narrow(NarrowOperator.Channel, "general"),
    new Narrow(NarrowOperator.Topic, "greetings"));
```

Available operators include: `Channel`, `Topic`, `Dm`, `DmIncluding`, `Sender`, `Search`, `Near`, `Id`, `HasAttachment`, `HasLink`, `HasImage`, `HasReaction`, `IsAlerted`, `IsMentioned`, `IsDm`, `IsStarred`, `IsUnread`, `IsFollowed`, `IsMuted`, `With`, `Mentions`, and more.

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

**`send-stream`** — Send a message to one or more streams by name or ID.

```
zulip-cs-cli send-stream --message "Hello!" --topic "greetings" --streams "general"
zulip-cs-cli send-stream --message "Hello!" --topic "greetings" --stream-ids "1,2"
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
