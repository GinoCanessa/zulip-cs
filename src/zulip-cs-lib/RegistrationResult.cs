using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>An event queue, its complete owned initial state, and request-local diagnostics.</summary>
    /// <remarks>
    /// Initial fields are optional and depend on fetch filters and server capabilities.
    /// Registration does not fetch message bodies; fetch those through the messages resource.
    /// </remarks>
    public sealed class RegistrationResult
    {
        /// <summary>Initializes a result from a validated registration envelope.</summary>
        /// <param name="payload">The complete response payload.</param>
        /// <param name="response">The response for this request, including warnings.</param>
        internal RegistrationResult(JsonElement payload, ZulipResponse response)
        {
            RawInitialState = payload.Clone();
            Response = response;
            QueueId = payload.GetProperty("queue_id").GetString()!;
            LastEventId64 = payload.GetProperty("last_event_id").GetInt64();
            ZulipFeatureLevel = ReadOptional<int?>(payload, "zulip_feature_level", JsonValueKind.Number);
            ZulipVersion = ReadOptional<string>(payload, "zulip_version", JsonValueKind.String);
            ZulipMergeBase = ReadOptional<string>(payload, "zulip_merge_base", JsonValueKind.String);
            IdleQueueTimeoutSeconds = ReadOptional<int?>(payload, "idle_queue_timeout_secs", JsonValueKind.Number);
            EventQueueLongpollTimeoutSeconds = ReadOptional<int?>(payload, "event_queue_longpoll_timeout_seconds", JsonValueKind.Number);
            ServerWebPublicStreamsEnabled = ReadOptional<bool?>(payload, "server_web_public_streams_enabled");
            ServerCanSummarizeTopics = ReadOptional<bool?>(payload, "server_can_summarize_topics");
            RealmUsers = ReadOptional<List<UserObject>>(payload, "realm_users", JsonValueKind.Array, ResponsePayload.ValidateUsers);
            RealmNonActiveUsers = ReadOptional<List<UserObject>>(payload, "realm_non_active_users", JsonValueKind.Array, ResponsePayload.ValidateUsers);
            Subscriptions = ReadOptional<List<SubscriptionObject>>(payload, "subscriptions", JsonValueKind.Array, ValidateChannels);
            Unsubscribed = ReadOptional<List<SubscriptionObject>>(payload, "unsubscribed", JsonValueKind.Array, ValidateChannels);
            NeverSubscribed = ReadOptional<List<StreamObject>>(payload, "never_subscribed", JsonValueKind.Array, ValidateChannels);
            Streams = ReadOptional<List<StreamObject>>(payload, "streams", JsonValueKind.Array, ValidateChannels);
            MaxMessageId = ReadOptional<int?>(payload, "max_message_id", JsonValueKind.Number);
            StarredMessages = ReadOptional<List<int>>(payload, "starred_messages", JsonValueKind.Array);
            UnreadMessages = ReadOptional<JsonElement?>(payload, "unread_msgs", JsonValueKind.Object);
            RecentPrivateConversations = ReadOptional<JsonElement?>(payload, "recent_private_conversations", JsonValueKind.Array);
            Presences = ReadOptional<Dictionary<string, PresenceData>>(payload, "presences", JsonValueKind.Object, ResponsePayload.ValidateObjectValues);
            ServerTimestamp = ReadOptional<double?>(payload, "server_timestamp", JsonValueKind.Number);
            response.IgnoredParametersUnsupported ??= ReadOptional<List<string>>(payload, "ignored_parameters_unsupported", JsonValueKind.Array);
        }

        /// <summary>Gets the complete cloned registration envelope, including all unmodeled initial fields and warnings.</summary>
        public JsonElement RawInitialState { get; }

        /// <summary>Gets the request-local response, including transport diagnostics and its owned wire payload.</summary>
        public ZulipResponse Response { get; }

        /// <summary>Gets nonfatal unsupported-parameter warnings.</summary>
        public IReadOnlyList<string> IgnoredParametersUnsupported => Response.IgnoredParametersUnsupported ?? [];

        /// <summary>Gets the nonempty allocated queue ID.</summary>
        public string QueueId { get; }

        /// <summary>Gets the legacy int cursor; use LastEventId64 for lossless polling.</summary>
        /// <exception cref="OverflowException">The cursor is outside the int range.</exception>
        [JsonIgnore]
        public int LastEventId => checked((int)LastEventId64);

        /// <summary>Gets the lossless initial polling cursor; -1 is a legitimate value.</summary>
        [JsonPropertyName("LastEventId")]
        public long LastEventId64 { get; }

        /// <summary>Gets the server feature level, when supplied.</summary>
        public int? ZulipFeatureLevel { get; }

        /// <summary>Gets the server version, when supplied.</summary>
        public string? ZulipVersion { get; }

        /// <summary>Gets the server's upstream merge base, when supplied.</summary>
        public string? ZulipMergeBase { get; }

        /// <summary>Gets the effective queue inactivity timeout in seconds, when supplied (feature level 481).</summary>
        public int? IdleQueueTimeoutSeconds { get; }

        /// <summary>Gets the recommended HTTP timeout for event long polling, when supplied with realm initial state.</summary>
        public int? EventQueueLongpollTimeoutSeconds { get; }

        /// <summary>Gets whether the server enables web-public channels, when supplied with realm initial state.</summary>
        public bool? ServerWebPublicStreamsEnabled { get; }

        /// <summary>Gets whether the server enables topic summarization, when supplied with realm initial state.</summary>
        public bool? ServerCanSummarizeTopics { get; }

        /// <summary>Gets active realm users; is_active is absent on the wire, not inferred.</summary>
        public List<UserObject>? RealmUsers { get; }

        /// <summary>Gets deactivated realm users; is_active is absent on the wire, not inferred.</summary>
        public List<UserObject>? RealmNonActiveUsers { get; }

        /// <summary>Gets current channel subscriptions and personal channel settings, when fetched.</summary>
        public List<SubscriptionObject>? Subscriptions { get; }

        /// <summary>Gets previously subscribed channels and personal settings, when fetched.</summary>
        public List<SubscriptionObject>? Unsubscribed { get; }

        /// <summary>Gets channels the user has never subscribed to, when fetched.</summary>
        public List<StreamObject>? NeverSubscribed { get; }

        /// <summary>Gets visible channels, when stream initial state is fetched.</summary>
        public List<StreamObject>? Streams { get; }

        /// <summary>Gets the deprecated highest received message ID, if returned. Prefer the latest messages anchor.</summary>
        public int? MaxMessageId { get; }

        /// <summary>Gets the user's starred message IDs, when fetched.</summary>
        public List<int>? StarredMessages { get; }

        /// <summary>Gets the unread conversation data structures, including truncation state, when supplied.</summary>
        public JsonElement? UnreadMessages { get; }

        /// <summary>Gets recent direct-message conversations with max_message_id and user_ids, when fetched.</summary>
        public JsonElement? RecentPrivateConversations { get; }

        /// <summary>Gets initial presence data keyed by user ID or email according to slim_presence.</summary>
        /// <remarks>Entries retain the modern timestamps or actual legacy client entries, without synthesizing status.</remarks>
        public Dictionary<string, PresenceData>? Presences { get; }

        /// <summary>Gets the server clock when initial presence data was fetched, when supplied.</summary>
        public double? ServerTimestamp { get; }

        /// <summary>Reads an optional field without silently accepting a null or incompatible supplied value.</summary>
        /// <typeparam name="T">The field's CLR representation.</typeparam>
        /// <param name="payload">The registration envelope.</param>
        /// <param name="field">The wire field name.</param>
        /// <param name="kind">The expected JSON kind; null accepts only booleans.</param>
        /// <param name="validate">Optional semantic validation.</param>
        /// <returns>The parsed value, or default if absent.</returns>
        private static T? ReadOptional<T>(JsonElement payload, string field, JsonValueKind? kind = null,
            Func<JsonElement, string?>? validate = null)
        {
            if (!payload.TryGetProperty(field, out JsonElement value)) return default;
            if (kind.HasValue ? value.ValueKind != kind.Value :
                value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                throw new JsonException($"field '{field}' must be {kind?.ToString() ?? "Boolean"}.");
            }

            string? error = validate?.Invoke(value);
            if (error != null) throw new JsonException($"field '{field}': {error}");
            try
            {
                return value.Deserialize<T>();
            }
            catch (JsonException exception)
            {
                throw new JsonException($"field '{field}': {exception.Message}", exception);
            }
        }

        /// <summary>Validates channel identities without substituting a default zero ID.</summary>
        /// <param name="channels">The channel or subscription array.</param>
        /// <returns>A diagnostic, or null.</returns>
        private static string? ValidateChannels(JsonElement channels)
        {
            foreach (JsonElement channel in channels.EnumerateArray())
            {
                if (channel.ValueKind != JsonValueKind.Object ||
                    !channel.TryGetProperty("stream_id", out JsonElement id) ||
                    id.ValueKind != JsonValueKind.Number || !id.TryGetInt32(out _) ||
                    !channel.TryGetProperty("name", out JsonElement name) || name.ValueKind != JsonValueKind.String)
                {
                    return "channel must have an integer stream_id and string name.";
                }
            }

            return null;
        }
    }
}
