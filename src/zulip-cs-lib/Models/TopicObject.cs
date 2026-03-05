using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip topic.</summary>
    public class TopicObject
    {
        /// <summary>Gets or sets the max message ID in this topic.</summary>
        [JsonPropertyName("max_id")]
        public long MaxId { get; set; }

        /// <summary>Gets or sets the topic name.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
