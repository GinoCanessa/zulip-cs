using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Metadata for a file uploaded by the requesting user.</summary>
    public sealed class AttachmentObject
    {
        /// <summary>Gets or sets the attachment's deletion identity.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the uploaded filename.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the path beneath the server's /user_uploads/ directory.</summary>
        [JsonPropertyName("path_id")]
        public string PathId { get; set; } = string.Empty;

        /// <summary>Gets or sets the file size in bytes.</summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary>Gets or sets the upload's UNIX timestamp in seconds (feature level 443).</summary>
        [JsonPropertyName("create_time")]
        public long CreateTime { get; set; }

        /// <summary>Gets or sets referencing message IDs, replacing legacy messages at feature level 472.</summary>
        [JsonPropertyName("message_ids")]
        public List<int> MessageIds { get; set; } = [];
    }
}
