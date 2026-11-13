using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Personal topic visibility policies (feature level 170; followed requires 219).</summary>
    public enum TopicVisibilityPolicy
    {
        /// <summary>Remove the personal policy and inherit the channel's visibility.</summary>
        None = 0,
        /// <summary>Mute this topic.</summary>
        Muted = 1,
        /// <summary>Unmute this topic even when its channel is muted.</summary>
        Unmuted = 2,
        /// <summary>Follow this topic.</summary>
        Followed = 3
    }

    /// <summary>Subscription properties supported by the live feature-level-513 contract.</summary>
    public enum SubscriptionProperty
    {
        /// <summary>The channel's personal display color.</summary>
        Color,
        /// <summary>Whether the channel is muted.</summary>
        IsMuted,
        /// <summary>The legacy inverse of is_muted.</summary>
        InHomeView,
        /// <summary>Whether to pin the channel to the top of the channel list.</summary>
        PinToTop,
        /// <summary>Whether to show desktop notifications.</summary>
        DesktopNotifications,
        /// <summary>Whether to play sound notifications.</summary>
        AudibleNotifications,
        /// <summary>Whether to send mobile push notifications.</summary>
        PushNotifications,
        /// <summary>Whether to send email notifications.</summary>
        EmailNotifications,
        /// <summary>Whether wildcard mentions trigger notifications.</summary>
        WildcardMentionsNotify
    }

    /// <summary>A typed channel subscription property change for either the bulk or single-channel endpoint.</summary>
    public sealed class SubscriptionPropertyChange
    {
        /// <summary>Initializes a boolean property change.</summary>
        /// <param name="streamId">The channel ID.</param>
        /// <param name="property">A boolean property; Color requires the string constructor.</param>
        /// <param name="value">The new boolean value.</param>
        public SubscriptionPropertyChange(int streamId, SubscriptionProperty property, bool value)
        {
            StreamId = streamId;
            Property = property;
            Value = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>Initializes a display color change.</summary>
        /// <param name="streamId">The channel ID.</param>
        /// <param name="color">The new hex display color.</param>
        public SubscriptionPropertyChange(int streamId, string color)
        {
            StreamId = streamId;
            Property = SubscriptionProperty.Color;
            Value = JsonSerializer.SerializeToElement(color);
        }

        /// <summary>Gets the channel ID.</summary>
        [JsonPropertyName("stream_id")]
        public int StreamId { get; }

        /// <summary>Gets the typed property.</summary>
        [JsonIgnore]
        public SubscriptionProperty Property { get; }

        /// <summary>Gets the documented wire property name.</summary>
        [JsonPropertyName("property")]
        public string PropertyName => Property switch
        {
            SubscriptionProperty.Color => "color",
            SubscriptionProperty.IsMuted => "is_muted",
            SubscriptionProperty.InHomeView => "in_home_view",
            SubscriptionProperty.PinToTop => "pin_to_top",
            SubscriptionProperty.DesktopNotifications => "desktop_notifications",
            SubscriptionProperty.AudibleNotifications => "audible_notifications",
            SubscriptionProperty.PushNotifications => "push_notifications",
            SubscriptionProperty.EmailNotifications => "email_notifications",
            SubscriptionProperty.WildcardMentionsNotify => "wildcard_mentions_notify",
            _ => string.Empty
        };

        /// <summary>Gets the new value as an actual JSON boolean or string, never a quoted boolean.</summary>
        [JsonPropertyName("value")]
        public JsonElement Value { get; }

        /// <summary>Validates the ID, supported property and associated value before requesting.</summary>
        /// <returns>An input error, or null.</returns>
        internal string? Validate()
        {
            if (StreamId <= 0) return "stream_id must be a positive integer.";
            if (PropertyName.Length == 0) return "property must be a supported subscription property.";
            if (Property == SubscriptionProperty.Color)
            {
                if (Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(Value.GetString()))
                {
                    return "color must be a nonempty string.";
                }
            }
            else if (Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return "the subscription property requires a boolean value.";
            }
            return null;
        }
    }
}
