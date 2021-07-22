using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
            Private,
            Stream,
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
        };

        /// <summary>The post asynchronous.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>
        /// Initializes a new instance of the zulip_cs_lib.ZulipClientMessages class.
        /// </summary>
        /// <param name="postFunc">The HTTP POST function.</param>
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

        /// <summary>Edits.</summary>
        /// <param name="messageId">     Identifier for the message.</param>
        /// <param name="content">       (Optional) The content.</param>
        /// <param name="topic">         (Optional) The stream topic.</param>
        /// <param name="moveToStreamId">(Optional) Identifier for the stream to move the message to.</param>
        /// <param name="propagateMode"> (Optional) The propagate mode.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task Edit(
            ulong messageId,
            string content = null,
            string topic = null,
            int? moveToStreamId = null,
            EditPropagateMode propagateMode = EditPropagateMode.One)
        {
            (bool success, string details) result = await TryEdit(
                messageId,
                content,
                topic,
                moveToStreamId,
                propagateMode);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Move an existing message.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="messageId">     Identifier for the message.</param>
        /// <param name="content">       (Optional) The content.</param>
        /// <param name="topic">         (Optional) The stream topic.</param>
        /// <param name="moveToStreamId">(Optional) Identifier for the stream to move the message to.</param>
        /// <param name="propagateMode"> (Optional) The propagate mode.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        public async Task<(bool success, string details)> TryEdit(
            ulong messageId,
            string content = null,
            string topic = null,
            int? moveToStreamId = null,
            EditPropagateMode propagateMode = EditPropagateMode.One)
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

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_messageApiEndpoint}/{messageId}", data);

            Console.WriteLine($"Messages.Edit <<< {response.HttpResponseBody}");

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.Edit failed: " + response.GetFailureMessage());
        }

        /// <summary>Adds an emoji reaction to a message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="emojiName">(Optional) Name of the emoji.</param>
        /// <param name="emojiCode">(Optional) The emoji code.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task AddEmoji(
            ulong messageId,
            string emojiName = null,
            string emojiCode = null)
        {
            (bool success, string details) result = await TryAddEmoji(
                messageId,
                emojiName,
                emojiCode);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }

        /// <summary>Try to add an emoji reaction to a message.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="emojiName">(Optional) Name of the emoji.</param>
        /// <param name="emojiCode">(Optional) The emoji code.</param>
        /// <returns>An asynchronous result that yields a (bool success,string details)</returns>
        public async Task<(bool success, string details)> TryAddEmoji(
            ulong messageId, 
            string emojiName = null,
            string emojiCode = null)
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

            if (data.Count == 0)
            {
                return (false, $"Messages.AddEmoji: one of {nameof(emojiName)} or {nameof(emojiCode)} must be present!");
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_messageApiEndpoint}/{messageId}/reactions", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Messages.AddEmoji failed: " + response.GetFailureMessage());
        }

        /// <summary>Removes the emoji.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="emojiName">(Optional) Name of the emoji.</param>
        /// <param name="emojiCode">(Optional) The emoji code.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task RemoveEmoji(
            ulong messageId,
            string emojiName = null,
            string emojiCode = null)
        {
            (bool success, string details) result = await TryRemoveEmoji(
                messageId,
                emojiName,
                emojiCode);

            if (!result.success)
            {
                throw new Exception(result.details);
            }
        }


        /// <summary>Try to remove an emoji reaction from a message.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="messageId">Identifier for the message.</param>
        /// <param name="emojiName">(Optional) Name of the emoji.</param>
        /// <param name="emojiCode">(Optional) The emoji code.</param>
        /// <returns>An asynchronous result that yields a (bool success,string details)</returns>
        public async Task<(bool success, string details)> TryRemoveEmoji(
            ulong messageId,
            string emojiName = null,
            string emojiCode = null)
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
        /// <param name="message">   The message.</param>
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
        /// <param name="message">   The message.</param>
        /// <param name="userEmails">A variable-length parameters list containing user email addresses.</param>
        /// <returns>
        /// An asynchronous result that yields a (bool success,string details,ulong messageId)
        /// </returns>
        public async Task<(bool success, string details, ulong messageId)> TrySendPrivate(
            string message, 
            params string[] userEmails)
        {
            ZulipResponse response = await Send(message, null, ZulipMessageType.Private, userEmails);

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
        /// <param name="userIds">  A variable-length parameters list containing user ids.</param>
        public async Task<(bool success, string details, ulong messageId)> TrySendPrivate(string message, params int[] userIds)
        {
            ZulipResponse response = await Send(message, null, ZulipMessageType.Private, userIds);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, (ulong)response.Id);
            }

            return (false, "Messages.SendPrivate failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Sends a stream message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="message">    The message.</param>
        /// <param name="topic">      The stream topic.</param>
        /// <param name="streamNames">A variable-length parameters list containing destination stream names.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
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
        /// <param name="message">    The message.</param>
        /// <param name="topic">      The stream topic.</param>
        /// <param name="streamNames">A variable-length parameters list containing destination stream names.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        public async Task<(bool success, string details, ulong messageId)> TrySendStream(
            string message, 
            string topic, 
            params string[] streamNames)
        {
            ZulipResponse response = await Send(message, topic, ZulipMessageType.Stream, streamNames);

            Console.WriteLine($"Messages.SendStream <<< {response.HttpResponseBody}");

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, (ulong)response.Id);
            }

            return (false, "Messages.SendStream failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Sends a stream message.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="message">  The message.</param>
        /// <param name="topic">    The stream topic.</param>
        /// <param name="streamIds">A variable-length parameters list containing stream ids.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
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
        /// <param name="message">  The message.</param>
        /// <param name="topic">    The stream topic.</param>
        /// <param name="streamIds">A variable-length parameters list containing stream ids.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        public async Task<(bool success, string details, ulong messageId)> TrySendStream(string message, string topic, params int[] streamIds)
        {
            ZulipResponse response = await Send(message, topic, ZulipMessageType.Stream, streamIds);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, (ulong)response.Id);
            }

            return (false, "Messages.SendStream failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Sends a message.</summary>
        /// <param name="message">  The message.</param>
        /// <param name="topic">    The topic, if sending a stream message.</param>
        /// <param name="type">     The message type (private, stream).</param>
        /// <param name="stringIds">A variable-length parameters list containing user email addresses or
        ///  stream names.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        private Task<ZulipResponse> Send(string message, string topic, ZulipMessageType type, params string[] stringIds)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            string recipients = string.Join(", ", stringIds);

            switch (type)
            {
                case ZulipMessageType.Private:
                    data.Add("type", "private");
                    break;
                case ZulipMessageType.Stream:
                    data.Add("type", "stream");
                    data.Add("topic", topic);
                    break;
            }

            data.Add("to", recipients);
            data.Add("content", message);

            return _doZulipRequest(HttpMethod.Post, _messageApiEndpoint, data);
        }

        /// <summary>Sends a message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="topic">  The topic, if sending a stream message.</param>
        /// <param name="type">   The message type (private, stream).</param>
        /// <param name="intIds"> A variable-length parameters list containing user or stream ids.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        private async Task<ZulipResponse> Send(string message, string topic, ZulipMessageType type, params int[] intIds)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            string recipients = "[" + string.Join(", ", intIds) + "]";

            switch (type)
            {
                case ZulipMessageType.Private:
                    data.Add("type", "private");
                    break;
                case ZulipMessageType.Stream:
                    data.Add("type", "stream");
                    data.Add("topic", topic);
                    break;
            }

            data.Add("to", recipients);
            data.Add("content", message);

            return await _doZulipRequest(HttpMethod.Post, _messageApiEndpoint, data);
        }
    }
}
