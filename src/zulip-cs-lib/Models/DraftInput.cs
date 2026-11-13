using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>A new draft, without a server-assigned identity. Requires feature level 87.</summary>
    public sealed class DraftInput
    {
        /// <summary>Gets or sets the draft type: empty, stream or private (the draft API's wire values).</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        /// <summary>Gets or sets one channel ID, direct-message user IDs, or an empty unaddressed audience.</summary>
        [JsonPropertyName("to")]
        public List<int> To { get; set; } = [];

        /// <summary>Gets or sets the topic, or an empty string for non-channel drafts.</summary>
        [JsonPropertyName("topic")]
        public string Topic { get; set; } = "";

        /// <summary>Gets or sets the draft Markdown.</summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        /// <summary>Gets or sets the edit UNIX timestamp in seconds; null lets the server supply it.</summary>
        [JsonPropertyName("timestamp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Timestamp { get; set; }
    }
}
