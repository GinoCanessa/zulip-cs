using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>A zulip client messages.</summary>
    public class Messages
    {
        /// <summary>The message API endpoint.</summary>
        private const string _messageApiEndpoint = "api/v1/messages";

        /// <summary>Values that represent zulip message types.</summary>
        private enum ZulipMessageType
        {
            Direct,
            Channel,
        }

        /// <summary>Values that represent edit propagate modes.</summary>
        public enum EditPropagateMode
        {
            /// <summary>Change only this message.</summary>
            One,

            /// <summary>Change this message and all subsequent ones.</summary>
            Later,

            /// <summary>Change all messages in this topic.</summary>
            All
        }

        /// <summary>Values that represent get anchor modes.</summary>
        public enum GetAnchorMode
        {
            /// <summary>The most recent message.</summary>
            Newest,

            /// <summary>The oldest message.</summary>
            Oldest,

            /// <summary>The oldest unread message matching the query, if any; otherwise, the most recent message.</summary>
            FirstUnread,

            /// <summary>Anchor to a specific message (by id).</summary>
            Id,
        }

        /// <summary>Values that represent flag operations.</summary>
        public enum FlagOperation
        {
            /// <summary>Add the flag.</summary>
            Add,

            /// <summary>Remove the flag.</summary>
            Remove,
        }

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>
        /// Initializes a new instance of the zulip_cs_lib.Resources.Messages class.
        /// </summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Messages(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Deletes the given messageId.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="messageId">Identifier for the message.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task Delete(ulong messageId)
        {
            (bool success, string details) result = await TryDelete(messageId);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Try delete.</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <returns>An asynchronous result that yields true if it succeeds, false if it fails.</returns>
        public async Task<(bool success, string details)> TryDelete(ulong messageId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_messageApiEndpoint}/{messageId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.Delete failed: " + response.GetFailureMessage());
        }

        /// <summary>Edits a message.</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="content">(Optional) The content.</param>
        /// <param name="topic">(Optional) The stream topic.</param>
        /// <param name="moveToStreamId">(Optional) Identifier for the stream to move the message to.</param>
        /// <param name="propagateMode">(Optional) The propagate mode.</param>
        /// <param name="sendNotificationToOldThread">(Optional) Whether to send notification to old thread.</param>
        /// <param name="sendNotificationToNewThread">(Optional) Whether to send notification to new thread.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task Edit(
            ulong messageId,
            string content = null,
            string topic = null,
            int? moveToStreamId = null,
            EditPropagateMode propagateMode = EditPropagateMode.One,
            bool? sendNotificationToOldThread = null,
            bool? sendNotificationToNewThread = null)
        {
            (bool success, string details) result = await TryEdit(
                messageId,
                content,
                topic,
                moveToStreamId,
                propagateMode,
                sendNotificationToOldThread,
                sendNotificationToNewThread);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Try to edit an existing message.</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="content">(Optional) The content.</param>
        /// <param name="topic">(Optional) The stream topic.</param>
        /// <param name="moveToStreamId">(Optional) Identifier for the stream to move the message to.</param>
        /// <param name="propagateMode">(Optional) The propagate mode.</param>
        /// <param name="sendNotificationToOldThread">(Optional) Whether to send notification to old thread.</param>
        /// <param name="sendNotificationToNewThread">(Optional) Whether to send notification to new thread.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryEdit(
            ulong messageId,
            string content = null,
            string topic = null,
            int? moveToStreamId = null,
            EditPropagateMode propagateMode = EditPropagateMode.One,
            bool? sendNotificationToOldThread = null,
            bool? sendNotificationToNewThread = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (content != null)
            {
                data.Add("content", content);
            }

            if (topic != null)
            {
                data.Add("topic", topic);
            }

            if (moveToStreamId != null)
            {
                data.Add("stream_id", moveToStreamId.ToString());
            }

            if (data.Count == 0)
            {
                return (false, $"Messages.Edit: one of {nameof(content)}, {nameof(topic)}, and {nameof(moveToStreamId)} must be present!");
            }

            switch (propagateMode)
            {
                case EditPropagateMode.One:
                    data.Add("propagate_mode", "change_one");
                    break;
                case EditPropagateMode.Later:
                    data.Add("propagate_mode", "change_later");
                    break;
                case EditPropagateMode.All:
                    data.Add("propagate_mode", "change_all");
                    break;
                default:
                    break;
            }

            if (sendNotificationToOldThread != null)
            {
                data.Add("send_notification_to_old_thread", sendNotificationToOldThread.Value.ToString().ToLowerInvariant());
            }

            if (sendNotificationToNewThread != null)
            {
                data.Add("send_notification_to_new_thread", sendNotificationToNewThread.Value.ToString().ToLowerInvariant());
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_messageApiEndpoint}/{messageId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.Edit failed: " + response.GetFailureMessage());
        }

        /// <summary>Adds an emoji reaction to a message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="emojiName">Name of the emoji (required).</param>
        /// <param name="emojiCode">(Optional) The emoji code.</param>
        /// <param name="reactionType">(Optional) The reaction type.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task AddEmoji(
            ulong messageId,
            string emojiName,
            string emojiCode = null,
            string reactionType = null)
        {
            (bool success, string details) result = await TryAddEmoji(
                messageId,
                emojiName,
                emojiCode,
                reactionType);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Try to add an emoji reaction to a message.</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="emojiName">Name of the emoji (required).</param>
        /// <param name="emojiCode">(Optional) The emoji code.</param>
        /// <param name="reactionType">(Optional) The reaction type.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryAddEmoji(
            ulong messageId,
            string emojiName,
            string emojiCode = null,
            string reactionType = null)
        {
            if (string.IsNullOrEmpty(emojiName))
            {
                return (false, "Messages.AddEmoji: emojiName is required!");
            }

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "emoji_name", emojiName }
            };

            if (emojiCode != null)
            {
                data.Add("emoji_code", emojiCode);
            }

            if (reactionType != null)
            {
                data.Add("reaction_type", reactionType);
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_messageApiEndpoint}/{messageId}/reactions", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.AddEmoji failed: " + response.GetFailureMessage());
        }

        /// <summary>Removes an emoji reaction from a message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="emojiName">(Optional) Name of the emoji.</param>
        /// <param name="emojiCode">(Optional) The emoji code.</param>
        /// <param name="reactionType">(Optional) The reaction type.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task RemoveEmoji(
            ulong messageId,
            string emojiName = null,
            string emojiCode = null,
            string reactionType = null)
        {
            (bool success, string details) result = await TryRemoveEmoji(
                messageId,
                emojiName,
                emojiCode,
                reactionType);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Try to remove an emoji reaction from a message.</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="emojiName">(Optional) Name of the emoji.</param>
        /// <param name="emojiCode">(Optional) The emoji code.</param>
        /// <param name="reactionType">(Optional) The reaction type.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryRemoveEmoji(
            ulong messageId,
            string emojiName = null,
            string emojiCode = null,
            string reactionType = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (emojiName != null)
            {
                data.Add("emoji_name", emojiName);
            }

            if (emojiCode != null)
            {
                data.Add("emoji_code", emojiCode);
            }

            if (reactionType != null)
            {
                data.Add("reaction_type", reactionType);
            }

            if (data.Count == 0)
            {
                return (false, $"Messages.RemoveEmoji: one of {nameof(emojiName)} or {nameof(emojiCode)} must be present!");
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_messageApiEndpoint}/{messageId}/reactions", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.RemoveEmoji failed: " + response.GetFailureMessage());
        }

        /// <summary>Sends a private message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="message">The message.</param>
        /// <param name="userEmails">A variable-length parameters list containing user email addresses.</param>
        /// <returns>The posted message id.</returns>
        public async Task<ulong> SendPrivate(string message, params string[] userEmails)
        {
            (bool success, string details, ulong messageId) result = await TrySendPrivate(message, userEmails);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.messageId;
        }

        /// <summary>Sends a private message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="userEmails">A variable-length parameters list containing user email addresses.</param>
        /// <returns>An asynchronous result that yields (success, details, messageId).</returns>
        public async Task<(bool success, string details, ulong messageId)> TrySendPrivate(
            string message,
            params string[] userEmails)
        {
            ZulipResponse response = await Send(message, null, ZulipMessageType.Direct, userEmails);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, (ulong)response.Id);
            }

            return (false, "Messages.SendPrivate failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Sends a private message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="message">The message.</param>
        /// <param name="userIds">A variable-length parameters list containing user ids.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task<ulong> SendPrivate(string message, params int[] userIds)
        {
            (bool success, string details, ulong messageId) result = await TrySendPrivate(message, userIds);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.messageId;
        }

        /// <summary>Sends a private message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="userIds">A variable-length parameters list containing user ids.</param>
        /// <returns>An asynchronous result that yields (success, details, messageId).</returns>
        public async Task<(bool success, string details, ulong messageId)> TrySendPrivate(string message, params int[] userIds)
        {
            ZulipResponse response = await Send(message, null, ZulipMessageType.Direct, userIds);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, (ulong)response.Id);
            }

            return (false, "Messages.SendPrivate failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Sends a stream message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="message">The message.</param>
        /// <param name="topic">The stream topic.</param>
        /// <param name="streamNames">A variable-length parameters list containing destination stream names.</param>
        /// <returns>An asynchronous result that yields the message ID.</returns>
        public async Task<ulong> SendStream(string message, string topic, params string[] streamNames)
        {
            (bool success, string details, ulong messageId) result = await TrySendStream(message, topic, streamNames);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.messageId;
        }

        /// <summary>Sends a stream message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="topic">The stream topic.</param>
        /// <param name="streamNames">A variable-length parameters list containing destination stream names.</param>
        /// <returns>An asynchronous result that yields (success, details, messageId).</returns>
        public async Task<(bool success, string details, ulong messageId)> TrySendStream(
            string message,
            string topic,
            params string[] streamNames)
        {
            ZulipResponse response = await Send(message, topic, ZulipMessageType.Channel, streamNames);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, (ulong)response.Id);
            }

            return (false, "Messages.SendStream failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Sends a stream message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="message">The message.</param>
        /// <param name="topic">The stream topic.</param>
        /// <param name="streamIds">A variable-length parameters list containing stream ids.</param>
        /// <returns>An asynchronous result that yields the message ID.</returns>
        public async Task<ulong> SendStream(string message, string topic, params int[] streamIds)
        {
            (bool success, string details, ulong messageId) result = await TrySendStream(message, topic, streamIds);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.messageId;
        }

        /// <summary>Sends a stream message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="topic">The stream topic.</param>
        /// <param name="streamIds">A variable-length parameters list containing stream ids.</param>
        /// <returns>An asynchronous result that yields (success, details, messageId).</returns>
        public async Task<(bool success, string details, ulong messageId)> TrySendStream(string message, string topic, params int[] streamIds)
        {
            ZulipResponse response = await Send(message, topic, ZulipMessageType.Channel, streamIds);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, (ulong)response.Id);
            }

            return (false, "Messages.SendStream failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Gets messages matching the given criteria.</summary>
        /// <param name="anchorMode">The anchor mode.</param>
        /// <param name="anchorMessageId">(Optional) The anchor message ID (when anchorMode is Id).</param>
        /// <param name="numBefore">The number of messages before the anchor.</param>
        /// <param name="numAfter">The number of messages after the anchor.</param>
        /// <param name="narrow">(Optional) Narrow filters.</param>
        /// <param name="clientGravatar">(Optional) Whether to use client gravatar.</param>
        /// <param name="applyMarkdown">(Optional) Whether to apply markdown.</param>
        /// <param name="includeAnchor">(Optional) Whether to include the anchor message.</param>
        /// <returns>An asynchronous result that yields (success, details, messages, foundNewest, foundOldest).</returns>
        public async Task<(bool success, string details, List<MessageObject> messages, bool? foundNewest, bool? foundOldest)> TryGet(
            GetAnchorMode anchorMode,
            ulong? anchorMessageId = null,
            int numBefore = 0,
            int numAfter = 0,
            Narrow[] narrow = null,
            bool? clientGravatar = null,
            bool? applyMarkdown = null,
            bool? includeAnchor = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            switch (anchorMode)
            {
                case GetAnchorMode.Newest:
                    data.Add("anchor", "newest");
                    break;
                case GetAnchorMode.Oldest:
                    data.Add("anchor", "oldest");
                    break;
                case GetAnchorMode.FirstUnread:
                    data.Add("anchor", "first_unread");
                    break;
                case GetAnchorMode.Id:
                    if (anchorMessageId == null)
                    {
                        return (false, "Messages.Get: anchorMessageId is required when anchorMode is Id!", null, null, null);
                    }
                    data.Add("anchor", anchorMessageId.Value.ToString());
                    break;
            }

            data.Add("num_before", numBefore.ToString());
            data.Add("num_after", numAfter.ToString());

            if (narrow != null && narrow.Length > 0)
            {
                data.Add("narrow", Narrow.ToJsonArray(narrow));
            }

            if (clientGravatar != null)
            {
                data.Add("client_gravatar", clientGravatar.Value.ToString().ToLowerInvariant());
            }

            if (applyMarkdown != null)
            {
                data.Add("apply_markdown", applyMarkdown.Value.ToString().ToLowerInvariant());
            }

            if (includeAnchor != null)
            {
                data.Add("include_anchor", includeAnchor.Value.ToString().ToLowerInvariant());
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _messageApiEndpoint, data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Messages ?? new List<MessageObject>(), response.FoundNewest, response.FoundOldest);
            }

            return (false, "Messages.Get failed: " + response.GetFailureMessage(), null, null, null);
        }

        /// <summary>Gets messages (throwing version).</summary>
        /// <param name="anchorMode">The anchor mode.</param>
        /// <param name="anchorMessageId">(Optional) The anchor message ID.</param>
        /// <param name="numBefore">The number of messages before the anchor.</param>
        /// <param name="numAfter">The number of messages after the anchor.</param>
        /// <param name="narrow">(Optional) Narrow filters.</param>
        /// <param name="clientGravatar">(Optional) Whether to use client gravatar.</param>
        /// <param name="applyMarkdown">(Optional) Whether to apply markdown.</param>
        /// <param name="includeAnchor">(Optional) Whether to include the anchor message.</param>
        /// <returns>An asynchronous result that yields the messages list.</returns>
        public async Task<List<MessageObject>> Get(
            GetAnchorMode anchorMode,
            ulong? anchorMessageId = null,
            int numBefore = 0,
            int numAfter = 0,
            Narrow[] narrow = null,
            bool? clientGravatar = null,
            bool? applyMarkdown = null,
            bool? includeAnchor = null)
        {
            var result = await TryGet(anchorMode, anchorMessageId, numBefore, numAfter, narrow, clientGravatar, applyMarkdown, includeAnchor);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.messages;
        }

        /// <summary>Fetches a single message by ID.</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="applyMarkdown">(Optional) Whether to apply markdown.</param>
        /// <returns>An asynchronous result that yields (success, details, message, rawContent).</returns>
        public async Task<(bool success, string details, MessageObject message, string rawContent)> TryGetSingle(
            ulong messageId,
            bool? applyMarkdown = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (applyMarkdown != null)
            {
                data.Add("apply_markdown", applyMarkdown.Value.ToString().ToLowerInvariant());
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_messageApiEndpoint}/{messageId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.MessageObject, response.RawContent);
            }

            return (false, "Messages.GetSingle failed: " + response.GetFailureMessage(), null, null);
        }

        /// <summary>Fetches a single message by ID (throwing version).</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="applyMarkdown">(Optional) Whether to apply markdown.</param>
        /// <returns>An asynchronous result that yields the message.</returns>
        public async Task<MessageObject> GetSingle(
            ulong messageId,
            bool? applyMarkdown = null)
        {
            var result = await TryGetSingle(messageId, applyMarkdown);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.message;
        }

        /// <summary>Renders a message to HTML.</summary>
        /// <param name="content">The message content to render.</param>
        /// <returns>An asynchronous result that yields (success, details, renderedHtml).</returns>
        public async Task<(bool success, string details, string renderedHtml)> TryRender(string content)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "content", content }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_messageApiEndpoint}/render", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.RenderedMessage);
            }

            return (false, "Messages.Render failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Renders a message to HTML (throwing version).</summary>
        /// <param name="content">The message content to render.</param>
        /// <returns>An asynchronous result that yields the rendered HTML.</returns>
        public async Task<string> Render(string content)
        {
            var result = await TryRender(content);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.renderedHtml;
        }

        /// <summary>Updates personal message flags.</summary>
        /// <param name="messageIds">The message IDs.</param>
        /// <param name="operation">The flag operation (add or remove).</param>
        /// <param name="flag">The flag name (e.g., read, starred).</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryUpdateFlags(
            ulong[] messageIds,
            FlagOperation operation,
            string flag)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "messages", JsonSerializer.Serialize(messageIds) },
                { "op", operation == FlagOperation.Add ? "add" : "remove" },
                { "flag", flag }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_messageApiEndpoint}/flags", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.UpdateFlags failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates personal message flags (throwing version).</summary>
        /// <param name="messageIds">The message IDs.</param>
        /// <param name="operation">The flag operation.</param>
        /// <param name="flag">The flag name.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task UpdateFlags(
            ulong[] messageIds,
            FlagOperation operation,
            string flag)
        {
            var result = await TryUpdateFlags(messageIds, operation, flag);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Gets the edit history of a message.</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <returns>An asynchronous result that yields (success, details, history).</returns>
        public async Task<(bool success, string details, List<MessageHistoryObject> history)> TryGetEditHistory(ulong messageId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_messageApiEndpoint}/{messageId}/history", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.MessageHistory ?? new List<MessageHistoryObject>());
            }

            return (false, "Messages.GetEditHistory failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets the edit history of a message (throwing version).</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <returns>An asynchronous result that yields the history list.</returns>
        public async Task<List<MessageHistoryObject>> GetEditHistory(ulong messageId)
        {
            var result = await TryGetEditHistory(messageId);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.history;
        }

        /// <summary>Marks all messages as read.</summary>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryMarkAllAsRead()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/mark_all_as_read", new Dictionary<string, string>());

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.MarkAllAsRead failed: " + response.GetFailureMessage());
        }

        /// <summary>Marks all messages as read (throwing version).</summary>
        /// <returns>An asynchronous result.</returns>
        public async Task MarkAllAsRead()
        {
            var result = await TryMarkAllAsRead();

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Marks all messages in a stream as read.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryMarkStreamAsRead(int streamId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream_id", streamId.ToString() }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/mark_stream_as_read", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.MarkStreamAsRead failed: " + response.GetFailureMessage());
        }

        /// <summary>Marks all messages in a stream as read (throwing version).</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task MarkStreamAsRead(int streamId)
        {
            var result = await TryMarkStreamAsRead(streamId);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Marks all messages in a topic as read.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <param name="topicName">The topic name.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryMarkTopicAsRead(int streamId, string topicName)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream_id", streamId.ToString() },
                { "topic_name", topicName }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/mark_topic_as_read", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.MarkTopicAsRead failed: " + response.GetFailureMessage());
        }

        /// <summary>Marks all messages in a topic as read (throwing version).</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <param name="topicName">The topic name.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task MarkTopicAsRead(int streamId, string topicName)
        {
            var result = await TryMarkTopicAsRead(streamId, topicName);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Gets read receipts for a message.</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <returns>An asynchronous result that yields (success, details, userIds).</returns>
        public async Task<(bool success, string details, List<int> userIds)> TryGetReadReceipts(ulong messageId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_messageApiEndpoint}/{messageId}/read_receipts", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.UserIds ?? new List<int>());
            }

            return (false, "Messages.GetReadReceipts failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets read receipts for a message (throwing version).</summary>
        /// <param name="messageId">Identifier for the message.</param>
        /// <returns>An asynchronous result that yields user ID list.</returns>
        public async Task<List<int>> GetReadReceipts(ulong messageId)
        {
            var result = await TryGetReadReceipts(messageId);

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return result.userIds;
        }

        /// <summary>Sends a message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="topic">The topic, if sending a stream message.</param>
        /// <param name="type">The message type.</param>
        /// <param name="stringIds">User email addresses or stream names.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        private Task<ZulipResponse> Send(string message, string topic, ZulipMessageType type, params string[] stringIds)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            string recipients = string.Join(", ", stringIds);

            switch (type)
            {
                case ZulipMessageType.Direct:
                    data.Add("type", "direct");
                    break;
                case ZulipMessageType.Channel:
                    data.Add("type", "channel");
                    data.Add("topic", topic);
                    break;
            }

            data.Add("to", recipients);
            data.Add("content", message);

            return _doZulipRequest(HttpMethod.Post, _messageApiEndpoint, data);
        }

        /// <summary>Sends a message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="topic">The topic, if sending a stream message.</param>
        /// <param name="type">The message type.</param>
        /// <param name="intIds">User or stream ids.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        private async Task<ZulipResponse> Send(string message, string topic, ZulipMessageType type, params int[] intIds)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            string recipients = "[" + string.Join(", ", intIds) + "]";

            switch (type)
            {
                case ZulipMessageType.Direct:
                    data.Add("type", "direct");
                    break;
                case ZulipMessageType.Channel:
                    data.Add("type", "channel");
                    data.Add("topic", topic);
                    break;
            }

            data.Add("to", recipients);
            data.Add("content", message);

            return await _doZulipRequest(HttpMethod.Post, _messageApiEndpoint, data);
        }
    }
}
