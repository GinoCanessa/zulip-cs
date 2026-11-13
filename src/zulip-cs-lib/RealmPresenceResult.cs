using System.Collections.Generic;
using System.Text.Json.Serialization;
using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>Organization presence data and clock/revision metadata; ping-only updates omit data and clock.</summary>
    public sealed class RealmPresenceResult
    {
        /// <summary>Gets or sets presence keyed by user ID for modern updates or email for legacy realm reads.</summary>
        [JsonPropertyName("presences")]
        public Dictionary<string, PresenceData>? Presences { get; set; }

        /// <summary>Gets or sets the server UNIX clock in seconds, absent on ping-only responses.</summary>
        [JsonPropertyName("server_timestamp")]
        public double? ServerTimestamp { get; set; }

        /// <summary>Gets or sets the incremental presence cursor from publication (feature level 263).</summary>
        [JsonPropertyName("presence_last_update_id")]
        public long? PresenceLastUpdateId { get; set; }

        /// <summary>Gets request-local diagnostics and warnings.</summary>
        [JsonIgnore]
        public ZulipResponse Response { get; internal set; } = null!;
    }
}
