using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Represents a saved snippet.</summary>
    public class SavedSnippetObject
    {
        /// <summary>Gets or sets the ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the title.</summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>Gets or sets the content.</summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>Gets or sets the date created.</summary>
        [JsonPropertyName("date_created")]
        public long? DateCreated { get; set; }
    }

    /// <summary>Represents a reminder.</summary>
    public class ReminderObject
    {
        /// <summary>Gets or sets the reminder ID.</summary>
        [JsonPropertyName("reminder_id")]
        public int ReminderId { get; set; }

        /// <summary>Gets or sets the message ID.</summary>
        [JsonPropertyName("reminder_target_message_id")]
        public ulong MessageId { get; set; }

        /// <summary>Gets or sets the scheduled delivery timestamp.</summary>
        [JsonPropertyName("scheduled_delivery_timestamp")]
        public long ScheduledDeliveryTimestamp { get; set; }

        /// <summary>Gets or sets the reminder message type.</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>Gets or sets the reminder's recipient IDs.</summary>
        [JsonPropertyName("to")]
        public List<int>? To { get; set; }

        /// <summary>Gets or sets the reminder's Markdown content.</summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>Gets or sets the rendered reminder content.</summary>
        [JsonPropertyName("rendered_content")]
        public string? RenderedContent { get; set; }

        /// <summary>Gets or sets whether delivery has failed.</summary>
        [JsonPropertyName("failed")]
        public bool? Failed { get; set; }

        /// <summary>Gets or sets unmodeled reminder fields.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    /// <summary>Represents a navigation view.</summary>
    public class NavigationViewObject
    {
        /// <summary>Gets or sets the obsolete numeric identity; navigation views are keyed by Fragment.</summary>
        [Obsolete("Navigation views are identified by Fragment, not numeric IDs.")]
        [JsonIgnore]
        public int Id { get; set; }

        /// <summary>Gets or sets the unique URL fragment used for navigation and identity.</summary>
        [JsonPropertyName("fragment")]
        public string? Fragment { get; set; }

        /// <summary>Gets or sets whether this view is pinned in the sidebar.</summary>
        [JsonPropertyName("is_pinned")]
        public bool? IsPinned { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the type.</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>Gets or sets unmodeled navigation-view fields.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }
}
