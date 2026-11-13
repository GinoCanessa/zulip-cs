using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>The server-reported state of an organization data export.</summary>
    /// <remarks>String export types require feature level 449. Export creation only queues work; callers own progress monitoring.</remarks>
    public sealed class OrganizationExport
    {
        /// <summary>Gets or sets the export identity.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user who requested the export.</summary>
        [JsonPropertyName("acting_user_id")]
        public int ActingUserId { get; set; }

        /// <summary>Gets or sets the export start UNIX timestamp, including fractional seconds.</summary>
        [JsonPropertyName("export_time")]
        public double ExportTime { get; set; }

        /// <summary>Gets or sets the deletion UNIX timestamp, or null when not deleted.</summary>
        [JsonPropertyName("deleted_timestamp")]
        public double? DeletedTimestamp { get; set; }

        /// <summary>Gets or sets the failure UNIX timestamp, or null when not failed.</summary>
        [JsonPropertyName("failed_timestamp")]
        public double? FailedTimestamp { get; set; }

        /// <summary>Gets or sets the download URL, or null when unavailable.</summary>
        [JsonPropertyName("export_url")]
        public string? ExportUrl { get; set; }

        /// <summary>Gets or sets whether generation is pending.</summary>
        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        /// <summary>Gets or sets whether the archive was on a prior server, or null when not reported.</summary>
        /// <remarks>Added at feature levels 499-501 and 506+.</remarks>
        [JsonPropertyName("export_from_prior_server")]
        public bool? ExportFromPriorServer { get; set; }

        /// <summary>Gets or sets the public, full_with_consent, or full_without_consent export type.</summary>
        [JsonPropertyName("export_type")]
        public string ExportType { get; set; } = string.Empty;
    }
}
