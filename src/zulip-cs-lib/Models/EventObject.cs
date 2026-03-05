using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip event.</summary>
    public class EventObject
    {
        /// <summary>Gets or sets the event ID.</summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }

        /// <summary>Gets or sets the event type.</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
