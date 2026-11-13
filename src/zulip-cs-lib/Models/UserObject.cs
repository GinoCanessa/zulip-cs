using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip user.</summary>
    /// <remarks>
    /// Optional metadata varies with server capabilities and endpoint. Unknown fields are retained.
    /// </remarks>
    public class UserObject
    {
        /// <summary>Gets or sets the user ID.</summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        /// <summary>Gets or sets the email.</summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>Gets or sets the full name.</summary>
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        /// <summary>Gets or sets a value indicating whether the user is a bot.</summary>
        [JsonPropertyName("is_bot")]
        public bool? IsBot { get; set; }

        /// <summary>Gets or sets the avatar URL.</summary>
        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        /// <summary>Gets or sets the role.</summary>
        [JsonPropertyName("role")]
        public int? Role { get; set; }

        /// <summary>Gets or sets a value indicating whether the user is active.</summary>
        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        /// <summary>Gets or sets the timezone.</summary>
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        /// <summary>Gets or sets the date joined.</summary>
        /// <remarks>
        /// For imported stubs this is initially the import account creation time.
        /// Starting at feature level 475 it is updated to the first login time when the stub becomes a regular user.
        /// </remarks>
        [JsonPropertyName("date_joined")]
        public string? DateJoined { get; set; }

        /// <summary>Gets or sets the delivery email.</summary>
        [JsonPropertyName("delivery_email")]
        public string? DeliveryEmail { get; set; }

        /// <summary>Gets or sets whether the user is an imported stub account that has not yet joined Zulip.</summary>
        [JsonPropertyName("is_imported_stub")]
        public bool? IsImportedStub { get; set; }

        /// <summary>Gets or sets whether the user's account data has been permanently deleted.</summary>
        /// <remarks>The server includes this field only when true; deleted users are also deactivated.</remarks>
        [JsonPropertyName("is_deleted")]
        public bool? IsDeleted { get; set; }

        /// <summary>Gets or sets owned JSON fields not modeled by this client.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = [];
    }
}
