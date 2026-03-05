using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Scheduled messages resource.</summary>
    public class ScheduledMessages
    {
        /// <summary>The API endpoint.</summary>
        private const string _endpoint = "api/v1/scheduled_messages";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the ScheduledMessages class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal ScheduledMessages(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all scheduled messages.</summary>
        /// <remarks>Feature level 181: scheduled-message listing endpoint became available.</remarks>
        /// <returns>An asynchronous result that yields (success, details, scheduledMessages).</returns>
        public async Task<(bool success, string details, List<ScheduledMessageObject> scheduledMessages)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.ScheduledMessages ?? new List<ScheduledMessageObject>());
            }

            return (false, "ScheduledMessages.GetAll failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all scheduled messages (throwing version).</summary>
        public async Task<List<ScheduledMessageObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.scheduledMessages;
        }

        /// <summary>Creates a scheduled message.</summary>
        /// <param name="type">The message type (direct or channel).</param>
        /// <param name="to">The recipient(s) as JSON.</param>
        /// <param name="content">The message content.</param>
        /// <param name="scheduledDeliveryTimestamp">Unix timestamp for delivery.</param>
        /// <param name="topic">(Optional) The topic (required for channel messages).</param>
        /// <remarks>
        /// Feature level 370: the special "(no topic)" value is interpreted as an empty topic name.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details, scheduledMessageId).</returns>
        public async Task<(bool success, string details, int scheduledMessageId)> TryCreate(
            string type,
            string to,
            string content,
            long scheduledDeliveryTimestamp,
            string topic = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "type", type },
                { "to", to },
                { "content", content },
                { "scheduled_delivery_timestamp", scheduledDeliveryTimestamp.ToString() }
            };

            if (topic != null) data.Add("topic", topic);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _endpoint, data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.ScheduledMessageId);
            }

            return (false, "ScheduledMessages.Create failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Creates a scheduled message (throwing version).</summary>
        public async Task<int> Create(string type, string to, string content, long scheduledDeliveryTimestamp, string topic = null)
        {
            var result = await TryCreate(type, to, content, scheduledDeliveryTimestamp, topic);
            if (!result.success) throw new Exception(result.details);
            return result.scheduledMessageId;
        }

        /// <summary>Edits a scheduled message.</summary>
        /// <param name="scheduledMessageId">The scheduled message ID.</param>
        /// <param name="content">(Optional) New content.</param>
        /// <param name="scheduledDeliveryTimestamp">(Optional) New delivery timestamp.</param>
        /// <param name="topic">(Optional) New topic.</param>
        /// <remarks>
        /// Feature level 370: updates using "(no topic)" map to an empty topic name.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryEdit(
            int scheduledMessageId,
            string content = null,
            long? scheduledDeliveryTimestamp = null,
            string topic = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (content != null) data.Add("content", content);
            if (scheduledDeliveryTimestamp != null) data.Add("scheduled_delivery_timestamp", scheduledDeliveryTimestamp.Value.ToString());
            if (topic != null) data.Add("topic", topic);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_endpoint}/{scheduledMessageId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "ScheduledMessages.Edit failed: " + response.GetFailureMessage());
        }

        /// <summary>Edits a scheduled message (throwing version).</summary>
        public async Task Edit(int scheduledMessageId, string content = null, long? scheduledDeliveryTimestamp = null, string topic = null)
        {
            var result = await TryEdit(scheduledMessageId, content, scheduledDeliveryTimestamp, topic);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes a scheduled message.</summary>
        /// <param name="scheduledMessageId">The scheduled message ID.</param>
        /// <remarks>Feature level 173: scheduled-message deletion endpoint was introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDelete(int scheduledMessageId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/{scheduledMessageId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "ScheduledMessages.Delete failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes a scheduled message (throwing version).</summary>
        public async Task Delete(int scheduledMessageId)
        {
            var result = await TryDelete(scheduledMessageId);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
