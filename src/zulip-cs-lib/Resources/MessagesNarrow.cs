using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>Modern range-based message flag operations.</summary>
    public partial class Messages
    {
        /// <summary>Updates flags within one narrow range, without automatic continuation (feature level 155).</summary>
        /// <param name="anchor">A message ID, newest, oldest or first_unread.</param>
        /// <param name="numBefore">Requested messages before the anchor; the server may reduce the range.</param>
        /// <param name="numAfter">Requested messages after the anchor; the server may reduce the range.</param>
        /// <param name="narrow">Required filters; an empty array selects all accessible messages.</param>
        /// <param name="operation">Whether to add or remove the flag.</param>
        /// <param name="flag">The flag name, including read, starred or hide_link_previews (feature level 510).</param>
        /// <param name="includeAnchor">Whether to include the anchor message.</param>
        /// <returns>Counts, range boundaries, cursors and diagnostics, or explicit failure details.</returns>
        public async Task<(bool success, string? details, NarrowFlagUpdateResult? update)> TryUpdateFlagsForNarrow(
            string anchor, int numBefore, int numAfter, Narrow[] narrow,
            FlagOperation operation, string flag, bool includeAnchor = true)
        {
            if (anchor is not ("newest" or "oldest" or "first_unread") &&
                !ulong.TryParse(anchor, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                return (false, "Messages.UpdateFlagsForNarrow failed: anchor must be a message ID, newest, oldest or first_unread.", null);
            }
            if (numBefore < 0 || numAfter < 0 || narrow == null || Array.Exists(narrow, item => item == null) ||
                string.IsNullOrWhiteSpace(flag) || operation is not (FlagOperation.Add or FlagOperation.Remove))
            {
                return (false, "Messages.UpdateFlagsForNarrow failed: nonnegative ranges, a narrow array, a flag and a valid operation are required.", null);
            }

            string narrowJson;
            try
            {
                narrowJson = Narrow.ToJsonArray(narrow);
            }
            catch (ArgumentException exception)
            {
                return (false, "Messages.UpdateFlagsForNarrow failed: " + exception.Message, null);
            }

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "anchor", anchor },
                { "num_before", numBefore.ToString(CultureInfo.InvariantCulture) },
                { "num_after", numAfter.ToString(CultureInfo.InvariantCulture) },
                { "narrow", narrowJson },
                { "op", operation == FlagOperation.Add ? "add" : "remove" },
                { "flag", flag },
                { "include_anchor", includeAnchor.ToString().ToLowerInvariant() }
            };
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_messageApiEndpoint}/flags/narrow", data);
            if (!ResponsePayload.TryRead<NarrowFlagUpdateResult>(response, "Messages.UpdateFlagsForNarrow",
                null, JsonValueKind.Object, out NarrowFlagUpdateResult? update, out string? details, ValidateNarrowFlagUpdate))
            {
                return (false, details, null);
            }
            update!.Response = response;
            return (true, null, update);
        }

        /// <summary>Updates one narrow range and returns explicit progress, throwing on failure.</summary>
        /// <param name="anchor">A message ID or supported symbolic anchor.</param>
        /// <param name="numBefore">Requested preceding messages.</param>
        /// <param name="numAfter">Requested following messages.</param>
        /// <param name="narrow">Required filters, possibly empty.</param>
        /// <param name="operation">The flag operation.</param>
        /// <param name="flag">The flag name.</param>
        /// <param name="includeAnchor">Whether to include the anchor.</param>
        /// <returns>One request's counts, boundaries, cursors and diagnostics.</returns>
        public async Task<NarrowFlagUpdateResult> UpdateFlagsForNarrow(
            string anchor, int numBefore, int numAfter, Narrow[] narrow,
            FlagOperation operation, string flag, bool includeAnchor = true)
        {
            var result = await TryUpdateFlagsForNarrow(anchor, numBefore, numAfter, narrow, operation, flag, includeAnchor);
            if (!result.success) throw new Exception(result.details);
            return result.update!;
        }

        /// <summary>Validates required range result fields while retaining valid null cursors for empty ranges.</summary>
        /// <param name="root">The returned range result.</param>
        /// <returns>An error or null.</returns>
        private static string? ValidateNarrowFlagUpdate(JsonElement root)
        {
            foreach (string field in new[] { "processed_count", "updated_count" })
            {
                if (!root.TryGetProperty(field, out JsonElement value) ||
                    value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out _))
                    return $"required field '{field}' must be an integer.";
            }
            foreach (string field in new[] { "first_processed_id", "last_processed_id" })
            {
                if (!root.TryGetProperty(field, out JsonElement value) ||
                    (value.ValueKind != JsonValueKind.Null &&
                        (value.ValueKind != JsonValueKind.Number || !value.TryGetUInt64(out _))))
                    return $"required field '{field}' must be an integer or null.";
            }
            foreach (string field in new[] { "found_oldest", "found_newest" })
            {
                if (!root.TryGetProperty(field, out JsonElement value) ||
                    value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return $"required field '{field}' must be a boolean.";
            }
            return null;
        }
    }
}
