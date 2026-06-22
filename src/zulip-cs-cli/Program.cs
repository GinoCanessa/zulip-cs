using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib;
using zulip_cs_lib.Resources;

namespace zulip_cs_cli;

/// <summary>A utility CLI for zulip-cs-lib.</summary>
public static class Program
{
    /// <summary>Main entry-point for this application.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    static async Task<int> Main(string[] args)
    {
        Option<string> zuliprcOption = new("--zuliprc") { Description = "Path to the zuliprc configuration file." };

        RootCommand rootCommand = new("Zulip CLI — a command-line client for the Zulip REST API.");
        zuliprcOption.Recursive = true;
        rootCommand.Options.Add(zuliprcOption);

        rootCommand.Subcommands.Add(BuildSendPmCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildSendStreamCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildEditMessageCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildDeleteMessageCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildGetMessageCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildGetMessagesCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildAddEmojiCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildRemoveEmojiCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildRenderMessageCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildUpdateFlagsCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildGetEditHistoryCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildMarkAllReadCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildMarkStreamReadCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildMarkTopicReadCommand(zuliprcOption));
        rootCommand.Subcommands.Add(BuildGetReadReceiptsCommand(zuliprcOption));

        return await rootCommand.Parse(args).InvokeAsync();
    }

    /// <summary>Creates a ZulipClient from the resolved zuliprc path.</summary>
    /// <param name="zuliprcFilename">Explicit path or empty to search.</param>
    /// <returns>A configured ZulipClient.</returns>
    private static ZulipClient CreateClient(string? zuliprcFilename)
    {
        if (string.IsNullOrEmpty(zuliprcFilename))
        {
            zuliprcFilename = FindZulipRC(AppContext.BaseDirectory);
        }

        if (!File.Exists(zuliprcFilename))
        {
            throw new FileNotFoundException($"Could not find file: {zuliprcFilename}");
        }

        return new ZulipClient(zuliprcFilename);
    }

    // ── send-pm ──────────────────────────────────────────────────────────

