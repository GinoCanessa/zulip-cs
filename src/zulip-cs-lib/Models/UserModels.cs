using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a user group.</summary>
    public class UserGroupObject
    {
        /// <summary>Gets or sets the ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>Gets or sets the members.</summary>
        [JsonPropertyName("members")]
        public List<int> Members { get; set; }
    }

    /// <summary>Represents a user status.</summary>
    public class UserStatusObject
    {
        /// <summary>Gets or sets the status text.</summary>
        [JsonPropertyName("status_text")]
        public string StatusText { get; set; }

        /// <summary>Gets or sets the emoji name.</summary>
        [JsonPropertyName("emoji_name")]
        public string EmojiName { get; set; }

        /// <summary>Gets or sets the emoji code.</summary>
        [JsonPropertyName("emoji_code")]
        public string EmojiCode { get; set; }

        /// <summary>Gets or sets the reaction type.</summary>
        [JsonPropertyName("reaction_type")]
        public string ReactionType { get; set; }

        /// <summary>Gets or sets whether the user is away.</summary>
        [JsonPropertyName("away")]
        public bool? Away { get; set; }
    }

    /// <summary>Represents presence info.</summary>
    public class PresenceInfo
    {
        /// <summary>Gets or sets the status string.</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [JsonPropertyName("timestamp")]
        public long? Timestamp { get; set; }

        /// <summary>Gets or sets the client.</summary>
        [JsonPropertyName("client")]
        public string Client { get; set; }
    }
}
