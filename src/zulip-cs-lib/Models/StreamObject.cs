using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip stream/channel.</summary>
    /// <remarks>
    /// Feature levels 394 and 441 added richer stream metadata in API responses (for example subscriber counts and permission-group settings).
    /// This model intentionally captures the core stream fields used by the library.
    /// </remarks>
    public class StreamObject
    {
        /// <summary>Gets or sets the stream ID.</summary>
        [JsonPropertyName("stream_id")]
        public int StreamId { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>Gets or sets the rendered description.</summary>
        [JsonPropertyName("rendered_description")]
        public string RenderedDescription { get; set; }

        /// <summary>Gets or sets a value indicating whether this is invite only.</summary>
        [JsonPropertyName("invite_only")]
        public bool? InviteOnly { get; set; }

        /// <summary>Gets or sets a value indicating whether this is a web public stream.</summary>
        [JsonPropertyName("is_web_public")]
        public bool? IsWebPublic { get; set; }

        /// <summary>Gets or sets the stream post policy.</summary>
        [JsonPropertyName("stream_post_policy")]
        public int? StreamPostPolicy { get; set; }

        /// <summary>Gets or sets the history public to subscribers flag.</summary>
        [JsonPropertyName("history_public_to_subscribers")]
        public bool? HistoryPublicToSubscribers { get; set; }

        /// <summary>Gets or sets the first message ID.</summary>
        [JsonPropertyName("first_message_id")]
        public long? FirstMessageId { get; set; }

        /// <summary>Gets or sets the message retention days.</summary>
        [JsonPropertyName("message_retention_days")]
        public int? MessageRetentionDays { get; set; }

        /// <summary>Gets or sets the date created.</summary>
        [JsonPropertyName("date_created")]
        public long? DateCreated { get; set; }
    }
}
