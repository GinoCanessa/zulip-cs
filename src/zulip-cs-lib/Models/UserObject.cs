using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip user.</summary>
    /// <remarks>
    /// Feature levels 433 and 437 updated user payload behavior and metadata across user-list and user-detail endpoints.
    /// This model contains the core fields used by this library version.
    /// </remarks>
    public class UserObject
    {
        /// <summary>Gets or sets the user ID.</summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        /// <summary>Gets or sets the email.</summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }

        /// <summary>Gets or sets the full name.</summary>
        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        /// <summary>Gets or sets a value indicating whether the user is a bot.</summary>
        [JsonPropertyName("is_bot")]
        public bool? IsBot { get; set; }

        /// <summary>Gets or sets the avatar URL.</summary>
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        /// <summary>Gets or sets the role.</summary>
        [JsonPropertyName("role")]
        public int? Role { get; set; }

        /// <summary>Gets or sets a value indicating whether the user is active.</summary>
        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        /// <summary>Gets or sets the timezone.</summary>
        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        /// <summary>Gets or sets the date joined.</summary>
        [JsonPropertyName("date_joined")]
        public string DateJoined { get; set; }

        /// <summary>Gets or sets the delivery email.</summary>
        [JsonPropertyName("delivery_email")]
        public string DeliveryEmail { get; set; }
    }
}
