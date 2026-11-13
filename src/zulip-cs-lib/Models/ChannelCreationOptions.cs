using System.Collections.Generic;
using System.Text.Json;

namespace zulip_cs_lib.Models
{
    /// <summary>Focused initial settings for explicit channel creation.</summary>
    /// <remarks>The creation endpoint requires feature level 417; topic creation permissions require 441 and default push notifications require 507. No folder controls are represented.</remarks>
    public sealed class ChannelCreationOptions
    {
        /// <summary>Gets or sets the channel description; null omits it.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets whether to announce the new channel.</summary>
        public bool? Announce { get; set; }

        /// <summary>Gets or sets whether the channel is private.</summary>
        public bool? InviteOnly { get; set; }

        /// <summary>Gets or sets whether the channel is web-public.</summary>
        public bool? IsWebPublic { get; set; }

        /// <summary>Gets or sets whether to make this a default channel.</summary>
        public bool? IsDefaultStream { get; set; }

        /// <summary>Gets or sets whether newly subscribed users can access existing history.</summary>
        public bool? HistoryPublicToSubscribers { get; set; }

        /// <summary>Gets or sets the topics policy: inherit, allow_empty_topic, disable_empty_topic or empty_topic_only.</summary>
        public string? TopicsPolicy { get; set; }

        /// <summary>Gets or sets retention as a positive day count, realm_default or unlimited.</summary>
        public string? MessageRetentionDays { get; set; }

        /// <summary>Gets or sets who can administer the channel.</summary>
        public GroupSettingValue? CanAdministerChannelGroup { get; set; }

        /// <summary>Gets or sets who can add subscribers.</summary>
        public GroupSettingValue? CanAddSubscribersGroup { get; set; }

        /// <summary>Gets or sets who can remove subscribers.</summary>
        public GroupSettingValue? CanRemoveSubscribersGroup { get; set; }

        /// <summary>Gets or sets who can subscribe themselves.</summary>
        public GroupSettingValue? CanSubscribeGroup { get; set; }

        /// <summary>Gets or sets who can send messages.</summary>
        public GroupSettingValue? CanSendMessageGroup { get; set; }

        /// <summary>Gets or sets who can create topics (feature level 441).</summary>
        public GroupSettingValue? CanCreateTopicGroup { get; set; }

        /// <summary>Gets or sets who can delete any message.</summary>
        public GroupSettingValue? CanDeleteAnyMessageGroup { get; set; }

        /// <summary>Gets or sets who can delete their own messages.</summary>
        public GroupSettingValue? CanDeleteOwnMessageGroup { get; set; }

        /// <summary>Gets or sets who can move messages out of the channel.</summary>
        public GroupSettingValue? CanMoveMessagesOutOfChannelGroup { get; set; }

        /// <summary>Gets or sets who can move messages within the channel.</summary>
        public GroupSettingValue? CanMoveMessagesWithinChannelGroup { get; set; }

        /// <summary>Gets or sets who can resolve topics.</summary>
        public GroupSettingValue? CanResolveTopicsGroup { get; set; }

        /// <summary>Gets or sets the initial mobile push notification default (feature level 507).</summary>
        public bool? DefaultPushNotifications { get; set; }

        /// <summary>Adds only explicitly supplied, validated creation settings to a form.</summary>
        /// <param name="data">The request form.</param>
        /// <returns>An input error, or null.</returns>
        internal string? AddTo(Dictionary<string, string> data)
        {
            if (TopicsPolicy != null &&
                TopicsPolicy is not ("inherit" or "allow_empty_topic" or "disable_empty_topic" or "empty_topic_only"))
            {
                return "topics_policy must be inherit, allow_empty_topic, disable_empty_topic or empty_topic_only.";
            }
            if (MessageRetentionDays != null &&
                MessageRetentionDays is not ("realm_default" or "unlimited") &&
                (!int.TryParse(MessageRetentionDays, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out int days) || days <= 0))
            {
                return "message_retention_days must be a positive day count, realm_default or unlimited.";
            }

            if (Description != null) data["description"] = Description;
            if (TopicsPolicy != null) data["topics_policy"] = TopicsPolicy;
            if (MessageRetentionDays != null) data["message_retention_days"] = JsonSerializer.Serialize(MessageRetentionDays);

            foreach ((string name, bool? value) in new (string, bool?)[]
            {
                ("announce", Announce),
                ("invite_only", InviteOnly),
                ("is_web_public", IsWebPublic),
                ("is_default_stream", IsDefaultStream),
                ("history_public_to_subscribers", HistoryPublicToSubscribers),
                ("default_push_notifications", DefaultPushNotifications)
            })
            {
                if (value.HasValue) data[name] = value.Value ? "true" : "false";
            }

            foreach ((string name, GroupSettingValue? value) in new (string, GroupSettingValue?)[]
            {
                ("can_administer_channel_group", CanAdministerChannelGroup),
                ("can_add_subscribers_group", CanAddSubscribersGroup),
                ("can_remove_subscribers_group", CanRemoveSubscribersGroup),
                ("can_subscribe_group", CanSubscribeGroup),
                ("can_send_message_group", CanSendMessageGroup),
                ("can_create_topic_group", CanCreateTopicGroup),
                ("can_delete_any_message_group", CanDeleteAnyMessageGroup),
                ("can_delete_own_message_group", CanDeleteOwnMessageGroup),
                ("can_move_messages_out_of_channel_group", CanMoveMessagesOutOfChannelGroup),
                ("can_move_messages_within_channel_group", CanMoveMessagesWithinChannelGroup),
                ("can_resolve_topics_group", CanResolveTopicsGroup)
            })
            {
                if (value == null) continue;
                string? error = GroupOptions.AddPermission(data, name, value);
                if (error != null) return error;
            }

            return null;
        }
    }
}
