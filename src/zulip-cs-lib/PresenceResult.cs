using System.Collections.Generic;
using System.Text.Json;
using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>Presence data with the server's clock and request-local diagnostics.</summary>
    public sealed class PresenceResult
    {
        /// <summary>Initializes a presence result.</summary>
        /// <param name="presence">The modern and optional legacy presence data.</param>
        /// <param name="serverTimestamp">The server's UNIX timestamp, absent on older servers.</param>
        /// <param name="response">The request-local response and warnings.</param>
        internal PresenceResult(PresenceData presence, double? serverTimestamp, ZulipResponse response)
        {
            Presence = presence;
            ServerTimestamp = serverTimestamp;
            Response = response;
        }

        /// <summary>Gets modern timestamps and optional legacy entries. No status is synthesized.</summary>
        public PresenceData Presence { get; }

        /// <summary>Gets the server's UNIX timestamp, when supplied.</summary>
        public double? ServerTimestamp { get; }

        /// <summary>Gets the response for this request, including the owned payload and unknown fields.</summary>
        public ZulipResponse Response { get; }

        /// <summary>Gets nonfatal unsupported-parameter warnings for this request.</summary>
        public IReadOnlyList<string> IgnoredParametersUnsupported => Response.IgnoredParametersUnsupported ?? [];

        /// <summary>Projects only actual legacy entries from the parsed endpoint DTO.</summary>
        /// <returns>The legacy entries, or null when the payload is modern-only.</returns>
        internal Dictionary<string, PresenceInfo>? GetLegacyPresence()
        {
            Dictionary<string, PresenceInfo> legacy = [];
            if (Presence.Website != null) legacy.Add("website", Presence.Website);
            if (Presence.Aggregated != null) legacy.Add("aggregated", Presence.Aggregated);
            if (Presence.ExtensionData != null)
            {
                foreach (KeyValuePair<string, JsonElement> entry in Presence.ExtensionData)
                {
                    if (entry.Value.ValueKind == JsonValueKind.Object &&
                        entry.Value.TryGetProperty("status", out _) &&
                        entry.Value.TryGetProperty("timestamp", out _))
                    {
                        legacy.Add(entry.Key, entry.Value.Deserialize<PresenceInfo>()!);
                    }
                }
            }

            return legacy.Count == 0 ? null : legacy;
        }
    }
}
