using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip topic.</summary>
    /// <remarks>Feature level 334 introduced broader support for empty-string topic names in related topic/message APIs.</remarks>
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
