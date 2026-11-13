using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>Deserialization target for Zulip API call responses.</summary>
    [JsonConverter(typeof(ZulipResponseConverter))]
    public class ZulipResponse
    {
        /// <summary>The wire members value, which may contain users or integer group-member IDs.</summary>
        private JsonElement? _membersPayload;

        /// <summary>The caller-supplied user list.</summary>
        private List<UserObject>? _members;

        /// <summary>The wire presence value, including modern scalar timestamps.</summary>
        private JsonElement? _presencePayload;

        /// <summary>The caller-supplied legacy presence dictionary.</summary>
        private Dictionary<string, PresenceInfo>? _presence;

        /// <summary>The owned response snapshot.</summary>
        private JsonElement _rawPayload;

        /// <summary>Explicitly assigned nonnullable wire integers, distinguishing missing fields from valid zero values.</summary>
        private readonly Dictionary<string, int> _assignedIntegers = [];

        /// <summary>Gets the owned, original JSON response, unaffected by subsequent property changes.</summary>
        [JsonIgnore]
        public JsonElement RawPayload
        {
            get => _rawPayload;
            internal set => _rawPayload = value.ValueKind == JsonValueKind.Undefined ? default : value.Clone();
        }

        /// <summary>Gets or sets unmodeled response fields.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        /// <summary>Gets or sets nonfatal warnings about parameters unsupported by the server.</summary>
        [JsonPropertyName("ignored_parameters_unsupported")]
        public List<string>? IgnoredParametersUnsupported { get; set; }

        /// <summary>The zulip result success.</summary>
        public const string ZulipResultSuccess = "success";

        /// <summary>Gets or sets a value indicating whether the success.</summary>
        [JsonIgnore]
        public bool Success { get; set; }

        /// <summary>Gets or sets the caught exception.</summary>
        [JsonIgnore]
        public string? CaughtException { get; set; }

        /// <summary>Gets or sets the HTTP response code.</summary>
        [JsonIgnore]
        public int HttpResponseCode { get; set; }

        /// <summary>Gets or sets the HTTP response body.</summary>
        [JsonIgnore]
        public string? HttpResponseBody { get; set; }

        /// <summary>Gets or sets the result.</summary>
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        /// <summary>Gets or sets the message.</summary>
        [JsonPropertyName("msg")]
        public string? Message { get; set; }

        /// <summary>Gets or sets the rendered message.</summary>
        [JsonPropertyName("rendered")]
        public string? RenderedMessage { get; set; }

        /// <summary>Gets or sets the error code.</summary>
        [JsonPropertyName("code")]
        public string? ErrorCode { get; set; }

        /// <summary>Gets or sets the response ID.</summary>
        [JsonPropertyName("id")]
        public ulong? Id { get; set; }

        /// <summary>
        /// Gets or sets the deliver at time.
        /// Present for scheduled messages, encodes the time when the message will be sent.
        /// </summary>
        [JsonPropertyName("deliver_at")]
        public string? DeliverAt { get; set; }

        /// <summary>Gets or sets the location.</summary>
        [JsonPropertyName("uri")]
        public Uri? ReturnedUri { get; set; }

        /// <summary>Gets or sets a single stream object.</summary>
        [JsonPropertyName("stream")]
        public StreamObject? Stream { get; set; }

        /// <summary>Gets or sets the messages list.</summary>
        [JsonPropertyName("messages")]
        public List<MessageObject>? Messages { get; set; }

        /// <summary>Gets or sets the users list. Integer group-member payloads are not user objects and return null.</summary>
        [JsonIgnore]
        public List<UserObject>? Members
        {
            get
            {
                if (_members == null && _membersPayload is JsonElement payload &&
                    payload.ValueKind == JsonValueKind.Array &&
                    payload.EnumerateArray().All(member => member.ValueKind == JsonValueKind.Object))
                {
                    _members = payload.Deserialize<List<UserObject>>();
                }

                return _members;
            }
            set
            {
                _members = value;
                _membersPayload = null;
            }
        }

        /// <summary>Gets or sets the collision-safe wire members value.</summary>
        [JsonPropertyName("members")]
        internal JsonElement? MembersPayload
        {
            get => _members != null ? JsonSerializer.SerializeToElement(_members) : _membersPayload;
            set
            {
                _membersPayload = value?.Clone();
                _members = null;
            }
        }

        /// <summary>Gets or sets the streams list.</summary>
        [JsonPropertyName("streams")]
        public List<StreamObject>? Streams { get; set; }

        /// <summary>Gets or sets the subscriptions list.</summary>
        [JsonPropertyName("subscriptions")]
        public List<SubscriptionObject>? Subscriptions { get; set; }

        /// <summary>Gets or sets the topics list.</summary>
        [JsonPropertyName("topics")]
        public List<TopicObject>? Topics { get; set; }

        /// <summary>Gets or sets the zulip version.</summary>
        [JsonPropertyName("zulip_version")]
        public string? ZulipVersion { get; set; }

        /// <summary>Gets or sets the zulip feature level.</summary>
        [JsonPropertyName("zulip_feature_level")]
        public int? ZulipFeatureLevel { get; set; }

        /// <summary>Gets or sets the queue ID for event queues.</summary>
        [JsonPropertyName("queue_id")]
        public string? QueueId { get; set; }

        /// <summary>Gets or sets the last event ID.</summary>
        [JsonPropertyName("last_event_id")]
        public int LastEventId
        {
            get => _assignedIntegers.GetValueOrDefault("last_event_id");
            set => _assignedIntegers["last_event_id"] = value;
        }

        /// <summary>Gets or sets the events list.</summary>
        [JsonPropertyName("events")]
        public List<EventObject>? Events { get; set; }

        /// <summary>Gets or sets a single message object.</summary>
        [JsonPropertyName("message")]
        public MessageObject? MessageObject { get; set; }

        /// <summary>Gets or sets a single user object.</summary>
        [JsonPropertyName("user")]
        public UserObject? User { get; set; }

        /// <summary>Gets or sets the stream ID.</summary>
        [JsonPropertyName("stream_id")]
        public int StreamId
        {
            get => _assignedIntegers.GetValueOrDefault("stream_id");
            set => _assignedIntegers["stream_id"] = value;
        }

        /// <summary>Gets or sets the raw content.</summary>
        [JsonPropertyName("raw_content")]
        public string? RawContent { get; set; }

        /// <summary>Gets or sets whether the newest message was found.</summary>
        [JsonPropertyName("found_newest")]
        public bool? FoundNewest { get; set; }

        /// <summary>Gets or sets whether the oldest message was found.</summary>
        [JsonPropertyName("found_oldest")]
        public bool? FoundOldest { get; set; }

        /// <summary>Gets or sets whether the anchor was found.</summary>
        [JsonPropertyName("found_anchor")]
        public bool? FoundAnchor { get; set; }

        /// <summary>Gets or sets the count of messages read.</summary>
        [JsonPropertyName("messages_read")]
        public int? MessagesRead { get; set; }

        /// <summary>Gets or sets the API key.</summary>
        [JsonPropertyName("api_key")]
        public string? ApiKey { get; set; }

        /// <summary>Gets or sets the fetched email.</summary>
        [JsonPropertyName("email")]
        public string? FetchedEmail { get; set; }

        /// <summary>Gets or sets the message history.</summary>
        [JsonPropertyName("message_history")]
        public List<MessageHistoryObject>? MessageHistory { get; set; }

        /// <summary>Gets or sets the user IDs list (for read receipts).</summary>
        [JsonPropertyName("user_ids")]
        public List<int>? UserIds { get; set; }

        /// <summary>Gets or sets the subscribers list.</summary>
        [JsonPropertyName("subscribers")]
        public List<int>? Subscribers { get; set; }

        /// <summary>Gets or sets the scheduled messages.</summary>
        [JsonPropertyName("scheduled_messages")]
        public List<ScheduledMessageObject>? ScheduledMessages { get; set; }

        /// <summary>Gets or sets the scheduled message ID.</summary>
        [JsonPropertyName("scheduled_message_id")]
        public int ScheduledMessageId
        {
            get => _assignedIntegers.GetValueOrDefault("scheduled_message_id");
            set => _assignedIntegers["scheduled_message_id"] = value;
        }

        /// <summary>Gets or sets the drafts.</summary>
        [JsonPropertyName("drafts")]
        public List<DraftObject>? Drafts { get; set; }

        /// <summary>Gets or sets the invites.</summary>
        [JsonPropertyName("invites")]
        public List<InviteObject>? Invites { get; set; }

        /// <summary>Gets or sets the linkifiers.</summary>
        [JsonPropertyName("linkifiers")]
        public List<LinkifierObject>? Linkifiers { get; set; }

        /// <summary>Gets or sets the custom emoji list.</summary>
        [JsonPropertyName("emoji")]
        public Dictionary<string, EmojiObject>? Emoji { get; set; }

        /// <summary>Gets or sets the custom profile fields.</summary>
        [JsonPropertyName("custom_fields")]
        public List<ProfileFieldObject>? CustomFields { get; set; }

        /// <summary>Gets or sets the saved snippets.</summary>
        [JsonPropertyName("saved_snippets")]
        public List<SavedSnippetObject>? SavedSnippets { get; set; }

        /// <summary>Gets or sets the reminders.</summary>
        [JsonPropertyName("reminders")]
        public List<ReminderObject>? Reminders { get; set; }

        /// <summary>Gets or sets the navigation views.</summary>
        [JsonPropertyName("navigation_views")]
        public List<NavigationViewObject>? NavigationViews { get; set; }

        /// <summary>Gets or sets the user groups.</summary>
        [JsonPropertyName("user_groups")]
        public List<UserGroupObject>? UserGroups { get; set; }

        /// <summary>Gets or sets the alert words.</summary>
        [JsonPropertyName("alert_words")]
        public List<string>? AlertWords { get; set; }

        /// <summary>Gets or sets the email address (for channel email endpoint).</summary>
        [JsonPropertyName("email_address")]
        public string? EmailAddress { get; set; }

        /// <summary>Gets or sets the is_subscribed flag.</summary>
        [JsonPropertyName("is_subscribed")]
        public bool? IsSubscribed { get; set; }

        /// <summary>Gets or sets the status object.</summary>
        [JsonPropertyName("status")]
        public UserStatusObject? Status { get; set; }

        /// <summary>Gets or sets legacy presence data. Modern timestamps remain available in RawPayload and presence details.</summary>
        [JsonIgnore]
        public Dictionary<string, PresenceInfo>? Presence
        {
            get
            {
                if (_presence == null && _presencePayload is JsonElement payload &&
                    payload.ValueKind == JsonValueKind.Object)
                {
                    Dictionary<string, PresenceInfo> legacy = [];
                    foreach (JsonProperty property in payload.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Object &&
                            property.Value.TryGetProperty("status", out _) &&
                            property.Value.TryGetProperty("timestamp", out _))
                        {
                            legacy.Add(property.Name, property.Value.Deserialize<PresenceInfo>()!);
                        }
                    }

                    if (legacy.Count != 0 || !payload.EnumerateObject().Any())
                    {
                        _presence = legacy;
                    }
                }

                return _presence;
            }
            set
            {
                _presence = value;
                _presencePayload = null;
            }
        }

        /// <summary>Gets or sets the collision-safe wire presence value.</summary>
        [JsonPropertyName("presence")]
        internal JsonElement? PresencePayload
        {
            get => _presencePayload ?? (_presence != null ? JsonSerializer.SerializeToElement(_presence) : null);
            set
            {
                _presencePayload = value?.Clone();
                _presence = null;
            }
        }

        /// <summary>Gets or sets the presences data for realm presence.</summary>
        [JsonPropertyName("presences")]
        public Dictionary<string, Dictionary<string, PresenceInfo>>? Presences { get; set; }

        /// <summary>Gets or sets the legacy filter_id field. Current linkifier creation returns Id instead.</summary>
        [JsonPropertyName("filter_id")]
        public int FilterId
        {
            get => _assignedIntegers.GetValueOrDefault("filter_id");
            set => _assignedIntegers["filter_id"] = value;
        }

        /// <summary>Gets or sets the reusable invitation URL returned as invite_link.</summary>
        [JsonPropertyName("invite_link")]
        public string? InviteLinkUrl { get; set; }

        /// <summary>Builds a payload for injected, caller-created responses without inventing unassigned integer fields.</summary>
        /// <returns>The explicit response fields with their actual assigned values.</returns>
        internal JsonElement GetAssignedPayload()
        {
            JsonElement serialized = JsonSerializer.SerializeToElement(this, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            Dictionary<string, JsonElement> fields = serialized.Deserialize<Dictionary<string, JsonElement>>()!;
            foreach (string field in new string[] { "last_event_id", "stream_id", "scheduled_message_id", "filter_id" })
            {
                if (!_assignedIntegers.ContainsKey(field)) fields.Remove(field);
            }

            return JsonSerializer.SerializeToElement(fields);
        }

        /// <summary>Builds error message.</summary>
        /// <returns>A string.</returns>
        public string GetFailureMessage()
        {
            if (!string.IsNullOrEmpty(Result) && Result != ZulipResultSuccess)
            {
                return $"result: {Result}, code: {ErrorCode}, message: {Message}";
            }

            if (!string.IsNullOrEmpty(CaughtException))
            {
                return CaughtException;
            }

            if (string.IsNullOrEmpty(Result))
            {
                return $"HTTP request failed: {HttpResponseCode}";
            }

            return $"result: {Result}," +
                $" code: {ErrorCode}," +
                $" message: {Message}";
        }
    }
}
