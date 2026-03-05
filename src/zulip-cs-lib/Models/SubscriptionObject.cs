using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip subscription.</summary>
    /// <remarks>
    /// Feature levels 392, 404, and 441 expanded subscription objects with newer policy and group-setting fields.
    /// This model keeps a stable subset of commonly consumed subscription properties.
    /// </remarks>
    public class SubscriptionObject
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

        /// <summary>Gets or sets the color.</summary>
        [JsonPropertyName("color")]
        public string Color { get; set; }

        /// <summary>Gets or sets a value indicating whether this is invite only.</summary>
        [JsonPropertyName("invite_only")]
        public bool? InviteOnly { get; set; }

        /// <summary>Gets or sets the pin to top flag.</summary>
        [JsonPropertyName("pin_to_top")]
        public bool? PinToTop { get; set; }

        /// <summary>Gets or sets the desktop notifications flag.</summary>
        [JsonPropertyName("desktop_notifications")]
        public bool? DesktopNotifications { get; set; }

        /// <summary>Gets or sets the audible notifications flag.</summary>
        [JsonPropertyName("audible_notifications")]
        public bool? AudibleNotifications { get; set; }

        /// <summary>Gets or sets the push notifications flag.</summary>
        [JsonPropertyName("push_notifications")]
        public bool? PushNotifications { get; set; }

        /// <summary>Gets or sets the email notifications flag.</summary>
        [JsonPropertyName("email_notifications")]
        public bool? EmailNotifications { get; set; }

        /// <summary>Gets or sets a value indicating whether the stream is muted.</summary>
        [JsonPropertyName("is_muted")]
        public bool? IsMuted { get; set; }

        /// <summary>Gets or sets the email address.</summary>
        [JsonPropertyName("email_address")]
        public string EmailAddress { get; set; }
    }
}
