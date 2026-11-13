using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents an invitation.</summary>
    public class InviteObject
    {
        /// <summary>Gets or sets the invite ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the invited by user ID.</summary>
        [JsonPropertyName("invited_by_user_id")]
        public int? InvitedByUserId { get; set; }

        /// <summary>Gets or sets the invited email.</summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>Gets or sets whether the invite is a multiuse link.</summary>
        [JsonPropertyName("is_multiuse")]
        public bool? IsMultiuse { get; set; }

        /// <summary>Gets or sets the link URL.</summary>
        [JsonPropertyName("link_url")]
        public string? LinkUrl { get; set; }

        /// <summary>Gets or sets the invitation creation time in UNIX UTC seconds.</summary>
        [JsonPropertyName("invited")]
        public long? Invited { get; set; }

        /// <summary>Gets or sets the expiry time in UNIX UTC seconds, or null for an invitation that never expires.</summary>
        [JsonPropertyName("expiry_date")]
        public long? ExpiryDate { get; set; }

        /// <summary>Gets or sets the organization role assigned when the invitation is accepted.</summary>
        /// <remarks>Known roles are 100 (owner), 200 (administrator), 300 (moderator), 400 (member) and 600 (guest).</remarks>
        [JsonPropertyName("invited_as")]
        public int? InvitedAs { get; set; }

        /// <summary>Gets or sets whether the referrer wants a notification when an account joins using this invitation.</summary>
        [JsonPropertyName("notify_referrer_on_join")]
        public bool? NotifyReferrerOnJoin { get; set; }

        /// <summary>Gets or sets owned JSON fields not modeled by this client.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = [];
    }
}
