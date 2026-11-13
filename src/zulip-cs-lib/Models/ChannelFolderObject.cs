using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>A channel folder returned by the channel-folder list endpoint (feature level 389).</summary>
    public sealed class ChannelFolderObject
    {
        /// <summary>Gets or sets the folder identity, returned as id rather than channel_folder_id in list responses.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the folder name.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the zero-indexed display order, available from feature level 414.</summary>
        [JsonPropertyName("order")]
        public int? Order { get; set; }

        /// <summary>Gets or sets the creation timestamp in UTC seconds, or null when unknown.</summary>
        [JsonPropertyName("date_created")]
        public long? DateCreated { get; set; }

        /// <summary>Gets or sets the creating user ID, or null when unknown.</summary>
        [JsonPropertyName("creator_id")]
        public int? CreatorId { get; set; }

        /// <summary>Gets or sets the description, including an empty description.</summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>Gets or sets the server-rendered description HTML.</summary>
        [JsonPropertyName("rendered_description")]
        public string? RenderedDescription { get; set; }

        /// <summary>Gets or sets whether the folder is archived.</summary>
        [JsonPropertyName("is_archived")]
        public bool IsArchived { get; set; }
    }
}
