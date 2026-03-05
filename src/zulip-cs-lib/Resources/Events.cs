using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Events (real-time) resource.</summary>
    public class Events
    {
        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Events class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Events(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Registers an event queue.</summary>
        /// <param name="eventTypes">(Optional) JSON array of event type strings.</param>
        /// <param name="narrow">(Optional) Narrow filter as JSON.</param>
        /// <param name="allPublicStreams">(Optional) Include all public streams.</param>
        /// <returns>An asynchronous result that yields (success, details, queueId, lastEventId).</returns>
        public async Task<(bool success, string details, string queueId, int lastEventId)> TryRegisterQueue(
            string eventTypes = null,
            string narrow = null,
            bool? allPublicStreams = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (eventTypes != null) data.Add("event_types", eventTypes);
            if (narrow != null) data.Add("narrow", narrow);
            if (allPublicStreams != null) data.Add("all_public_streams", allPublicStreams.Value.ToString().ToLowerInvariant());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/register", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.QueueId, response.LastEventId);
            }

            return (false, "Events.RegisterQueue failed: " + response.GetFailureMessage(), null, -1);
        }

        /// <summary>Registers an event queue (throwing version).</summary>
        public async Task<(string queueId, int lastEventId)> RegisterQueue(
            string eventTypes = null, string narrow = null, bool? allPublicStreams = null)
        {
            var result = await TryRegisterQueue(eventTypes, narrow, allPublicStreams);
            if (!result.success) throw new Exception(result.details);
            return (result.queueId, result.lastEventId);
        }

        /// <summary>Gets events from a queue.</summary>
        /// <param name="queueId">The queue ID.</param>
        /// <param name="lastEventId">The last event ID received.</param>
        /// <param name="dontBlock">(Optional) Whether to return immediately.</param>
        /// <returns>An asynchronous result that yields (success, details, events).</returns>
        public async Task<(bool success, string details, List<EventObject> events)> TryGetEvents(
            string queueId,
            int lastEventId,
            bool? dontBlock = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "queue_id", queueId },
                { "last_event_id", lastEventId.ToString() }
            };

            if (dontBlock != null) data.Add("dont_block", dontBlock.Value.ToString().ToLowerInvariant());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/events", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Events ?? new List<EventObject>());
            }

            return (false, "Events.GetEvents failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets events from a queue (throwing version).</summary>
        public async Task<List<EventObject>> GetEvents(string queueId, int lastEventId, bool? dontBlock = null)
        {
            var result = await TryGetEvents(queueId, lastEventId, dontBlock);
            if (!result.success) throw new Exception(result.details);
            return result.events;
        }

        /// <summary>Deletes an event queue.</summary>
        /// <param name="queueId">The queue ID to delete.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDeleteQueue(string queueId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "queue_id", queueId }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, "api/v1/events", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Events.DeleteQueue failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes an event queue (throwing version).</summary>
        public async Task DeleteQueue(string queueId)
        {
            var result = await TryDeleteQueue(queueId);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
