using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
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
        Option<string> zuliprcOption = new("--zuliprc", "Path to the zuliprc configuration file.");

        RootCommand rootCommand = new("Zulip CLI — a command-line client for the Zulip REST API.");
        rootCommand.AddGlobalOption(zuliprcOption);

        rootCommand.AddCommand(BuildSendPmCommand(zuliprcOption));
        rootCommand.AddCommand(BuildSendStreamCommand(zuliprcOption));
        rootCommand.AddCommand(BuildEditMessageCommand(zuliprcOption));
        rootCommand.AddCommand(BuildDeleteMessageCommand(zuliprcOption));
        rootCommand.AddCommand(BuildGetMessageCommand(zuliprcOption));
        rootCommand.AddCommand(BuildGetMessagesCommand(zuliprcOption));
        rootCommand.AddCommand(BuildAddEmojiCommand(zuliprcOption));
        rootCommand.AddCommand(BuildRemoveEmojiCommand(zuliprcOption));
        rootCommand.AddCommand(BuildRenderMessageCommand(zuliprcOption));
        rootCommand.AddCommand(BuildUpdateFlagsCommand(zuliprcOption));
        rootCommand.AddCommand(BuildGetEditHistoryCommand(zuliprcOption));
        rootCommand.AddCommand(BuildMarkAllReadCommand(zuliprcOption));
        rootCommand.AddCommand(BuildMarkStreamReadCommand(zuliprcOption));
        rootCommand.AddCommand(BuildMarkTopicReadCommand(zuliprcOption));
        rootCommand.AddCommand(BuildGetReadReceiptsCommand(zuliprcOption));

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>Creates a ZulipClient from the resolved zuliprc path.</summary>
    /// <param name="zuliprcFilename">Explicit path or empty to search.</param>
    /// <returns>A configured ZulipClient.</returns>
    private static ZulipClient CreateClient(string zuliprcFilename)
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
        Option<string> messageOpt = new("--message", "Message content.") { IsRequired = true };
        Option<string> emailsOpt = new("--emails", "Comma-separated recipient email addresses.");
        Option<string> userIdsOpt = new("--user-ids", "Comma-separated recipient user IDs.");

        Command cmd = new("send-pm", "Send a direct (private) message.");
        cmd.AddOption(messageOpt);
        cmd.AddOption(emailsOpt);
        cmd.AddOption(userIdsOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            string message = ctx.ParseResult.GetValueForOption(messageOpt);
            string emails = ctx.ParseResult.GetValueForOption(emailsOpt);
            string userIds = ctx.ParseResult.GetValueForOption(userIdsOpt);

            ZulipClient client = CreateClient(zuliprc);

            (bool success, string details, ulong messageId) result;

            if (!string.IsNullOrEmpty(emails))
            {
                string[] emailArr = emails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                result = await client.Messages.TrySendPrivate(message, emailArr);
            }
            else if (!string.IsNullOrEmpty(userIds))
            {
                int[] idArr = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse).ToArray();
                result = await client.Messages.TrySendPrivate(message, idArr);
            }
            else
            {
                Console.Error.WriteLine("Error: --emails or --user-ids is required.");
                ctx.ExitCode = 1;
                return;
            }

            if (result.success)
            {
                Console.WriteLine($"Message sent. ID: {result.messageId}");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── send-stream ──────────────────────────────────────────────────────

    /// <summary>Builds the send-stream command.</summary>
    private static Command BuildSendStreamCommand(Option<string> zuliprcOption)
    {
        Option<string> messageOpt = new("--message", "Message content.") { IsRequired = true };
        Option<string> topicOpt = new("--topic", "Stream topic.") { IsRequired = true };
        Option<string> streamsOpt = new("--streams", "Comma-separated stream names.");
        Option<string> streamIdsOpt = new("--stream-ids", "Comma-separated stream IDs.");

        Command cmd = new("send-stream", "Send a message to a stream/channel.");
        cmd.AddOption(messageOpt);
        cmd.AddOption(topicOpt);
        cmd.AddOption(streamsOpt);
        cmd.AddOption(streamIdsOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            string message = ctx.ParseResult.GetValueForOption(messageOpt);
            string topic = ctx.ParseResult.GetValueForOption(topicOpt);
            string streams = ctx.ParseResult.GetValueForOption(streamsOpt);
            string streamIds = ctx.ParseResult.GetValueForOption(streamIdsOpt);

            ZulipClient client = CreateClient(zuliprc);

            (bool success, string details, ulong messageId) result;

            if (!string.IsNullOrEmpty(streams))
            {
                string[] streamArr = streams.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                result = await client.Messages.TrySendStream(message, topic, streamArr);
            }
            else if (!string.IsNullOrEmpty(streamIds))
            {
                int[] idArr = streamIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse).ToArray();
                result = await client.Messages.TrySendStream(message, topic, idArr);
            }
            else
            {
                Console.Error.WriteLine("Error: --streams or --stream-ids is required.");
                ctx.ExitCode = 1;
                return;
            }

            if (result.success)
            {
                Console.WriteLine($"Message sent. ID: {result.messageId}");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── edit-message ─────────────────────────────────────────────────────

    /// <summary>Builds the edit-message command.</summary>
    private static Command BuildEditMessageCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id", "ID of the message to edit.") { IsRequired = true };
        Option<string> contentOpt = new("--content", "New message content.");
        Option<string> topicOpt = new("--topic", "New topic.");
        Option<int?> moveToStreamOpt = new("--move-to-stream-id", "Stream ID to move the message to.");
        Option<string> propagateOpt = new("--propagate-mode", () => "one", "Propagate mode: one, later, or all.");

        Command cmd = new("edit-message", "Edit an existing message.");
        cmd.AddOption(messageIdOpt);
        cmd.AddOption(contentOpt);
        cmd.AddOption(topicOpt);
        cmd.AddOption(moveToStreamOpt);
        cmd.AddOption(propagateOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            ulong messageId = ctx.ParseResult.GetValueForOption(messageIdOpt);
            string content = ctx.ParseResult.GetValueForOption(contentOpt);
            string topic = ctx.ParseResult.GetValueForOption(topicOpt);
            int? moveToStream = ctx.ParseResult.GetValueForOption(moveToStreamOpt);
            string propagate = ctx.ParseResult.GetValueForOption(propagateOpt);

            Messages.EditPropagateMode mode = propagate?.ToLowerInvariant() switch
            {
                "later" => Messages.EditPropagateMode.Later,
                "all" => Messages.EditPropagateMode.All,
                _ => Messages.EditPropagateMode.One,
            };

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryEdit(messageId, content, topic, moveToStream, mode);

            if (result.success)
            {
                Console.WriteLine("Message edited successfully.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── delete-message ───────────────────────────────────────────────────

    /// <summary>Builds the delete-message command.</summary>
    private static Command BuildDeleteMessageCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id", "ID of the message to delete.") { IsRequired = true };

        Command cmd = new("delete-message", "Delete a message.");
        cmd.AddOption(messageIdOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            ulong messageId = ctx.ParseResult.GetValueForOption(messageIdOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryDelete(messageId);

            if (result.success)
            {
                Console.WriteLine("Message deleted.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── get-message ──────────────────────────────────────────────────────

    /// <summary>Builds the get-message command.</summary>
    private static Command BuildGetMessageCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id", "ID of the message to retrieve.") { IsRequired = true };
        Option<bool?> markdownOpt = new("--apply-markdown", "Whether to apply markdown rendering.");

        Command cmd = new("get-message", "Fetch a single message by ID.");
        cmd.AddOption(messageIdOpt);
        cmd.AddOption(markdownOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            ulong messageId = ctx.ParseResult.GetValueForOption(messageIdOpt);
            bool? markdown = ctx.ParseResult.GetValueForOption(markdownOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryGetSingle(messageId, markdown);

            if (result.success)
            {
                Console.WriteLine(JsonSerializer.Serialize(result.message, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── get-messages ─────────────────────────────────────────────────────

    /// <summary>Builds the get-messages command.</summary>
    private static Command BuildGetMessagesCommand(Option<string> zuliprcOption)
    {
        Option<string> anchorOpt = new("--anchor", () => "newest", "Anchor: newest, oldest, first_unread, or a message ID.");
        Option<int> numBeforeOpt = new("--num-before", () => 0, "Number of messages before the anchor.");
        Option<int> numAfterOpt = new("--num-after", () => 0, "Number of messages after the anchor.");
        Option<bool?> markdownOpt = new("--apply-markdown", "Whether to apply markdown rendering.");

        Command cmd = new("get-messages", "Fetch multiple messages.");
        cmd.AddOption(anchorOpt);
        cmd.AddOption(numBeforeOpt);
        cmd.AddOption(numAfterOpt);
        cmd.AddOption(markdownOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            string anchor = ctx.ParseResult.GetValueForOption(anchorOpt);
            int numBefore = ctx.ParseResult.GetValueForOption(numBeforeOpt);
            int numAfter = ctx.ParseResult.GetValueForOption(numAfterOpt);
            bool? markdown = ctx.ParseResult.GetValueForOption(markdownOpt);

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

            var result = await client.Messages.TryGet(anchorMode, anchorId, numBefore, numAfter, applyMarkdown: markdown);

            if (result.success)
            {
                Console.WriteLine(JsonSerializer.Serialize(result.messages, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── add-emoji ────────────────────────────────────────────────────────

    /// <summary>Builds the add-emoji command.</summary>
    private static Command BuildAddEmojiCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id", "ID of the message.") { IsRequired = true };
        Option<string> emojiNameOpt = new("--emoji-name", "Emoji name.") { IsRequired = true };
        Option<string> emojiCodeOpt = new("--emoji-code", "Emoji code.");
        Option<string> reactionTypeOpt = new("--reaction-type", "Reaction type.");

        Command cmd = new("add-emoji", "Add an emoji reaction to a message.");
        cmd.AddOption(messageIdOpt);
        cmd.AddOption(emojiNameOpt);
        cmd.AddOption(emojiCodeOpt);
        cmd.AddOption(reactionTypeOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            ulong messageId = ctx.ParseResult.GetValueForOption(messageIdOpt);
            string emojiName = ctx.ParseResult.GetValueForOption(emojiNameOpt);
            string emojiCode = ctx.ParseResult.GetValueForOption(emojiCodeOpt);
            string reactionType = ctx.ParseResult.GetValueForOption(reactionTypeOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryAddEmoji(messageId, emojiName, emojiCode, reactionType);

            if (result.success)
            {
                Console.WriteLine("Emoji added.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── remove-emoji ─────────────────────────────────────────────────────

    /// <summary>Builds the remove-emoji command.</summary>
    private static Command BuildRemoveEmojiCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id", "ID of the message.") { IsRequired = true };
        Option<string> emojiNameOpt = new("--emoji-name", "Emoji name.");
        Option<string> emojiCodeOpt = new("--emoji-code", "Emoji code.");
        Option<string> reactionTypeOpt = new("--reaction-type", "Reaction type.");

        Command cmd = new("remove-emoji", "Remove an emoji reaction from a message.");
        cmd.AddOption(messageIdOpt);
        cmd.AddOption(emojiNameOpt);
        cmd.AddOption(emojiCodeOpt);
        cmd.AddOption(reactionTypeOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            ulong messageId = ctx.ParseResult.GetValueForOption(messageIdOpt);
            string emojiName = ctx.ParseResult.GetValueForOption(emojiNameOpt);
            string emojiCode = ctx.ParseResult.GetValueForOption(emojiCodeOpt);
            string reactionType = ctx.ParseResult.GetValueForOption(reactionTypeOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryRemoveEmoji(messageId, emojiName, emojiCode, reactionType);

            if (result.success)
            {
                Console.WriteLine("Emoji removed.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── render-message ───────────────────────────────────────────────────

    /// <summary>Builds the render-message command.</summary>
    private static Command BuildRenderMessageCommand(Option<string> zuliprcOption)
    {
        Option<string> contentOpt = new("--content", "Message content to render.") { IsRequired = true };

        Command cmd = new("render-message", "Render message content to HTML.");
        cmd.AddOption(contentOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            string content = ctx.ParseResult.GetValueForOption(contentOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryRender(content);

            if (result.success)
            {
                Console.WriteLine(result.renderedHtml);
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── update-flags ─────────────────────────────────────────────────────

    /// <summary>Builds the update-flags command.</summary>
    private static Command BuildUpdateFlagsCommand(Option<string> zuliprcOption)
    {
        Option<string> messageIdsOpt = new("--message-ids", "Comma-separated message IDs.") { IsRequired = true };
        Option<string> opOpt = new("--op", "Operation: add or remove.") { IsRequired = true };
        Option<string> flagOpt = new("--flag", "Flag name (e.g., read, starred).") { IsRequired = true };

        Command cmd = new("update-flags", "Update personal message flags.");
        cmd.AddOption(messageIdsOpt);
        cmd.AddOption(opOpt);
        cmd.AddOption(flagOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            string messageIdsStr = ctx.ParseResult.GetValueForOption(messageIdsOpt);
            string op = ctx.ParseResult.GetValueForOption(opOpt);
            string flag = ctx.ParseResult.GetValueForOption(flagOpt);

            ulong[] messageIds = messageIdsStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ulong.Parse).ToArray();

            Messages.FlagOperation operation = op?.ToLowerInvariant() == "remove"
                ? Messages.FlagOperation.Remove
                : Messages.FlagOperation.Add;

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryUpdateFlags(messageIds, operation, flag);

            if (result.success)
            {
                Console.WriteLine("Flags updated.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── get-edit-history ─────────────────────────────────────────────────

    /// <summary>Builds the get-edit-history command.</summary>
    private static Command BuildGetEditHistoryCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id", "ID of the message.") { IsRequired = true };

        Command cmd = new("get-edit-history", "Get the edit history of a message.");
        cmd.AddOption(messageIdOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            ulong messageId = ctx.ParseResult.GetValueForOption(messageIdOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryGetEditHistory(messageId);

            if (result.success)
            {
                Console.WriteLine(JsonSerializer.Serialize(result.history, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── mark-all-read ────────────────────────────────────────────────────

    /// <summary>Builds the mark-all-read command.</summary>
    private static Command BuildMarkAllReadCommand(Option<string> zuliprcOption)
    {
        Command cmd = new("mark-all-read", "Mark all messages as read.");

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryMarkAllAsRead();

            if (result.success)
            {
                Console.WriteLine("All messages marked as read.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── mark-stream-read ─────────────────────────────────────────────────

    /// <summary>Builds the mark-stream-read command.</summary>
    private static Command BuildMarkStreamReadCommand(Option<string> zuliprcOption)
    {
        Option<int> streamIdOpt = new("--stream-id", "Stream ID.") { IsRequired = true };

        Command cmd = new("mark-stream-read", "Mark all messages in a stream as read.");
        cmd.AddOption(streamIdOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            int streamId = ctx.ParseResult.GetValueForOption(streamIdOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryMarkStreamAsRead(streamId);

            if (result.success)
            {
                Console.WriteLine("Stream marked as read.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── mark-topic-read ──────────────────────────────────────────────────

    /// <summary>Builds the mark-topic-read command.</summary>
    private static Command BuildMarkTopicReadCommand(Option<string> zuliprcOption)
    {
        Option<int> streamIdOpt = new("--stream-id", "Stream ID.") { IsRequired = true };
        Option<string> topicOpt = new("--topic", "Topic name.") { IsRequired = true };

        Command cmd = new("mark-topic-read", "Mark all messages in a topic as read.");
        cmd.AddOption(streamIdOpt);
        cmd.AddOption(topicOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            int streamId = ctx.ParseResult.GetValueForOption(streamIdOpt);
            string topic = ctx.ParseResult.GetValueForOption(topicOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryMarkTopicAsRead(streamId, topic);

            if (result.success)
            {
                Console.WriteLine("Topic marked as read.");
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
        });

        return cmd;
    }

    // ── get-read-receipts ────────────────────────────────────────────────

    /// <summary>Builds the get-read-receipts command.</summary>
    private static Command BuildGetReadReceiptsCommand(Option<string> zuliprcOption)
    {
        Option<ulong> messageIdOpt = new("--message-id", "ID of the message.") { IsRequired = true };

        Command cmd = new("get-read-receipts", "Get read receipts for a message.");
        cmd.AddOption(messageIdOpt);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            string zuliprc = ctx.ParseResult.GetValueForOption(zuliprcOption);
            ulong messageId = ctx.ParseResult.GetValueForOption(messageIdOpt);

            ZulipClient client = CreateClient(zuliprc);

            var result = await client.Messages.TryGetReadReceipts(messageId);

            if (result.success)
            {
                Console.WriteLine(JsonSerializer.Serialize(result.userIds, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Failed: {result.details}");
                ctx.ExitCode = 1;
            }
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
