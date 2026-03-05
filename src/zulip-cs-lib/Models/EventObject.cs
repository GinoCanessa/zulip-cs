using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip event.</summary>
    /// <remarks>
    /// Feature levels 390 and 468 added new event types and payload variations (for example navigation-view and device-related events).
    /// </remarks>
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
