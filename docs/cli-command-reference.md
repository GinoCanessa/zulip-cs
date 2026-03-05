# CLI Command Reference

`zulip-cs-cli` is a command-line wrapper around `zulip-cs-lib` message-oriented APIs.

## Invocation

```bash
zulip-cs-cli [command] [options]
```

## Global Option

| Option | Required | Description |
|---|---|---|
| `--zuliprc <path>` | No | Path to the Zulip INI file. If omitted, CLI auto-searches for `zuliprc` or `secrets/zuliprc` upward from executable directory. |

## Commands

### 1. `send-pm`

Send a private/direct message.

| Option | Required | Description |
|---|---|---|
| `--message <text>` | Yes | Message content. |
| `--emails <csv>` | Conditional | Comma-separated recipient emails. |
| `--user-ids <csv>` | Conditional | Comma-separated recipient user IDs. |

Notes:

- One of `--emails` or `--user-ids` must be provided.

### 2. `send-stream`

Send a message to one or more streams/channels.

| Option | Required | Description |
|---|---|---|
| `--message <text>` | Yes | Message content. |
| `--topic <topic>` | Yes | Stream topic. |
| `--streams <csv>` | Conditional | Comma-separated stream names. |
| `--stream-ids <csv>` | Conditional | Comma-separated stream IDs. |

Notes:

- One of `--streams` or `--stream-ids` must be provided.

### 3. `edit-message`

Edit existing message content/topic/stream.

| Option | Required | Description |
|---|---|---|
| `--message-id <id>` | Yes | Target message ID. |
| `--content <text>` | No | New content. |
| `--topic <topic>` | No | New topic. |
| `--move-to-stream-id <id>` | No | New stream ID destination. |
| `--propagate-mode <mode>` | No | `one`, `later`, `all` (default: `one`). |

### 4. `delete-message`

Delete a message.

| Option | Required | Description |
|---|---|---|
| `--message-id <id>` | Yes | Message ID to delete. |

### 5. `get-message`

Fetch one message and output JSON.

| Option | Required | Description |
|---|---|---|
| `--message-id <id>` | Yes | Message ID to fetch. |
| `--apply-markdown <bool>` | No | Whether server should apply markdown rendering. |

### 6. `get-messages`

Fetch multiple messages and output JSON.

| Option | Required | Description |
|---|---|---|
| `--anchor <value>` | No | `newest`, `oldest`, `first_unread`, or numeric message ID (default: `newest`). |
| `--num-before <n>` | No | Number of messages before anchor (default: `0`). |
| `--num-after <n>` | No | Number of messages after anchor (default: `0`). |
| `--apply-markdown <bool>` | No | Whether server should apply markdown rendering. |

### 7. `add-emoji`

Add emoji reaction to message.

| Option | Required | Description |
|---|---|---|
| `--message-id <id>` | Yes | Message ID. |
| `--emoji-name <name>` | Yes | Emoji name. |
| `--emoji-code <code>` | No | Emoji code. |
| `--reaction-type <type>` | No | Reaction type. |

### 8. `remove-emoji`

Remove emoji reaction from message.

| Option | Required | Description |
|---|---|---|
| `--message-id <id>` | Yes | Message ID. |
| `--emoji-name <name>` | No | Emoji name. |
| `--emoji-code <code>` | No | Emoji code. |
| `--reaction-type <type>` | No | Reaction type. |

### 9. `render-message`

Render message content as HTML.

| Option | Required | Description |
|---|---|---|
| `--content <text>` | Yes | Message content to render. |

### 10. `update-flags`

Update personal message flags.

| Option | Required | Description |
|---|---|---|
| `--message-ids <csv>` | Yes | Comma-separated message IDs. |
| `--op <op>` | Yes | `add` or `remove`. |
| `--flag <flag>` | Yes | Flag name (e.g., `read`, `starred`). |

### 11. `get-edit-history`

Fetch edit history as JSON.

| Option | Required | Description |
|---|---|---|
| `--message-id <id>` | Yes | Message ID. |

### 12. `mark-all-read`

Mark all messages as read.

- No command-specific options.

### 13. `mark-stream-read`

Mark all messages in a stream as read.

| Option | Required | Description |
|---|---|---|
| `--stream-id <id>` | Yes | Stream ID. |

### 14. `mark-topic-read`

Mark all messages in a topic as read.

| Option | Required | Description |
|---|---|---|
| `--stream-id <id>` | Yes | Stream ID. |
| `--topic <name>` | Yes | Topic name. |

### 15. `get-read-receipts`

Fetch read receipt user IDs as JSON.

| Option | Required | Description |
|---|---|---|
| `--message-id <id>` | Yes | Message ID. |

## Exit Behavior

- Success path: command returns exit code `0`.
- Failure path: command writes to stderr and sets non-zero exit code.

---
Generated on: 2026-03-05  
Published version: 0.0.1-beta.1
