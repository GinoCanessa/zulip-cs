using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>A user's organization data-export consent state.</summary>
    public sealed class OrganizationExportConsent
    {
        /// <summary>Gets or sets the user identity.</summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        /// <summary>Gets or sets whether private data export is consented to.</summary>
        [JsonPropertyName("consented")]
        public bool Consented { get; set; }

        /// <summary>Gets or sets email visibility, or null on servers before feature level 430.</summary>
        [JsonPropertyName("email_address_visibility")]
        public int? EmailAddressVisibility { get; set; }
    }
}
