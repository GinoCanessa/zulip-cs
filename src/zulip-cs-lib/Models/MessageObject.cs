using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip message.</summary>
    public class MessageObject
    {
        /// <summary>Gets or sets the message ID.</summary>
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        /// <summary>Gets or sets the sender ID.</summary>
        [JsonPropertyName("sender_id")]
        public int SenderId { get; set; }

        /// <summary>Gets or sets the content (rendered HTML).</summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

        /// <summary>Gets or sets the recipient ID.</summary>
        [JsonPropertyName("recipient_id")]
        public int RecipientId { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>Gets or sets the subject (topic).</summary>
        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        /// <summary>Gets or sets the type (stream, private, direct, channel).</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>Gets or sets the sender email.</summary>
        [JsonPropertyName("sender_email")]
        public string SenderEmail { get; set; }

        /// <summary>Gets or sets the sender full name.</summary>
        [JsonPropertyName("sender_full_name")]
        public string SenderFullName { get; set; }

        /// <summary>Gets or sets the sender realm string.</summary>
        [JsonPropertyName("sender_realm_str")]
        public string SenderRealmStr { get; set; }

        /// <summary>Gets or sets the display recipient.</summary>
        [JsonPropertyName("display_recipient")]
        public object DisplayRecipient { get; set; }

        /// <summary>Gets or sets the stream ID.</summary>
        [JsonPropertyName("stream_id")]
        public int? StreamId { get; set; }

        /// <summary>Gets or sets the client string.</summary>
        [JsonPropertyName("client")]
        public string Client { get; set; }

        /// <summary>Gets or sets the content type.</summary>
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        /// <summary>Gets or sets the flags.</summary>
        [JsonPropertyName("flags")]
        public List<string> Flags { get; set; }

        /// <summary>Gets or sets the avatar URL.</summary>
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        /// <summary>Gets or sets a value indicating whether this is me message.</summary>
        [JsonPropertyName("is_me_message")]
        public bool? IsMeMessage { get; set; }
    }
}
