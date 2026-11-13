using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace zulip_cs_lib
{
    /// <summary>One flags-for-narrow update range; continuation is explicitly caller-owned (feature level 155).</summary>
    public sealed class NarrowFlagUpdateResult
    {
        /// <summary>Gets or sets the number of messages in the processed range.</summary>
        [JsonPropertyName("processed_count")]
        public int ProcessedCount { get; set; }

        /// <summary>Gets or sets the number of messages whose flag changed.</summary>
        [JsonPropertyName("updated_count")]
        public int UpdatedCount { get; set; }

        /// <summary>Gets or sets the oldest processed message ID, or null for an empty range.</summary>
        [JsonPropertyName("first_processed_id")]
        public ulong? FirstProcessedId { get; set; }

        /// <summary>Gets or sets the newest processed message ID, or null for an empty range.</summary>
        [JsonPropertyName("last_processed_id")]
        public ulong? LastProcessedId { get; set; }

        /// <summary>Gets or sets whether the range reached the oldest matching message.</summary>
        [JsonPropertyName("found_oldest")]
        public bool FoundOldest { get; set; }

        /// <summary>Gets or sets whether the range reached the newest matching message.</summary>
        [JsonPropertyName("found_newest")]
        public bool FoundNewest { get; set; }

        /// <summary>Gets or sets channels skipped when removing read from unsubscribed messages (feature level 355).</summary>
        [JsonPropertyName("ignored_because_not_subscribed_channels")]
        public List<int>? IgnoredBecauseNotSubscribedChannels { get; set; }

        /// <summary>Gets the request-local diagnostics and unsupported-parameter warnings.</summary>
        [JsonIgnore]
        public ZulipResponse Response { get; internal set; } = null!;
    }
}
