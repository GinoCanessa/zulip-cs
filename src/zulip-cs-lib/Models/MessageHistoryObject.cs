using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a message edit history entry.</summary>
    public class MessageHistoryObject
    {
        /// <summary>Gets or sets the user ID of the editor.</summary>
        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the timestamp of the edit.</summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>Gets or sets the previous content.</summary>
        [JsonPropertyName("prev_content")]
        public string PrevContent { get; set; }

        /// <summary>Gets or sets the previous rendered content.</summary>
        [JsonPropertyName("prev_rendered_content")]
        public string PrevRenderedContent { get; set; }

        /// <summary>Gets or sets the previous topic.</summary>
        [JsonPropertyName("prev_topic")]
        public string PrevTopic { get; set; }

        /// <summary>Gets or sets the topic.</summary>
        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        /// <summary>Gets or sets the content.</summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>Gets or sets the rendered content.</summary>
        [JsonPropertyName("rendered_content")]
        public string RenderedContent { get; set; }
    }
}
