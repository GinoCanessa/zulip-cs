using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Optional initial subgroups and governance settings for a new user group.</summary>
    /// <remarks>Subgroups require feature level 311; all represented governance settings are supported by feature level 324.</remarks>
    public sealed class GroupOptions
    {
        /// <summary>Gets or sets the initial subgroup IDs; null leaves the server default unchanged.</summary>
        public int[]? Subgroups { get; set; }

        /// <summary>Gets or sets who can add members (feature level 305).</summary>
        public GroupSettingValue? CanAddMembersGroup { get; set; }

        /// <summary>Gets or sets who can join (feature level 301).</summary>
        public GroupSettingValue? CanJoinGroup { get; set; }

        /// <summary>Gets or sets who can leave (feature level 308).</summary>
        public GroupSettingValue? CanLeaveGroup { get; set; }

        /// <summary>Gets or sets who can manage the group (feature level 283).</summary>
        public GroupSettingValue? CanManageGroup { get; set; }

        /// <summary>Gets or sets who can mention the group, including anonymous values (feature level 258).</summary>
        public GroupSettingValue? CanMentionGroup { get; set; }

        /// <summary>Gets or sets who can remove members (feature level 324).</summary>
        public GroupSettingValue? CanRemoveMembersGroup { get; set; }

        /// <summary>Adds only explicitly supplied, validated creation options to a form.</summary>
        /// <param name="data">The request form.</param>
        /// <returns>An input error, or null.</returns>
        internal string? AddTo(Dictionary<string, string> data)
        {
            if (Subgroups != null)
            {
                if (!ValidIds(Subgroups)) return "subgroups must contain positive integer IDs.";
                data["subgroups"] = JsonSerializer.Serialize(Subgroups);
            }

            foreach ((string name, GroupSettingValue? value) in new (string, GroupSettingValue?)[]
            {
                ("can_add_members_group", CanAddMembersGroup),
                ("can_join_group", CanJoinGroup),
                ("can_leave_group", CanLeaveGroup),
                ("can_manage_group", CanManageGroup),
                ("can_mention_group", CanMentionGroup),
                ("can_remove_members_group", CanRemoveMembersGroup)
            })
            {
                if (value == null) continue;
                string? error = AddPermission(data, name, value);
                if (error != null) return error;
            }

            return null;
        }

        /// <summary>Checks integer IDs without rejecting an empty list.</summary>
        /// <param name="ids">The IDs to validate.</param>
        /// <returns>Whether every ID is positive.</returns>
        internal static bool ValidIds(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                if (id <= 0) return false;
            }
            return true;
        }

        /// <summary>Validates an input permission value without discovering realm-specific restrictions.</summary>
        /// <param name="value">The permission value.</param>
        /// <returns>Whether it contains only supported fields and positive IDs.</returns>
        internal static bool ValidPermission(GroupSettingValue value)
        {
            return value.AdditionalProperties.Count == 0 &&
                (value.GroupId.HasValue ? value.GroupId.Value > 0 :
                    value.DirectMembers != null && value.DirectSubgroups != null &&
                    ValidIds(value.DirectMembers) && ValidIds(value.DirectSubgroups));
        }

        /// <summary>Validates and serializes a supplied creation permission setting.</summary>
        /// <param name="data">The request form.</param>
        /// <param name="name">The documented parameter name.</param>
        /// <param name="value">The permission value.</param>
        /// <returns>An input error, or null.</returns>
        internal static string? AddPermission(Dictionary<string, string> data, string name, GroupSettingValue value)
        {
            if (!ValidPermission(value)) return name + " must be a group ID or an anonymous group with positive integer IDs.";
            data[name] = JsonSerializer.Serialize(value);
            return null;
        }
    }

    /// <summary>A replacement permission value with an optional expected current value.</summary>
    /// <remarks>An old-value mismatch is reported by the server as EXPECTATION_MISMATCH; this client does not retry.</remarks>
    public sealed class GroupSettingUpdate
    {
        /// <summary>Initializes a group-setting update.</summary>
        /// <param name="newValue">The required replacement permission value.</param>
        /// <param name="oldValue">An optional expected current value.</param>
        public GroupSettingUpdate(GroupSettingValue newValue, GroupSettingValue? oldValue = null)
        {
            ArgumentNullException.ThrowIfNull(newValue);
            New = newValue;
            Old = oldValue;
        }

        /// <summary>Gets the replacement value.</summary>
        [JsonPropertyName("new")]
        public GroupSettingValue New { get; }

        /// <summary>Gets the expected current value, omitted when no expectation is supplied.</summary>
        [JsonPropertyName("old")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GroupSettingValue? Old { get; }
    }

    /// <summary>Optional governance updates and reactivation for an existing user group.</summary>
    /// <remarks>All represented governance settings are supported by feature level 324. Reactivation requires feature level 386.</remarks>
    public sealed class GroupUpdateOptions
    {
        /// <summary>Gets or sets the update to who can add members.</summary>
        public GroupSettingUpdate? CanAddMembersGroup { get; set; }

        /// <summary>Gets or sets the update to who can join.</summary>
        public GroupSettingUpdate? CanJoinGroup { get; set; }

        /// <summary>Gets or sets the update to who can leave.</summary>
        public GroupSettingUpdate? CanLeaveGroup { get; set; }

        /// <summary>Gets or sets the update to who can manage the group.</summary>
        public GroupSettingUpdate? CanManageGroup { get; set; }

        /// <summary>Gets or sets the update to who can mention the group.</summary>
        public GroupSettingUpdate? CanMentionGroup { get; set; }

        /// <summary>Gets or sets the update to who can remove members.</summary>
        public GroupSettingUpdate? CanRemoveMembersGroup { get; set; }

        /// <summary>Gets or sets whether to reactivate the group; false omits the parameter.</summary>
        public bool Reactivate { get; set; }

        /// <summary>Adds validated group-setting updates using the new/old wire format.</summary>
        /// <param name="data">The request form.</param>
        /// <returns>An input error, or null.</returns>
        internal string? AddTo(Dictionary<string, string> data)
        {
            foreach ((string name, GroupSettingUpdate? value) in new (string, GroupSettingUpdate?)[]
            {
                ("can_add_members_group", CanAddMembersGroup),
                ("can_join_group", CanJoinGroup),
                ("can_leave_group", CanLeaveGroup),
                ("can_manage_group", CanManageGroup),
                ("can_mention_group", CanMentionGroup),
                ("can_remove_members_group", CanRemoveMembersGroup)
            })
            {
                if (value == null) continue;
                if (!GroupOptions.ValidPermission(value.New) ||
                    (value.Old != null && !GroupOptions.ValidPermission(value.Old)))
                {
                    return name + " must contain valid new and optional old group-setting values.";
                }
                data[name] = JsonSerializer.Serialize(value);
            }

            if (Reactivate) data["deactivated"] = "false";
            return null;
        }
    }
}
