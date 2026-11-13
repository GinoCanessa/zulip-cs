using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a Zulip subscription.</summary>
    /// <remarks>
    /// Inherits channel metadata and permission settings, adding the user's subscription preferences.
    /// Nullable notification preferences inherit their user-level defaults when null.
    /// </remarks>
    public class SubscriptionObject : StreamObject
    {
        /// <summary>Gets or sets the color.</summary>
        [JsonPropertyName("color")]
        public string? Color { get; set; }

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
        public string? EmailAddress { get; set; }

        /// <summary>Gets or sets whether wildcard mentions trigger personal-mention notifications.</summary>
        [JsonPropertyName("wildcard_mentions_notify")]
        public bool? WildcardMentionsNotify { get; set; }

        /// <summary>Gets or sets all subscriber user IDs when requested.</summary>
        [JsonPropertyName("subscribers")]
        public List<int>? Subscribers { get; set; }

        /// <summary>Gets or sets a server-selected subset of subscriber IDs when partial data was requested.</summary>
        /// <remarks>This is not the full membership list; it includes bots and recently active users.</remarks>
        [JsonPropertyName("partial_subscribers")]
        public List<int>? PartialSubscribers { get; set; }
    }
}
