# Functionality Summary

This document provides a high-level summary of what `zulip-cs` currently does.

## Core Library (`zulip-cs-lib`)

Primary functional areas:

- Message operations (send/edit/delete/fetch/render/reactions/flags/read state/history/receipts).
- Channel operations (list/manage/archive/subscriptions/topics/default channels/topic muting).
- User operations (lookup/admin lifecycle/status/presence/typing/muting/alert words/groups).
- Server introspection (Zulip version + feature level).
- Event queue management (register, poll, delete).
- Scheduled messages, drafts, invitations, reminders, saved snippets, navigation views.
- Organization-level metadata (linkifiers, custom emoji, profile fields).

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

## Functional Boundaries

- The library is a wrapper around Zulip REST API endpoints and does not implement an offline store.
- CLI output is terminal-oriented text/JSON and is not a full interactive TUI.
- Authentication in normal flows is API key based (Basic auth header).

---
Generated on: 2026-03-05  
Published version: 0.0.1-beta.1
