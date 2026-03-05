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
        public string Email { get; set; }

        /// <summary>Gets or sets whether the invite is a multiuse link.</summary>
        [JsonPropertyName("is_multiuse")]
        public bool? IsMultiuse { get; set; }

        /// <summary>Gets or sets the link URL.</summary>
        [JsonPropertyName("link_url")]
        public string LinkUrl { get; set; }
    }
}
