using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip message.</summary>
    /// <remarks>
    /// Feature level 365 refined edit/move metadata semantics for message payloads.
    /// Feature level 327 adjusted <c>recipient_id</c> behavior for incoming 1:1 direct messages.
    /// </remarks>
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
        public string? Content { get; set; }

        /// <summary>Gets or sets the recipient ID.</summary>
        /// <remarks>Feature level 327 aligned incoming direct-message recipient IDs with outgoing-conversation semantics.</remarks>
        [JsonPropertyName("recipient_id")]
        public int RecipientId { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>Gets or sets the subject (topic).</summary>
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        /// <summary>Gets or sets the type (stream, private, direct, channel).</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>Gets or sets the sender email.</summary>
        [JsonPropertyName("sender_email")]
        public string? SenderEmail { get; set; }

        /// <summary>Gets or sets the sender full name.</summary>
        [JsonPropertyName("sender_full_name")]
        public string? SenderFullName { get; set; }

        /// <summary>Gets or sets the sender realm string.</summary>
        [JsonPropertyName("sender_realm_str")]
        public string? SenderRealmStr { get; set; }

        /// <summary>Gets or sets the display recipient.</summary>
        [JsonPropertyName("display_recipient")]
        public object? DisplayRecipient { get; set; }

        /// <summary>Gets or sets the stream ID.</summary>
        [JsonPropertyName("stream_id")]
        public int? StreamId { get; set; }

        /// <summary>Gets or sets the client string.</summary>
        [JsonPropertyName("client")]
        public string? Client { get; set; }

        /// <summary>Gets or sets the content type.</summary>
        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        /// <summary>Gets or sets the flags.</summary>
        [JsonPropertyName("flags")]
        public List<string>? Flags { get; set; }

        /// <summary>Gets or sets the avatar URL.</summary>
        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        /// <summary>Gets or sets a value indicating whether this is me message.</summary>
        [JsonPropertyName("is_me_message")]
        public bool? IsMeMessage { get; set; }

        /// <summary>Gets or sets reactions, ordered from oldest to newest.</summary>
        [JsonPropertyName("reactions")]
        public List<MessageReactionObject>? Reactions { get; set; }

        /// <summary>Gets or sets edits and moves, ordered from newest to oldest.</summary>
        /// <remarks>May be absent when unmodified or when edit history access is disabled.</remarks>
        [JsonPropertyName("edit_history")]
        public List<MessageHistoryObject>? EditHistory { get; set; }

        /// <summary>Gets or sets the last content edit time in UNIX UTC seconds.</summary>
        /// <remarks>Absent when content was never edited. Before feature level 365 this also included moves.</remarks>
        [JsonPropertyName("last_edit_timestamp")]
        public long? LastEditTimestamp { get; set; }

        /// <summary>Gets or sets the last channel or topic move time in UNIX UTC seconds.</summary>
        /// <remarks>Absent when never moved; resolving or unresolving a topic alone does not set this value.</remarks>
        [JsonPropertyName("last_moved_timestamp")]
        public long? LastMovedTimestamp { get; set; }

        /// <summary>Gets or sets owned JSON fields not modeled by this client.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = [];
    }

    /// <summary>Represents a reaction to a message, including its emoji namespace and reacting user.</summary>
    public class MessageReactionObject
    {
        /// <summary>Gets or sets the emoji name.</summary>
        [JsonPropertyName("emoji_name")]
        public string? EmojiName { get; set; }

        /// <summary>Gets or sets the emoji identifier within its reaction type namespace.</summary>
        [JsonPropertyName("emoji_code")]
        public string? EmojiCode { get; set; }

        /// <summary>Gets or sets the emoji namespace, such as unicode_emoji, realm_emoji or zulip_extra_emoji.</summary>
        [JsonPropertyName("reaction_type")]
        public string? ReactionType { get; set; }

        /// <summary>Gets or sets the user ID of the person who added the reaction.</summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        /// <summary>Gets or sets owned JSON fields not modeled by this client.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalProperties { get; set; } = [];
    }
}