    /// <summary>Builds the send-pm command.</summary>
    private static Command BuildSendPmCommand(Option<string> zuliprcOption)
    {
        Option<string> messageOpt = new("--message") { Description = "Message content.", Required = true };
        Option<string> emailsOpt = new("--emails") { Description = "Comma-separated recipient email addresses." };
        Option<string> userIdsOpt = new("--user-ids") { Description = "Comma-separated recipient user IDs." };

        Command cmd = new("send-pm", "Send a direct (private) message.");
        cmd.Options.Add(messageOpt);
        cmd.Options.Add(emailsOpt);
        cmd.Options.Add(userIdsOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            string? message = parseResult.GetValue(messageOpt);
            string? emails = parseResult.GetValue(emailsOpt);
            string? userIds = parseResult.GetValue(userIdsOpt);

            ZulipClient client = CreateClient(zuliprc);

            (bool success, string? details, ulong messageId) result;

            if (!string.IsNullOrEmpty(emails))
            {
                string[] emailArr = emails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                result = await client.Messages.TrySendPrivate(message!, emailArr);
            }
            else if (!string.IsNullOrEmpty(userIds))
            {
                int[] idArr = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse).ToArray();
                result = await client.Messages.TrySendPrivate(message!, idArr);
            }
            else
            {
                Console.Error.WriteLine("Error: --emails or --user-ids is required.");
                return 1;
            }

            if (result.success)
            {
                Console.WriteLine($"Message sent. ID: {result.messageId}");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── send-stream ──────────────────────────────────────────────────────

    /// <summary>Builds the send-stream command.</summary>
    private static Command BuildSendStreamCommand(Option<string> zuliprcOption)
    {
        Option<string> messageOpt = new("--message") { Description = "Message content.", Required = true };
        Option<string> topicOpt = new("--topic") { Description = "Stream topic.", Required = true };
        Option<string> streamsOpt = new("--streams") { Description = "Comma-separated stream names." };
        Option<string> streamIdsOpt = new("--stream-ids") { Description = "Comma-separated stream IDs." };

        Command cmd = new("send-stream", "Send a message to a stream/channel.");
        cmd.Options.Add(messageOpt);
        cmd.Options.Add(topicOpt);
        cmd.Options.Add(streamsOpt);
        cmd.Options.Add(streamIdsOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            string? message = parseResult.GetValue(messageOpt);
            string? topic = parseResult.GetValue(topicOpt);
            string? streams = parseResult.GetValue(streamsOpt);
            string? streamIds = parseResult.GetValue(streamIdsOpt);

            ZulipClient client = CreateClient(zuliprc);

            (bool success, string? details, ulong messageId) result;

            if (!string.IsNullOrEmpty(streams))
            {
                string[] streamArr = streams.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                result = await client.Messages.TrySendStream(message!, topic!, streamArr);
            }
            else if (!string.IsNullOrEmpty(streamIds))
            {
                int[] idArr = streamIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse).ToArray();
                result = await client.Messages.TrySendStream(message!, topic!, idArr);
            }
            else
            {
                Console.Error.WriteLine("Error: --streams or --stream-ids is required.");
                return 1;
            }

            if (result.success)
            {
                Console.WriteLine($"Message sent. ID: {result.messageId}");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── edit-message ─────────────────────────────────────────────────────

    /// <summary>Builds the edit-message command.</summary>
    private static Command BuildEditMessageCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id") { Description = "ID of the message to edit.", Required = true };
        Option<string> contentOpt = new("--content") { Description = "New message content." };
        Option<string> topicOpt = new("--topic") { Description = "New topic." };
        Option<int?> moveToStreamOpt = new("--move-to-stream-id") { Description = "Stream ID to move the message to." };
        Option<string> propagateOpt = new("--propagate-mode") { Description = "Propagate mode: one, later, or all.", DefaultValueFactory = _ => "one" };

        Command cmd = new("edit-message", "Edit an existing message.");
        cmd.Options.Add(messageIdOpt);
        cmd.Options.Add(contentOpt);
        cmd.Options.Add(topicOpt);
        cmd.Options.Add(moveToStreamOpt);
        cmd.Options.Add(propagateOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            ulong messageId = parseResult.GetValue(messageIdOpt);
            string? content = parseResult.GetValue(contentOpt);
            string? topic = parseResult.GetValue(topicOpt);
            int? moveToStream = parseResult.GetValue(moveToStreamOpt);
            string? propagate = parseResult.GetValue(propagateOpt);

            Messages.EditPropagateMode mode = propagate?.ToLowerInvariant() switch
            {
                "later" => Messages.EditPropagateMode.Later,
                "all" => Messages.EditPropagateMode.All,
                _ => Messages.EditPropagateMode.One,
            };

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryEdit(messageId, content!, topic!, moveToStream, mode);

            if (result.success)
            {
                Console.WriteLine("Message edited successfully.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── delete-message ───────────────────────────────────────────────────

    /// <summary>Builds the delete-message command.</summary>
    private static Command BuildDeleteMessageCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id") { Description = "ID of the message to delete.", Required = true };

        Command cmd = new("delete-message", "Delete a message.");
        cmd.Options.Add(messageIdOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            ulong messageId = parseResult.GetValue(messageIdOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryDelete(messageId);

            if (result.success)
            {
                Console.WriteLine("Message deleted.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── get-message ──────────────────────────────────────────────────────

    /// <summary>Builds the get-message command.</summary>
    private static Command BuildGetMessageCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id") { Description = "ID of the message to retrieve.", Required = true };
        Option<bool?> markdownOpt = new("--apply-markdown") { Description = "Whether to apply markdown rendering." };

        Command cmd = new("get-message", "Fetch a single message by ID.");
        cmd.Options.Add(messageIdOpt);
        cmd.Options.Add(markdownOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            ulong messageId = parseResult.GetValue(messageIdOpt);
            bool? markdown = parseResult.GetValue(markdownOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryGetSingle(messageId, markdown);

            if (result.success)
            {
                Console.WriteLine(JsonSerializer.Serialize(result.message, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── get-messages ─────────────────────────────────────────────────────

    /// <summary>Builds the get-messages command.</summary>
    private static Command BuildGetMessagesCommand(Option<string> zuliprcOption)
    {
        Option<string> streamIdOpt = new("--stream-id") { Description = "Stream ID to filter messages." };
        Option<string> anchorOpt = new("--anchor") { Description = "Anchor: newest, oldest, first_unread, or a message ID.", DefaultValueFactory = _ => "newest" };
        Option<int> numBeforeOpt = new("--num-before") { Description = "Number of messages before the anchor.", DefaultValueFactory = _ => 0 };
        Option<int> numAfterOpt = new("--num-after") { Description = "Number of messages after the anchor.", DefaultValueFactory = _ => 0 };
        Option<bool?> markdownOpt = new("--apply-markdown") { Description = "Whether to apply markdown rendering." };
        Option<bool?> includeAnchorOpt = new("--include-anchor") { Description = "Whether to include the anchor message in results." };

        Command cmd = new("get-messages", "Fetch multiple messages.");
        cmd.Options.Add(streamIdOpt);
        cmd.Options.Add(anchorOpt);
        cmd.Options.Add(numBeforeOpt);
        cmd.Options.Add(numAfterOpt);
        cmd.Options.Add(markdownOpt);
        cmd.Options.Add(includeAnchorOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            string? streamIdValue = parseResult.GetValue(streamIdOpt);
            string? anchor = parseResult.GetValue(anchorOpt);
            int numBefore = parseResult.GetValue(numBeforeOpt);
            int numAfter = parseResult.GetValue(numAfterOpt);
            bool? markdown = parseResult.GetValue(markdownOpt);
            bool? includeAnchor = parseResult.GetValue(includeAnchorOpt);

            Messages.GetAnchorMode anchorMode;
            ulong? anchorId = null;

            switch (anchor?.ToLowerInvariant())
            {
                case "oldest":
                    anchorMode = Messages.GetAnchorMode.Oldest;
                    break;
                case "first_unread":
                    anchorMode = Messages.GetAnchorMode.FirstUnread;
                    break;
                case "newest":
                case null:
                    anchorMode = Messages.GetAnchorMode.Newest;
                    break;
                default:
                    anchorMode = Messages.GetAnchorMode.Id;
                    anchorId = ulong.Parse(anchor);
                    break;
            }

            ZulipClient client = CreateClient(zuliprc);

            Narrow[] narrows;
            if (!string.IsNullOrEmpty(streamIdValue) &&
                long.TryParse(streamIdValue, out long streamId))
            {
                narrows = [new Narrow(Narrow.NarrowOperator.Channel, streamId)];
            }
            else
            {
                narrows = [];
            }

            var result = await client.Messages.TryGet(
                anchorMode, 
                anchorId, 
                numBefore, 
                numAfter, 
                applyMarkdown: markdown,
                narrow: narrows,
                includeAnchor: includeAnchor);

            if (result.success)
            {
                Console.WriteLine(JsonSerializer.Serialize(result.messages, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── add-emoji ────────────────────────────────────────────────────────

    /// <summary>Builds the add-emoji command.</summary>
    private static Command BuildAddEmojiCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id") { Description = "ID of the message.", Required = true };
        Option<string> emojiNameOpt = new("--emoji-name") { Description = "Emoji name.", Required = true };
        Option<string> emojiCodeOpt = new("--emoji-code") { Description = "Emoji code." };
        Option<string> reactionTypeOpt = new("--reaction-type") { Description = "Reaction type." };

        Command cmd = new("add-emoji", "Add an emoji reaction to a message.");
        cmd.Options.Add(messageIdOpt);
        cmd.Options.Add(emojiNameOpt);
        cmd.Options.Add(emojiCodeOpt);
        cmd.Options.Add(reactionTypeOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            ulong messageId = parseResult.GetValue(messageIdOpt);
            string? emojiName = parseResult.GetValue(emojiNameOpt);
            string? emojiCode = parseResult.GetValue(emojiCodeOpt);
            string? reactionType = parseResult.GetValue(reactionTypeOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryAddEmoji(messageId, emojiName!, emojiCode!, reactionType!);

            if (result.success)
            {
                Console.WriteLine("Emoji added.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── remove-emoji ─────────────────────────────────────────────────────

    /// <summary>Builds the remove-emoji command.</summary>
    private static Command BuildRemoveEmojiCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id") { Description = "ID of the message.", Required = true };
        Option<string> emojiNameOpt = new("--emoji-name") { Description = "Emoji name." };
        Option<string> emojiCodeOpt = new("--emoji-code") { Description = "Emoji code." };
        Option<string> reactionTypeOpt = new("--reaction-type") { Description = "Reaction type." };

        Command cmd = new("remove-emoji", "Remove an emoji reaction from a message.");
        cmd.Options.Add(messageIdOpt);
        cmd.Options.Add(emojiNameOpt);
        cmd.Options.Add(emojiCodeOpt);
        cmd.Options.Add(reactionTypeOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            ulong messageId = parseResult.GetValue(messageIdOpt);
            string? emojiName = parseResult.GetValue(emojiNameOpt);
            string? emojiCode = parseResult.GetValue(emojiCodeOpt);
            string? reactionType = parseResult.GetValue(reactionTypeOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryRemoveEmoji(messageId, emojiName!, emojiCode!, reactionType!);

            if (result.success)
            {
                Console.WriteLine("Emoji removed.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── render-message ───────────────────────────────────────────────────

    /// <summary>Builds the render-message command.</summary>
    private static Command BuildRenderMessageCommand(Option<string> zuliprcOption)
    {
        Option<string> contentOpt = new("--content") { Description = "Message content to render.", Required = true };

        Command cmd = new("render-message", "Render message content to HTML.");
        cmd.Options.Add(contentOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            string? content = parseResult.GetValue(contentOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryRender(content!);

            if (result.success)
            {
                Console.WriteLine(result.renderedHtml);
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── update-flags ─────────────────────────────────────────────────────

    /// <summary>Builds the update-flags command.</summary>
    private static Command BuildUpdateFlagsCommand(Option<string> zuliprcOption)
    {
        Option<string> messageIdsOpt = new("--message-ids") { Description = "Comma-separated message IDs.", Required = true };
        Option<string> opOpt = new("--op") { Description = "Operation: add or remove.", Required = true };
        Option<string> flagOpt = new("--flag") { Description = "Flag name (e.g., read, starred).", Required = true };

        Command cmd = new("update-flags", "Update personal message flags.");
        cmd.Options.Add(messageIdsOpt);
        cmd.Options.Add(opOpt);
        cmd.Options.Add(flagOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            string? messageIdsStr = parseResult.GetValue(messageIdsOpt);
            string? op = parseResult.GetValue(opOpt);
            string? flag = parseResult.GetValue(flagOpt);

            ulong[] messageIds = messageIdsStr!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ulong.Parse).ToArray();

            Messages.FlagOperation operation = op?.ToLowerInvariant() == "remove"
                ? Messages.FlagOperation.Remove
                : Messages.FlagOperation.Add;

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryUpdateFlags(messageIds, operation, flag!);

            if (result.success)
            {
                Console.WriteLine("Flags updated.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── get-edit-history ─────────────────────────────────────────────────

    /// <summary>Builds the get-edit-history command.</summary>
    private static Command BuildGetEditHistoryCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id") { Description = "ID of the message.", Required = true };

        Command cmd = new("get-edit-history", "Get the edit history of a message.");
        cmd.Options.Add(messageIdOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            ulong messageId = parseResult.GetValue(messageIdOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryGetEditHistory(messageId);

            if (result.success)
            {
                Console.WriteLine(JsonSerializer.Serialize(result.history, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── mark-all-read ────────────────────────────────────────────────────

    /// <summary>Builds the mark-all-read command.</summary>
    private static Command BuildMarkAllReadCommand(Option<string> zuliprcOption)
    {
        Command cmd = new("mark-all-read", "Mark all messages as read.");

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryMarkAllAsRead();

            if (result.success)
            {
                Console.WriteLine("All messages marked as read.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── mark-stream-read ─────────────────────────────────────────────────

    /// <summary>Builds the mark-stream-read command.</summary>
    private static Command BuildMarkStreamReadCommand(Option<string> zuliprcOption)
    {
        Option<int> streamIdOpt = new("--stream-id") { Description = "Stream ID.", Required = true };

        Command cmd = new("mark-stream-read", "Mark all messages in a stream as read.");
        cmd.Options.Add(streamIdOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            int streamId = parseResult.GetValue(streamIdOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryMarkStreamAsRead(streamId);

            if (result.success)
            {
                Console.WriteLine("Stream marked as read.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── mark-topic-read ──────────────────────────────────────────────────

    /// <summary>Builds the mark-topic-read command.</summary>
    private static Command BuildMarkTopicReadCommand(Option<string> zuliprcOption)
    {
        Option<int> streamIdOpt = new("--stream-id") { Description = "Stream ID.", Required = true };
        Option<string> topicOpt = new("--topic") { Description = "Topic name.", Required = true };

        Command cmd = new("mark-topic-read", "Mark all messages in a topic as read.");
        cmd.Options.Add(streamIdOpt);
        cmd.Options.Add(topicOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            int streamId = parseResult.GetValue(streamIdOpt);
            string? topic = parseResult.GetValue(topicOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryMarkTopicAsRead(streamId, topic!);

            if (result.success)
            {
                Console.WriteLine("Topic marked as read.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── get-read-receipts ────────────────────────────────────────────────

    /// <summary>Builds the get-read-receipts command.</summary>
    private static Command BuildGetReadReceiptsCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id") { Description = "ID of the message.", Required = true };

        Command cmd = new("get-read-receipts", "Get read receipts for a message.");
        cmd.Options.Add(messageIdOpt);

        cmd.SetAction(async (ParseResult parseResult) =>
        {
            string? zuliprc = parseResult.GetValue(zuliprcOption);
            ulong messageId = parseResult.GetValue(messageIdOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryGetReadReceipts(messageId);

            if (result.success)
            {
                Console.WriteLine(JsonSerializer.Serialize(result.userIds, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                return 1;
            }

            return 0;
        });

        return cmd;
    }

    // ── Utilities ────────────────────────────────────────────────────────

    /// <summary>Searches for the first zulip RC file.</summary>
    /// <exception cref="DirectoryNotFoundException">Thrown when the requested directory is not
    ///  present.</exception>
    /// <param name="startingDir">The starting dir.</param>
    /// <returns>The found zulip RC file path.</returns>
    public static string FindZulipRC(string startingDir)
    {
        string currentDir = startingDir;
        string filePath = Path.Combine(currentDir, "zuliprc");

        while (!File.Exists(filePath))
        {
            // check for /secrets/.zuliprc
            string pathInSubdir = Path.Combine(currentDir, "secrets", "zuliprc");

            if (File.Exists(pathInSubdir))
            {
                return pathInSubdir;
            }

            currentDir = Path.GetFullPath(Path.Combine(currentDir, ".."));

            if (currentDir == Path.GetPathRoot(currentDir))
            {
                throw new DirectoryNotFoundException("Could not find zuliprc in path!");
            }

            filePath = Path.Combine(currentDir, "zuliprc");
        }

        return filePath;
    }
}
