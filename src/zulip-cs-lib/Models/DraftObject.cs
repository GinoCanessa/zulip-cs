using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a draft.</summary>
    public class DraftObject
    {
        /// <summary>Gets or sets the draft ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the type.</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>Gets or sets the recipients.</summary>
        [JsonPropertyName("to")]
        public List<int> To { get; set; }

        /// <summary>Gets or sets the topic.</summary>
        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        /// <summary>Gets or sets the content.</summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }
}
