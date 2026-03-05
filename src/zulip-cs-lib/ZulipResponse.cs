using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>Deserialization target for Zulip API call responses.</summary>
    public class ZulipResponse
    {
        /// <summary>The zulip result success.</summary>
        public const string ZulipResultSuccess = "success";

        /// <summary>Gets or sets a value indicating whether the success.</summary>
        [JsonIgnore]
        public bool Success { get; set; }

        /// <summary>Gets or sets the caught exception.</summary>
        [JsonIgnore]
        public string CaughtException { get; set; }

        /// <summary>Gets or sets the HTTP response code.</summary>
        [JsonIgnore]
        public int HttpResponseCode { get; set; }

        /// <summary>Gets or sets the HTTP response body.</summary>
        [JsonIgnore]
        public string HttpResponseBody { get; set; }

        /// <summary>Gets or sets the result.</summary>
        [JsonPropertyName("result")]
        public string Result { get; set; }

        /// <summary>Gets or sets the message.</summary>
        [JsonPropertyName("msg")]
        public string Message { get; set; }

        /// <summary>Gets or sets the rendered message.</summary>
        [JsonPropertyName("rendered")]
        public string RenderedMessage { get; set; }

        /// <summary>Gets or sets the error code.</summary>
        [JsonPropertyName("code")]
        public string ErrorCode { get; set; }

        /// <summary>Gets or sets the response ID.</summary>
        [JsonPropertyName("id")]
        public ulong? Id { get; set; }

        /// <summary>
        /// Gets or sets the deliver at time.
        /// Present for scheduled messages, encodes the time when the message will be sent.
        /// </summary>
        [JsonPropertyName("deliver_at")]
        public string DeliverAt { get; set; }

        /// <summary>Gets or sets the location.</summary>
        [JsonPropertyName("uri")]
        public Uri ReturnedUri { get; set; }

        /// <summary>Gets or sets a single stream object.</summary>
        [JsonPropertyName("stream")]
        public StreamObject Stream { get; set; }

        /// <summary>Gets or sets the messages list.</summary>
        [JsonPropertyName("messages")]
        public List<MessageObject> Messages { get; set; }

        /// <summary>Gets or sets the members list.</summary>
        [JsonPropertyName("members")]
        public List<UserObject> Members { get; set; }

        /// <summary>Gets or sets the streams list.</summary>
        [JsonPropertyName("streams")]
        public List<StreamObject> Streams { get; set; }

        /// <summary>Gets or sets the subscriptions list.</summary>
        [JsonPropertyName("subscriptions")]
        public List<SubscriptionObject> Subscriptions { get; set; }

        /// <summary>Gets or sets the topics list.</summary>
        [JsonPropertyName("topics")]
        public List<TopicObject> Topics { get; set; }

        /// <summary>Gets or sets the zulip version.</summary>
        [JsonPropertyName("zulip_version")]
        public string ZulipVersion { get; set; }

        /// <summary>Gets or sets the zulip feature level.</summary>
        [JsonPropertyName("zulip_feature_level")]
        public int? ZulipFeatureLevel { get; set; }

        /// <summary>Gets or sets the queue ID for event queues.</summary>
        [JsonPropertyName("queue_id")]
        public string QueueId { get; set; }

        /// <summary>Gets or sets the last event ID.</summary>
        [JsonPropertyName("last_event_id")]
        public int LastEventId { get; set; }

        /// <summary>Gets or sets the events list.</summary>
        [JsonPropertyName("events")]
        public List<EventObject> Events { get; set; }

        /// <summary>Gets or sets a single message object.</summary>
        [JsonPropertyName("message")]
        public MessageObject MessageObject { get; set; }

        /// <summary>Gets or sets a single user object.</summary>
        [JsonPropertyName("user")]
        public UserObject User { get; set; }

        /// <summary>Gets or sets the stream ID.</summary>
        [JsonPropertyName("stream_id")]
        public int StreamId { get; set; }

        /// <summary>Gets or sets the raw content.</summary>
        [JsonPropertyName("raw_content")]
        public string RawContent { get; set; }

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
        public string ApiKey { get; set; }

        /// <summary>Gets or sets the fetched email.</summary>
        [JsonPropertyName("email")]
        public string FetchedEmail { get; set; }

        /// <summary>Gets or sets the message history.</summary>
        [JsonPropertyName("message_history")]
        public List<MessageHistoryObject> MessageHistory { get; set; }

        /// <summary>Gets or sets the user IDs list (for read receipts).</summary>
        [JsonPropertyName("user_ids")]
        public List<int> UserIds { get; set; }

        /// <summary>Gets or sets the subscribers list.</summary>
        [JsonPropertyName("subscribers")]
        public List<int> Subscribers { get; set; }

        /// <summary>Gets or sets the scheduled messages.</summary>
        [JsonPropertyName("scheduled_messages")]
        public List<ScheduledMessageObject> ScheduledMessages { get; set; }

        /// <summary>Gets or sets the scheduled message ID.</summary>
        [JsonPropertyName("scheduled_message_id")]
        public int ScheduledMessageId { get; set; }

        /// <summary>Gets or sets the drafts.</summary>
        [JsonPropertyName("drafts")]
        public List<DraftObject> Drafts { get; set; }

        /// <summary>Gets or sets the invites.</summary>
        [JsonPropertyName("invites")]
        public List<InviteObject> Invites { get; set; }

        /// <summary>Gets or sets the linkifiers.</summary>
        [JsonPropertyName("linkifiers")]
        public List<LinkifierObject> Linkifiers { get; set; }

        /// <summary>Gets or sets the custom emoji list.</summary>
        [JsonPropertyName("emoji")]
        public Dictionary<string, EmojiObject> Emoji { get; set; }

        /// <summary>Gets or sets the custom profile fields.</summary>
        [JsonPropertyName("custom_fields")]
        public List<ProfileFieldObject> CustomFields { get; set; }

        /// <summary>Gets or sets the saved snippets.</summary>
        [JsonPropertyName("saved_snippets")]
        public List<SavedSnippetObject> SavedSnippets { get; set; }

        /// <summary>Gets or sets the reminders.</summary>
        [JsonPropertyName("reminders")]
        public List<ReminderObject> Reminders { get; set; }

        /// <summary>Gets or sets the navigation views.</summary>
        [JsonPropertyName("navigation_views")]
        public List<NavigationViewObject> NavigationViews { get; set; }

        /// <summary>Gets or sets the user groups.</summary>
        [JsonPropertyName("user_groups")]
        public List<UserGroupObject> UserGroups { get; set; }

        /// <summary>Gets or sets the alert words.</summary>
        [JsonPropertyName("alert_words")]
        public List<string> AlertWords { get; set; }

        /// <summary>Gets or sets the email address (for channel email endpoint).</summary>
        [JsonPropertyName("email_address")]
        public string EmailAddress { get; set; }

        /// <summary>Gets or sets the is_subscribed flag.</summary>
        [JsonPropertyName("is_subscribed")]
        public bool? IsSubscribed { get; set; }

        /// <summary>Gets or sets the status object.</summary>
        [JsonPropertyName("status")]
        public UserStatusObject Status { get; set; }

        /// <summary>Gets or sets the presence data.</summary>
        [JsonPropertyName("presence")]
        public Dictionary<string, PresenceInfo> Presence { get; set; }

        /// <summary>Gets or sets the presences data for realm presence.</summary>
        [JsonPropertyName("presences")]
        public Dictionary<string, Dictionary<string, PresenceInfo>> Presences { get; set; }

        /// <summary>Gets or sets the linkifier filter ID.</summary>
        [JsonPropertyName("filter_id")]
        public int FilterId { get; set; }

        /// <summary>Gets or sets the invite link URL.</summary>
        [JsonPropertyName("invite_link_url")]
        public string InviteLinkUrl { get; set; }

        /// <summary>Builds error message.</summary>
        /// <returns>A string.</returns>
        public string GetFailureMessage()
        {
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
