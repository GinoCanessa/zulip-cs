using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Events (real-time) resource.</summary>
    public class Events
    {
        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Events class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Events(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Registers an event queue and retains its complete initial state and diagnostics.</summary>
        /// <param name="options">Optional filters, capabilities and queue inactivity settings.</param>
        /// <returns>Success, failure details, and the request-local detailed result.</returns>
        public Task<(bool success, string? details, RegistrationResult? registration)> TryRegister(
            RegistrationOptions? options = null)
        {
            Dictionary<string, string> data = [];
            if (options != null)
            {
                if (options.IncludeSubscribers != null && options.IncludeSubscribers is not "true" and not "false" and not "partial")
                {
                    return Task.FromResult<(bool, string?, RegistrationResult?)>(
                        (false, "Events.Register failed: include_subscribers must be true, false or partial.", null));
                }

                if (options.IdleQueueTimeout is JsonElement timeout)
                {
                    if (!(timeout.ValueKind == JsonValueKind.Number && timeout.TryGetInt32(out int seconds) && seconds >= 1) &&
                        !(timeout.ValueKind == JsonValueKind.String && timeout.GetString() == "mobile"))
                    {
                        return Task.FromResult<(bool, string?, RegistrationResult?)>(
                            (false, "Events.Register failed: idle_queue_timeout must be an integer of at least 1 or the JSON string \"mobile\".", null));
                    }

                    data.Add("idle_queue_timeout", timeout.GetRawText());
                }

                if (options.EventTypes != null) data.Add("event_types", JsonSerializer.Serialize(options.EventTypes));
                if (options.FetchEventTypes != null) data.Add("fetch_event_types", JsonSerializer.Serialize(options.FetchEventTypes));
                if (options.ClientCapabilities != null) data.Add("client_capabilities", JsonSerializer.Serialize(options.ClientCapabilities));
                if (options.Narrow != null) data.Add("narrow", options.Narrow);
                if (options.IncludeSubscribers != null) data.Add("include_subscribers", options.IncludeSubscribers);
                if (options.AllPublicStreams.HasValue) data.Add("all_public_streams", JsonSerializer.Serialize(options.AllPublicStreams.Value));
                if (options.ClientGravatar.HasValue) data.Add("client_gravatar", JsonSerializer.Serialize(options.ClientGravatar.Value));
                if (options.SlimPresence.HasValue) data.Add("slim_presence", JsonSerializer.Serialize(options.SlimPresence.Value));
                if (options.ApplyMarkdown.HasValue) data.Add("apply_markdown", JsonSerializer.Serialize(options.ApplyMarkdown.Value));
            }

            return TryRegisterCore(data);
        }

        /// <summary>Registers an event queue and returns its complete initial state (throwing version).</summary>
        /// <param name="options">Optional filters, capabilities and queue inactivity settings.</param>
        /// <returns>The request-local detailed result.</returns>
        public async Task<RegistrationResult> Register(RegistrationOptions? options = null)
        {
            (bool success, string? details, RegistrationResult? registration) = await TryRegister(options);
            if (!success) throw new Exception(details);
            return registration!;
        }

        /// <summary>Executes registration and validates its required queue and cursor, including injected delegates.</summary>
        /// <param name="data">The form parameters.</param>
        /// <returns>The detailed registration outcome.</returns>
        private async Task<(bool success, string? details, RegistrationResult? registration)> TryRegisterCore(
            Dictionary<string, string> data)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/register", data);
            if (!ResponsePayload.TryRead(response, "Events.Register", null, JsonValueKind.Object,
                out JsonElement payload, out string? details, ValidateRegistration))
            {
                return (false, details, null);
            }

            try
            {
                RegistrationResult registration = new RegistrationResult(payload, response);
                return (true, null, registration);
            }
            catch (JsonException exception)
            {
                return (false, "Events.Register failed: Invalid response payload: " + exception.Message, null);
            }
        }

        /// <summary>Requires a usable queue ID and an integer cursor, without rejecting -1.</summary>
        /// <param name="payload">The registration envelope.</param>
        /// <returns>A diagnostic, or null.</returns>
        private static string? ValidateRegistration(JsonElement payload)
        {
            if (!payload.TryGetProperty("queue_id", out JsonElement queue) ||
                queue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(queue.GetString()))
            {
                return "required field 'queue_id' must be a nonempty String.";
            }

            if (!payload.TryGetProperty("last_event_id", out JsonElement cursor) ||
                cursor.ValueKind != JsonValueKind.Number || !cursor.TryGetInt64(out _))
            {
                return "required field 'last_event_id' must be an integer.";
            }

            return null;
        }

        /// <summary>Registers an event queue.</summary>
        /// <param name="eventTypes">(Optional) JSON array of event type strings.</param>
        /// <param name="narrow">(Optional) Narrow filter as JSON.</param>
        /// <param name="allPublicStreams">(Optional) Include all public streams.</param>
        /// <remarks>Projects the detailed Register result. Use Register to retain initial state and request-local warnings.</remarks>
        /// <returns>An asynchronous result that yields (success, details, queueId, lastEventId).</returns>
        public async Task<(bool success, string? details, string? queueId, int lastEventId)> TryRegisterQueue(
            string? eventTypes = null,
            string? narrow = null,
            bool? allPublicStreams = null)
        {
            Dictionary<string, string> data = [];

            if (eventTypes != null) data.Add("event_types", eventTypes);
            if (narrow != null) data.Add("narrow", narrow);
            if (allPublicStreams != null) data.Add("all_public_streams", allPublicStreams.Value.ToString().ToLowerInvariant());

            (bool success, string? details, RegistrationResult? registration) = await TryRegisterCore(data);
            if (!success)
            {
                return (false, details?.Replace("Events.Register failed:", "Events.RegisterQueue failed:"), null, -1);
            }
            if (registration!.LastEventId64 is < int.MinValue or > int.MaxValue)
            {
                return (false, $"Events.RegisterQueue failed: Cursor exceeds the legacy int range. " +
                    $"Queue '{registration.QueueId}' was allocated; delete it when unused, and use Register with LastEventId64.",
                    registration.QueueId, -1);
            }
            return (true, null, registration.QueueId, registration.LastEventId);
        }

        /// <summary>Registers an event queue (throwing version).</summary>
        /// <param name="eventTypes">Optional event types as a JSON array.</param>
        /// <param name="narrow">Optional narrow filter as JSON.</param>
        /// <param name="allPublicStreams">Optional inclusion of all public channels.</param>
        /// <returns>The queue ID and initial cursor.</returns>
        public async Task<(string queueId, int lastEventId)> RegisterQueue(
            string? eventTypes = null, string? narrow = null, bool? allPublicStreams = null)
        {
            (bool success, string? details, string? queueId, int lastEventId) result = await TryRegisterQueue(eventTypes, narrow, allPublicStreams);
            if (!result.success) throw new Exception(result.details);
            return (result.queueId!, result.lastEventId);
        }

        /// <summary>Gets events from a queue.</summary>
        /// <param name="queueId">The queue ID.</param>
        /// <param name="lastEventId">The last event ID received.</param>
        /// <param name="dontBlock">(Optional) Whether to return immediately.</param>
        /// <remarks>Missing or invalid events fail explicitly; an actual empty events array is valid.</remarks>
        /// <returns>An asynchronous result that yields (success, details, events).</returns>
        public Task<(bool success, string? details, List<EventObject>? events)> TryGetEvents(
            string queueId,
            int lastEventId,
            bool? dontBlock = null) => TryGetEvents(queueId, (long)lastEventId, dontBlock);

        /// <summary>Gets events using a lossless last-received event cursor.</summary>
        /// <param name="queueId">The queue ID.</param>
        /// <param name="lastEventId">The last event ID received, including -1.</param>
        /// <param name="dontBlock">Optional nonblocking polling.</param>
        /// <returns>The Try result with validated events or failure details.</returns>
        public async Task<(bool success, string? details, List<EventObject>? events)> TryGetEvents(
            string queueId,
            long lastEventId,
            bool? dontBlock = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "queue_id", queueId },
                { "last_event_id", lastEventId.ToString(CultureInfo.InvariantCulture) }
            };

            if (dontBlock != null) data.Add("dont_block", dontBlock.Value.ToString().ToLowerInvariant());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/events", data);

            if (ResponsePayload.TryRead(response, "Events.GetEvents", "events", JsonValueKind.Array,
                out List<EventObject>? events, out string? details, ValidateEvents,
                response.TakeUnobservedEvents, ValidateEventMessages))
            {
                return (true, null, events!);
            }

            return (false, details, null);
        }

        /// <summary>Validates event identities and common message identities without restricting event type names.</summary>
        /// <param name="events">The required events array.</param>
        /// <returns>A diagnostic, or null.</returns>
        private static string? ValidateEvents(JsonElement events)
        {
            foreach (JsonElement item in events.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("id", out JsonElement id) ||
                    id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out _) ||
                    !item.TryGetProperty("type", out JsonElement type) ||
                    type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString()))
                {
                    return "each event must have an integer 'id' and nonempty String 'type'.";
                }

                if (type.GetString() == "message")
                {
                    if (!item.TryGetProperty("message", out JsonElement message) ||
                        message.ValueKind != JsonValueKind.Object)
                    {
                        return "message events require a 'message' object.";
                    }

                    string? error = ResponsePayload.ValidateObjectId(message, "id");
                    if (error != null) return "event message: " + error;
                    if (!message.GetProperty("id").TryGetInt32(out _))
                    {
                        return "event message 'id' must fit the supported integer message-ID representation.";
                    }
                }
            }

            return null;
        }

        /// <summary>Validates and retains typed message data without discarding a duplicate parse.</summary>
        /// <param name="events">The shape-validated events returned to the caller.</param>
        private static void ValidateEventMessages(List<EventObject> events)
        {
            foreach (EventObject item in events)
            {
                if (item.Type == "message")
                {
                    _ = item.Message;
                }
            }
        }

        /// <summary>Gets events from a queue (throwing version).</summary>
        /// <param name="queueId">The queue ID.</param>
        /// <param name="lastEventId">The last acknowledged event ID.</param>
        /// <param name="dontBlock">Optional nonblocking polling.</param>
        /// <returns>The returned events, including an actual empty array.</returns>
        public Task<List<EventObject>> GetEvents(string queueId, int lastEventId, bool? dontBlock = null) =>
            GetEvents(queueId, (long)lastEventId, dontBlock);

        /// <summary>Gets events using a lossless cursor, throwing on invalid or failed responses.</summary>
        /// <param name="queueId">The queue ID.</param>
        /// <param name="lastEventId">The last acknowledged event ID, including -1.</param>
        /// <param name="dontBlock">Optional nonblocking polling.</param>
        /// <returns>The returned events, including an actual empty array.</returns>
        public async Task<List<EventObject>> GetEvents(string queueId, long lastEventId, bool? dontBlock = null)
        {
            (bool success, string? details, List<EventObject>? events) result = await TryGetEvents(queueId, lastEventId, dontBlock);
            if (!result.success) throw new Exception(result.details);
            return result.events!;
        }

        /// <summary>Deletes an event queue.</summary>
        /// <param name="queueId">The queue ID to delete.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryDeleteQueue(string queueId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "queue_id", queueId }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, "api/v1/events", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Events.DeleteQueue failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes an event queue (throwing version).</summary>
        /// <param name="queueId">The queue ID to delete.</param>
        public async Task DeleteQueue(string queueId)
        {
            (bool success, string? details) result = await TryDeleteQueue(queueId);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
