using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Modern presence timestamps and optional legacy entries, without inferred status.</summary>
    public class PresenceData
    {
        /// <summary>Gets or sets the last reported active UNIX timestamp, when supplied.</summary>
        [JsonPropertyName("active_timestamp")]
        public long? ActiveTimestamp { get; set; }

        /// <summary>Gets or sets the last reported connected UNIX timestamp, including idle clients.</summary>
        [JsonPropertyName("idle_timestamp")]
        public long? IdleTimestamp { get; set; }

        /// <summary>Gets or sets the optional legacy website entry.</summary>
        [JsonPropertyName("website")]
        public PresenceInfo? Website { get; set; }

        /// <summary>Gets or sets the optional legacy aggregated entry, including server-computed offline status.</summary>
        [JsonPropertyName("aggregated")]
        public PresenceInfo? Aggregated { get; set; }

        /// <summary>Gets or sets additional presence fields, including older client-specific entries.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
}
