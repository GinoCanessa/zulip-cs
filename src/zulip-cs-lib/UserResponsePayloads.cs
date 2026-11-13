using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>The group-members endpoint's integer members envelope, distinct from the users list.</summary>
    internal sealed class GroupMembersPayload
    {
        /// <summary>Gets or sets the actual member IDs.</summary>
        [JsonPropertyName("members")]
        public List<int>? Members { get; set; }
    }

    /// <summary>The user-presence endpoint's modern and legacy data envelope.</summary>
    internal sealed class UserPresencePayload
    {
        /// <summary>Gets or sets the user's presence.</summary>
        [JsonPropertyName("presence")]
        public PresenceData? Presence { get; set; }

        /// <summary>Gets or sets the optional server UNIX timestamp.</summary>
        [JsonPropertyName("server_timestamp")]
        public double? ServerTimestamp { get; set; }

        /// <summary>Validates meaningful modern or legacy presence without inventing a status.</summary>
        /// <param name="root">The response root.</param>
        /// <returns>A validation error, or null.</returns>
        internal static string? Validate(JsonElement root)
        {
            if (!root.TryGetProperty("presence", out JsonElement presence) ||
                presence.ValueKind != JsonValueKind.Object)
            {
                return "required field 'presence' must be Object.";
            }

            if (root.TryGetProperty("server_timestamp", out JsonElement serverTimestamp) &&
                (serverTimestamp.ValueKind != JsonValueKind.Number ||
                    !serverTimestamp.TryGetDouble(out double timestampValue) || !double.IsFinite(timestampValue)))
            {
                return "presence field 'server_timestamp' must be Number.";
            }

            return ValidateData(presence);
        }

        /// <summary>Validates one presence entry without inventing active/idle or legacy status values.</summary>
        /// <param name="presence">The user's presence entry.</param>
        /// <returns>A validation error, or null.</returns>
        internal static string? ValidateData(JsonElement presence)
        {
            if (presence.ValueKind != JsonValueKind.Object)
            {
                return "presence entry must be Object.";
            }
            bool hasData = false;
            foreach (JsonProperty property in presence.EnumerateObject())
            {
                if (property.Name is "active_timestamp" or "idle_timestamp")
                {
                    if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt64(out _))
                    {
                        return $"presence field '{property.Name}' must be an integer.";
                    }

                    hasData = true;
                }
                else if (property.Name is "website" or "aggregated" ||
                    (property.Value.ValueKind == JsonValueKind.Object &&
                        property.Value.TryGetProperty("status", out _) &&
                        property.Value.TryGetProperty("timestamp", out _)))
                {
                    if (property.Value.ValueKind != JsonValueKind.Object ||
                        !property.Value.TryGetProperty("status", out JsonElement status) ||
                        status.ValueKind != JsonValueKind.String ||
                        !property.Value.TryGetProperty("timestamp", out JsonElement timestamp) ||
                        timestamp.ValueKind != JsonValueKind.Number || !timestamp.TryGetInt64(out _))
                    {
                        return $"legacy presence field '{property.Name}' requires a string status and integer timestamp.";
                    }

                    hasData = true;
                }
            }

            return hasData ? null : "required field 'presence' contains no modern timestamps or legacy presence entries.";
        }
    }
}
