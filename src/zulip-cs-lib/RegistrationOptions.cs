using System.Collections.Generic;
using System.Text.Json;

namespace zulip_cs_lib
{
    /// <summary>Optional parameters for registering an event queue and fetching its initial state.</summary>
    /// <remarks>Omitted values use server defaults. Inspect response warnings for unsupported parameters.</remarks>
    public sealed class RegistrationOptions
    {
        /// <summary>Gets or sets event types to receive. Unknown server event types are ignored.</summary>
        public IReadOnlyList<string>? EventTypes { get; set; }

        /// <summary>Gets or sets initial-state event types, independently of the queue's event filter.</summary>
        public IReadOnlyList<string>? FetchEventTypes { get; set; }

        /// <summary>Gets or sets the narrow filter as a JSON array.</summary>
        public string? Narrow { get; set; }

        /// <summary>Gets or sets whether to include events from all public channels.</summary>
        public bool? AllPublicStreams { get; set; }

        /// <summary>Gets or sets named boolean capabilities, including bulk_message_deletion and empty_topic_name.</summary>
        /// <remarks>Capability names are not restricted to those known by this library.</remarks>
        public IReadOnlyDictionary<string, bool>? ClientCapabilities { get; set; }

        /// <summary>Gets or sets whether the client can compute Gravatar URLs.</summary>
        public bool? ClientGravatar { get; set; }

        /// <summary>Gets or sets subscriber inclusion: true, false or partial (string values).</summary>
        public string? IncludeSubscribers { get; set; }

        /// <summary>Gets or sets whether initial presences use the modern user-ID-keyed format.</summary>
        public bool? SlimPresence { get; set; }

        /// <summary>Gets or sets whether applicable content is rendered as HTML rather than Markdown.</summary>
        public bool? ApplyMarkdown { get; set; }

        /// <summary>Gets or sets the idle queue timeout as a JSON integer of at least 1 second or the JSON string "mobile".</summary>
        /// <remarks>
        /// New at feature level 481. The server applies defaults and limits; use the returned IdleQueueTimeoutSeconds.
        /// This does not configure the HTTP timeout or make event polling nonblocking.
        /// </remarks>
        public JsonElement? IdleQueueTimeout { get; set; }
    }
}
