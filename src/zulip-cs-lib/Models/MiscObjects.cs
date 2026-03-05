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
        public string Title { get; set; }

        /// <summary>Gets or sets the content.</summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }

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
        [JsonPropertyName("message_id")]
        public ulong MessageId { get; set; }

        /// <summary>Gets or sets the scheduled delivery timestamp.</summary>
        [JsonPropertyName("scheduled_delivery_timestamp")]
        public long ScheduledDeliveryTimestamp { get; set; }
    }

    /// <summary>Represents a navigation view.</summary>
    public class NavigationViewObject
    {
        /// <summary>Gets or sets the ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the type.</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
