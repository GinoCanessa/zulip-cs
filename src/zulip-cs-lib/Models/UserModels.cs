using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a user group.</summary>
    public class UserGroupObject
    {
        /// <summary>Gets or sets the ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>Gets or sets the members.</summary>
        [JsonPropertyName("members")]
        public List<int>? Members { get; set; }

        /// <summary>Gets or sets direct subgroup IDs, excluding indirect descendants.</summary>
        [JsonPropertyName("direct_subgroup_ids")]
        public List<int>? DirectSubgroupIds { get; set; }

        /// <summary>Gets or sets the creation time in UNIX UTC seconds, or null when unrecorded.</summary>
        [JsonPropertyName("date_created")]
        public long? DateCreated { get; set; }

        /// <summary>Gets or sets the creator user ID, or null when unrecorded.</summary>
        [JsonPropertyName("creator_id")]
        public int? CreatorId { get; set; }

        /// <summary>Gets or sets whether this is a system group that cannot be directly modified.</summary>
        [JsonPropertyName("is_system_group")]
        public bool? IsSystemGroup { get; set; }

        /// <summary>Gets or sets whether the group is deactivated and cannot be used as a subgroup or permission setting.</summary>
        [JsonPropertyName("deactivated")]
        public bool? Deactivated { get; set; }

        /// <summary>Gets or sets the group setting for adding members.</summary>
        [JsonPropertyName("can_add_members_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanAddMembersGroup { get; set; }

        /// <summary>Gets or sets the group setting for joining the group.</summary>
        [JsonPropertyName("can_join_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanJoinGroup { get; set; }

        /// <summary>Gets or sets the group setting for leaving the group.</summary>
        [JsonPropertyName("can_leave_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanLeaveGroup { get; set; }

        /// <summary>Gets or sets the group setting for managing the group.</summary>
        [JsonPropertyName("can_manage_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanManageGroup { get; set; }

        /// <summary>Gets or sets the group setting for mentioning the group.</summary>
        [JsonPropertyName("can_mention_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanMentionGroup { get; set; }

        /// <summary>Gets or sets the group setting for removing members.</summary>
        [JsonPropertyName("can_remove_members_group")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? CanRemoveMembersGroup { get; set; }

        /// <summary>Gets or sets owned JSON fields not modeled by this client.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = [];
    }

    /// <summary>Represents a user status.</summary>
    public class UserStatusObject
    {
        /// <summary>Gets or sets the status text.</summary>
        [JsonPropertyName("status_text")]
        public string? StatusText { get; set; }

        /// <summary>Gets or sets the emoji name.</summary>
        [JsonPropertyName("emoji_name")]
        public string? EmojiName { get; set; }

        /// <summary>Gets or sets the emoji code.</summary>
        [JsonPropertyName("emoji_code")]
        public string? EmojiCode { get; set; }

        /// <summary>Gets or sets the reaction type.</summary>
        [JsonPropertyName("reaction_type")]
        public string? ReactionType { get; set; }

        /// <summary>Gets or sets whether the user is away.</summary>
        [JsonPropertyName("away")]
        public bool? Away { get; set; }
    }

    /// <summary>Represents presence info.</summary>
    public class PresenceInfo
    {
        /// <summary>Gets or sets the status string.</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }

        /// <summary>Gets or sets the client.</summary>
        [JsonPropertyName("client")]
        public string? Client { get; set; }
    }
}
