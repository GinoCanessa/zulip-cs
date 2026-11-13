using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip event.</summary>
    /// <remarks>Common message fields are typed; other event payloads remain available in extension data.</remarks>
    [JsonConverter(typeof(EventObjectConverter))]
    public class EventObject
    {
        /// <summary>The complete owned message payload, including unmodeled message fields.</summary>
        private JsonElement? _messagePayload;

        /// <summary>The typed common message data.</summary>
        private MessageObject? _message;

        /// <summary>Gets or sets the event ID.</summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>Gets or sets the event type.</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>Gets or sets the operation, such as add, remove or update.</summary>
        [JsonPropertyName("op")]
        public string? Op { get; set; }

        /// <summary>Gets or sets the deprecated operation alias used by message-flag events.</summary>
        [JsonPropertyName("operation")]
        public string? Operation { get; set; }

        /// <summary>Gets or sets common message data for a message event.</summary>
        /// <remarks>Unmodeled nested message fields remain available in RawMessage.</remarks>
        [JsonIgnore]
        public MessageObject? Message
        {
            get
            {
                if (_message == null && _messagePayload is JsonElement payload && payload.ValueKind == JsonValueKind.Object)
                {
                    _message = payload.Deserialize<MessageObject>();
                }

                return _message;
            }
            set
            {
                _message = value;
                _messagePayload = null;
            }
        }

        /// <summary>Gets the complete original message payload, when supplied by the server.</summary>
        [JsonIgnore]
        public JsonElement? RawMessage => _messagePayload;

        /// <summary>Gets or sets the collision-safe wire message payload.</summary>
        [JsonInclude]
        [JsonPropertyName("message")]
        internal JsonElement? MessagePayload
        {
            get => _messagePayload ?? (_message != null ? JsonSerializer.SerializeToElement(_message) : null);
            set
            {
                _messagePayload = value?.Clone();
                _message = null;
            }
        }

        /// <summary>Gets or sets the user's message flags after message creation or editing.</summary>
        [JsonPropertyName("flags")]
        public List<string>? Flags { get; set; }

        /// <summary>Gets or sets the single flag changed by a message-flag event.</summary>
        [JsonPropertyName("flag")]
        public string? Flag { get; set; }

        /// <summary>Gets or sets the affected message IDs in a message-flag event.</summary>
        [JsonPropertyName("messages")]
        public List<int>? Messages { get; set; }

        /// <summary>Gets or sets whether a flag change applies to all messages.</summary>
        [JsonPropertyName("all")]
        public bool? All { get; set; }

        /// <summary>Gets or sets the edited message ID or legacy single deleted message ID.</summary>
        [JsonPropertyName("message_id")]
        public int? MessageId { get; set; }

        /// <summary>Gets or sets message IDs affected by a move or bulk deletion.</summary>
        [JsonPropertyName("message_ids")]
        public List<int>? MessageIds { get; set; }

        /// <summary>Gets or sets the deleted message type, stream or private.</summary>
        [JsonPropertyName("message_type")]
        public string? MessageType { get; set; }

        /// <summary>Gets or sets the channel ID, or the pre-edit channel ID for a message update.</summary>
        [JsonPropertyName("stream_id")]
        public int? StreamId { get; set; }

        /// <summary>Gets or sets affected channel IDs in subscription events.</summary>
        [JsonPropertyName("stream_ids")]
        public List<int>? StreamIds { get; set; }

        /// <summary>Gets or sets affected user IDs in subscription events.</summary>
        [JsonPropertyName("user_ids")]
        public List<int>? UserIds { get; set; }

        /// <summary>Gets or sets the deleted message's topic.</summary>
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        /// <summary>Gets or sets the local echo identifier returned for the sending client's queue.</summary>
        [JsonPropertyName("local_message_id")]
        public string? LocalMessageId { get; set; }

        /// <summary>Gets or sets the message-update user ID; null is valid for rendering-only updates.</summary>
        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets whether the update only changes rendering, not message history.</summary>
        [JsonPropertyName("rendering_only")]
        public bool? RenderingOnly { get; set; }

        /// <summary>Gets or sets the UNIX timestamp of the message edit.</summary>
        [JsonPropertyName("edit_timestamp")]
        public long? EditTimestamp { get; set; }

        /// <summary>Gets or sets the original channel name for a message update.</summary>
        [JsonPropertyName("stream_name")]
        public string? StreamName { get; set; }

        /// <summary>Gets or sets the destination channel ID for moved messages.</summary>
        [JsonPropertyName("new_stream_id")]
        public int? NewStreamId { get; set; }

        /// <summary>Gets or sets the message-move propagation mode.</summary>
        [JsonPropertyName("propagate_mode")]
        public string? PropagateMode { get; set; }

        /// <summary>Gets or sets the pre-edit topic for moved messages.</summary>
        [JsonPropertyName("orig_subject")]
        public string? OriginalSubject { get; set; }

        /// <summary>Gets or sets the post-edit topic for moved messages.</summary>
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        /// <summary>Gets or sets the message's Markdown content before an edit.</summary>
        [JsonPropertyName("orig_content")]
        public string? OriginalContent { get; set; }

        /// <summary>Gets or sets the message's rendered HTML before an edit.</summary>
        [JsonPropertyName("orig_rendered_content")]
        public string? OriginalRenderedContent { get; set; }

        /// <summary>Gets or sets the updated Markdown content.</summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>Gets or sets the updated rendered HTML content.</summary>
        [JsonPropertyName("rendered_content")]
        public string? RenderedContent { get; set; }

        /// <summary>Gets or sets whether the edited message is a /me status message.</summary>
        [JsonPropertyName("is_me_message")]
        public bool? IsMeMessage { get; set; }

        /// <summary>Gets or sets unmodeled fields, including payloads of unknown future event types.</summary>
        /// <remarks>Fields with varying shapes, such as user and value, are deliberately not strongly typed.</remarks>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
}
