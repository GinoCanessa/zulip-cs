using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a named group ID or an anonymous collection of users and subgroups.</summary>
    /// <remarks>
    /// Anonymous values contain both <c>direct_members</c> and <c>direct_subgroups</c> arrays.
    /// This value describes a permission setting, not a complete authorization decision:
    /// realm permissions, membership and content access can also affect authorization.
    /// </remarks>
    [JsonConverter(typeof(GroupSettingValueConverter))]
    public sealed class GroupSettingValue
    {
        /// <summary>Creates a permission value referring to a named user group.</summary>
        /// <param name="groupId">The named user group ID.</param>
        public GroupSettingValue(int groupId)
        {
            GroupId = groupId;
        }

        /// <summary>Creates an anonymous permission value.</summary>
        /// <param name="directMembers">The directly included user IDs.</param>
        /// <param name="directSubgroups">The directly included subgroup IDs.</param>
        public GroupSettingValue(IEnumerable<int> directMembers, IEnumerable<int> directSubgroups)
        {
            ArgumentNullException.ThrowIfNull(directMembers);
            ArgumentNullException.ThrowIfNull(directSubgroups);
            DirectMembers = new List<int>(directMembers).AsReadOnly();
            DirectSubgroups = new List<int>(directSubgroups).AsReadOnly();
        }

        /// <summary>Gets the named group ID, or null for an anonymous value.</summary>
        public int? GroupId { get; }

        /// <summary>Gets direct user IDs, or null for a named group value.</summary>
        public IReadOnlyList<int>? DirectMembers { get; }

        /// <summary>Gets direct subgroup IDs, or null for a named group value.</summary>
        public IReadOnlyList<int>? DirectSubgroups { get; }

        /// <summary>Gets owned unknown fields of an anonymous group object.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; } = [];
    }
}
