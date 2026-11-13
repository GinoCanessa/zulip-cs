using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>Personal alert-word, presence and message-edit typing workflows.</summary>
    public partial class Users
    {
        /// <summary>Values a client can publish as its user's presence.</summary>
        public enum PresenceStatus
        {
            /// <summary>The user is present at this device.</summary>
            Active,
            /// <summary>The client is connected but the user is not known to be present.</summary>
            Idle
        }

        /// <summary>Removes alert words and returns the remaining configured words.</summary>
        /// <param name="words">Case-insensitive words or phrases to remove.</param>
        /// <returns>The remaining alert words, or explicit failure details.</returns>
        public async Task<(bool success, string? details, List<string>? words)> TryRemoveAlertWords(string[] words)
        {
            if (words == null || Array.Exists(words, word => word == null))
                return (false, "Users.RemoveAlertWords failed: an array of words is required.", null);
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, "api/v1/users/me/alert_words",
                new Dictionary<string, string> { { "alert_words", JsonSerializer.Serialize(words) } });
            if (!ResponsePayload.TryRead<List<string>>(response, "Users.RemoveAlertWords", "alert_words", JsonValueKind.Array,
                out List<string>? remaining, out string? details,
                array => ResponsePayload.ValidateElements(array, JsonValueKind.String)))
                return (false, details, null);
            return (true, null, remaining);
        }

        /// <summary>Removes alert words and returns remaining words, throwing on failure.</summary>
        /// <param name="words">Words or phrases to remove.</param>
        /// <returns>The remaining configured alert words.</returns>
        public async Task<List<string>> RemoveAlertWords(string[] words)
        {
            var result = await TryRemoveAlertWords(words);
            if (!result.success) throw new Exception(result.details);
            return result.words!;
        }

        /// <summary>Publishes this client's presence using the incremental protocol (feature level 263).</summary>
        /// <param name="status">Active or idle at this device.</param>
        /// <param name="lastUpdateId">Use -1 initially, then the last returned presence revision.</param>
        /// <param name="pingOnly">Only publish presence; do not fetch other users' presence.</param>
        /// <param name="newUserInput">Whether the user actually interacted with the client since the preceding request.</param>
        /// <param name="historyLimitDays">Optional history window in days (feature level 288).</param>
        /// <returns>The revision, optional presence/clock and diagnostics, or explicit failure details.</returns>
        public async Task<(bool success, string? details, RealmPresenceResult? presence)> TryUpdateOwnPresence(
            PresenceStatus status, long lastUpdateId = -1, bool pingOnly = false,
            bool newUserInput = false, int? historyLimitDays = null)
        {
            if (status is not (PresenceStatus.Active or PresenceStatus.Idle) ||
                lastUpdateId < -1 || historyLimitDays < 0)
                return (false, "Users.UpdateOwnPresence failed: a valid status, revision >= -1 and nonnegative history window are required.", null);
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "status", status == PresenceStatus.Active ? "active" : "idle" },
                { "last_update_id", lastUpdateId.ToString(CultureInfo.InvariantCulture) },
                { "ping_only", pingOnly.ToString().ToLowerInvariant() },
                { "new_user_input", newUserInput.ToString().ToLowerInvariant() }
            };
            if (historyLimitDays is int days) data.Add("history_limit_days", days.ToString(CultureInfo.InvariantCulture));
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/users/me/presence", data);
            return ReadRealmPresence(response, "Users.UpdateOwnPresence", !pingOnly, true);
        }

        /// <summary>Publishes presence and returns incremental state, throwing on failure (feature level 263).</summary>
        /// <param name="status">Active or idle.</param>
        /// <param name="lastUpdateId">The last revision, or -1 for initialization.</param>
        /// <param name="pingOnly">Whether to omit presence fetching.</param>
        /// <param name="newUserInput">Whether the user interacted with this client.</param>
        /// <param name="historyLimitDays">Optional history window (feature level 288).</param>
        /// <returns>The request-local revision, presence/clock and diagnostics.</returns>
        public async Task<RealmPresenceResult> UpdateOwnPresence(
            PresenceStatus status, long lastUpdateId = -1, bool pingOnly = false,
            bool newUserInput = false, int? historyLimitDays = null)
        {
            var result = await TryUpdateOwnPresence(status, lastUpdateId, pingOnly, newUserInput, historyLimitDays);
            if (!result.success) throw new Exception(result.details);
            return result.presence!;
        }

        /// <summary>Gets organization presence using the endpoint's legacy email-keyed format.</summary>
        /// <returns>Presence, the server clock and diagnostics, or explicit failure details.</returns>
        public async Task<(bool success, string? details, RealmPresenceResult? presence)> TryGetRealmPresence()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/realm/presence", null);
            return ReadRealmPresence(response, "Users.GetRealmPresence", true, false);
        }

        /// <summary>Gets organization presence and its server clock, throwing on failure.</summary>
        /// <returns>The request-local presence result.</returns>
        public async Task<RealmPresenceResult> GetRealmPresence()
        {
            var result = await TryGetRealmPresence();
            if (!result.success) throw new Exception(result.details);
            return result.presence!;
        }

        /// <summary>Publishes whether this user is typing while editing a message (feature level 361).</summary>
        /// <param name="messageId">The edited message ID.</param>
        /// <param name="isTyping">True for start, false for stop.</param>
        /// <returns>Success or explicit failure details.</returns>
        public async Task<(bool success, string? details)> TrySetMessageEditTyping(ulong messageId, bool isTyping)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post,
                $"api/v1/messages/{messageId.ToString(CultureInfo.InvariantCulture)}/typing",
                new Dictionary<string, string> { { "op", isTyping ? "start" : "stop" } });
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "Users.SetMessageEditTyping failed: " + response.GetFailureMessage());
        }

        /// <summary>Publishes message-edit typing state, throwing on failure (feature level 361).</summary>
        /// <param name="messageId">The edited message ID.</param>
        /// <param name="isTyping">True for start, false for stop.</param>
        /// <returns>The completed request.</returns>
        public async Task SetMessageEditTyping(ulong messageId, bool isTyping)
        {
            var result = await TrySetMessageEditTyping(messageId, isTyping);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Reads presence response fields with endpoint-specific required data and revision semantics.</summary>
        /// <param name="response">The completed response.</param>
        /// <param name="operation">The operation used in failure details.</param>
        /// <param name="requireData">Whether presence/clock must be returned.</param>
        /// <param name="requireRevision">Whether the incremental revision is required.</param>
        /// <returns>The parsed result, or explicit failure details.</returns>
        private static (bool success, string? details, RealmPresenceResult? presence) ReadRealmPresence(
            ZulipResponse response, string operation, bool requireData, bool requireRevision)
        {
            if (!ResponsePayload.TryRead<RealmPresenceResult>(response, operation, null, JsonValueKind.Object,
                out RealmPresenceResult? presence, out string? details, root =>
                {
                    if (requireRevision && (!root.TryGetProperty("presence_last_update_id", out JsonElement revision) ||
                        revision.ValueKind != JsonValueKind.Number || !revision.TryGetInt64(out _)))
                        return "required field 'presence_last_update_id' must be an integer.";
                    if (requireData)
                    {
                        if (!root.TryGetProperty("server_timestamp", out JsonElement clock) ||
                            clock.ValueKind != JsonValueKind.Number || !clock.TryGetDouble(out double timestamp) || !double.IsFinite(timestamp))
                            return "required field 'server_timestamp' must be a finite number.";
                        if (!root.TryGetProperty("presences", out JsonElement users) || users.ValueKind != JsonValueKind.Object)
                            return "required field 'presences' must be Object.";
                        foreach (JsonProperty user in users.EnumerateObject())
                        {
                            string? error = UserPresencePayload.ValidateData(user.Value);
                            if (error != null) return error;
                        }
                    }
                    return null;
                }))
                return (false, details, null);
            presence!.Response = response;
            return (true, null, presence);
        }
    }
}
