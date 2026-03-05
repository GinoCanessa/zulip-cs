using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a linkifier.</summary>
    public class LinkifierObject
    {
        /// <summary>Gets or sets the ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the pattern.</summary>
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; }

        /// <summary>Gets or sets the URL template.</summary>
        [JsonPropertyName("url_template")]
        public string UrlTemplate { get; set; }
    }

    /// <summary>Represents a custom emoji.</summary>
    public class EmojiObject
    {
        /// <summary>Gets or sets the ID.</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the source URL.</summary>
        [JsonPropertyName("source_url")]
        public string SourceUrl { get; set; }

        /// <summary>Gets or sets the author.</summary>
        [JsonPropertyName("author_id")]
        public int? AuthorId { get; set; }

        /// <summary>Gets or sets a value indicating whether this is deactivated.</summary>
        [JsonPropertyName("deactivated")]
        public bool? Deactivated { get; set; }
    }

    /// <summary>Represents a custom profile field.</summary>
    public class ProfileFieldObject
    {
        /// <summary>Gets or sets the ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the hint.</summary>
        [JsonPropertyName("hint")]
        public string Hint { get; set; }

        /// <summary>Gets or sets the type.</summary>
        [JsonPropertyName("type")]
        public int Type { get; set; }

        /// <summary>Gets or sets the order.</summary>
        [JsonPropertyName("order")]
        public int? Order { get; set; }
    }
}
