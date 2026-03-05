using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a scheduled message.</summary>
    public class ScheduledMessageObject
    {
        /// <summary>Gets or sets the scheduled message ID.</summary>
        [JsonPropertyName("scheduled_message_id")]
        public int ScheduledMessageId { get; set; }

        /// <summary>Gets or sets the type.</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>Gets or sets the recipients.</summary>
        [JsonPropertyName("to")]
        public object To { get; set; }

        /// <summary>Gets or sets the content.</summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>Gets or sets the rendered content.</summary>
        [JsonPropertyName("rendered_content")]
        public string RenderedContent { get; set; }

        /// <summary>Gets or sets the topic.</summary>
        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        /// <summary>Gets or sets the scheduled delivery timestamp.</summary>
        [JsonPropertyName("scheduled_delivery_timestamp")]
        public long ScheduledDeliveryTimestamp { get; set; }

        /// <summary>Gets or sets a value indicating whether this message has failed.</summary>
        [JsonPropertyName("failed")]
        public bool? Failed { get; set; }
    }
}
