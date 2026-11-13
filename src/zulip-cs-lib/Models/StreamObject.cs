using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip stream/channel.</summary>
    /// <remarks>
    /// Metadata is shared with subscriptions; fields can be absent depending on endpoint and server capabilities.
    /// Permission groups describe channel settings, not complete authorization decisions:
    /// realm permissions and channel access can also grant or restrict permissions.
    /// </remarks>
    public class StreamObject
    {
        /// <summary>Gets or sets the stream ID.</summary>
        [JsonPropertyName("stream_id")]
        public int StreamId { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>Gets or sets the rendered description.</summary>
        [JsonPropertyName("rendered_description")]
        public string? RenderedDescription { get; set; }

        /// <summary>Gets or sets a value indicating whether this is invite only.</summary>
        [JsonPropertyName("invite_only")]
        public bool? InviteOnly { get; set; }

        /// <summary>Gets or sets a value indicating whether this is a web public stream.</summary>
        [JsonPropertyName("is_web_public")]
        public bool? IsWebPublic { get; set; }

        /// <summary>Gets or sets the stream post policy.</summary>
        /// <remarks>
        /// Deprecated since feature level 333. This coarse policy is an inexact superset of users allowed to post,
        /// not an authorization check. Use <see cref="CanSendMessageGroup"/> for the authoritative channel setting.
        /// </remarks>
        [JsonPropertyName("stream_post_policy")]
        public int? StreamPostPolicy { get; set; }

        /// <summary>Gets or sets the history public to subscribers flag.</summary>
        [JsonPropertyName("history_public_to_subscribers")]
        public bool? HistoryPublicToSubscribers { get; set; }

        /// <summary>Gets or sets the first message ID.</summary>
        [JsonPropertyName("first_message_id")]
        public long? FirstMessageId { get; set; }

        /// <summary>Gets or sets the message retention days.</summary>
        [JsonPropertyName("message_retention_days")]
        public int? MessageRetentionDays { get; set; }

        /// <summary>Gets or sets the date created.</summary>
        [JsonPropertyName("date_created")]
        public long? DateCreated { get; set; }

        /// <summary>Gets or sets the channel creator ID, or null when no creator is recorded.</summary>
        [JsonPropertyName("creator_id")]
        public int? CreatorId { get; set; }

        /// <summary>Gets or sets whether the channel is archived.</summary>
        [JsonPropertyName("is_archived")]
        public bool? IsArchived { get; set; }

        /// <summary>Gets or sets the folder ID, or null when the channel has no folder.</summary>
        [JsonPropertyName("folder_id")]
        public int? FolderId { get; set; }

        /// <summary>Gets or sets the named/empty topic policy.</summary>
        /// <remarks>Known values are inherit, allow_empty_topic, disable_empty_topic and empty_topic_only.</remarks>
        [JsonPropertyName("topics_policy")]
        public string? TopicsPolicy { get; set; }

        /// <summary>Gets or sets whether mobile push is enabled by default on first subscription.</summary>
        /// <remarks>Added at feature level 507; unsubscribing and resubscribing does not reset the user's preference.</remarks>
        [JsonPropertyName("default_push_notifications")]
        public bool? DefaultPushNotifications { get; set; }

        /// <summary>Gets or sets whether the channel has recent message activity.</summary>
        [JsonPropertyName("is_recently_active")]
        public bool? IsRecentlyActive { get; set; }

        /// <summary>Gets or sets the estimated average weekly message traffic, or null when unknown.</summary>
        [JsonPropertyName("stream_weekly_traffic")]
        public int? StreamWeeklyTraffic { get; set; }

        /// <summary>Gets or sets the count of non-deactivated subscribers, including bots.</summary>
        /// <remarks>This server estimate can drift during concurrent subscription changes; do not treat it as an exact membership list.</remarks>
        [JsonPropertyName("subscriber_count")]
        public double? SubscriberCount { get; set; }

        /// <summary>Gets or sets the group setting for adding subscribers.</summary>
        [JsonPropertyName("can_add_subscribers_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanAddSubscribersGroup { get; set; }

        /// <summary>Gets or sets the group setting for removing subscribers.</summary>
        [JsonPropertyName("can_remove_subscribers_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanRemoveSubscribersGroup { get; set; }

        /// <summary>Gets or sets the group setting for administering the channel.</summary>
        [JsonPropertyName("can_administer_channel_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanAdministerChannelGroup { get; set; }

        /// <summary>Gets or sets the group setting for deleting any message in the channel.</summary>
        [JsonPropertyName("can_delete_any_message_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanDeleteAnyMessageGroup { get; set; }

        /// <summary>Gets or sets the group setting for deleting a user's own messages in the channel.</summary>
        [JsonPropertyName("can_delete_own_message_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanDeleteOwnMessageGroup { get; set; }

        /// <summary>Gets or sets the group setting for moving messages out of the channel.</summary>
        [JsonPropertyName("can_move_messages_out_of_channel_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanMoveMessagesOutOfChannelGroup { get; set; }

        /// <summary>Gets or sets the group setting for moving messages between topics within the channel.</summary>
        [JsonPropertyName("can_move_messages_within_channel_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanMoveMessagesWithinChannelGroup { get; set; }

        /// <summary>Gets or sets the authoritative channel group setting for sending messages.</summary>
        /// <remarks>Sending to a new topic also requires the corresponding topic creation permission.</remarks>
        [JsonPropertyName("can_send_message_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanSendMessageGroup { get; set; }

        /// <summary>Gets or sets the group setting for subscribing oneself to the channel.</summary>
        [JsonPropertyName("can_subscribe_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanSubscribeGroup { get; set; }

        /// <summary>Gets or sets the group setting for resolving topics.</summary>
        [JsonPropertyName("can_resolve_topics_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanResolveTopicsGroup { get; set; }

        /// <summary>Gets or sets the group setting for creating topics.</summary>
        [JsonPropertyName("can_create_topic_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanCreateTopicGroup { get; set; }

        /// <summary>Gets or sets owned JSON fields not modeled by this client.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = [];
    }
}
