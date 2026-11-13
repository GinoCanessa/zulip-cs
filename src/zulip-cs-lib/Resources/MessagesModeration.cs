using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>Message reporting to an organization's configured moderation-request channel.</summary>
    public partial class Messages
    {
        /// <summary>Reports a message once for moderation (FL382).</summary>
        /// <remarks>Use a report type advertised by server_report_message_types (FL435). Delivery errors are reported by the server at FL511.</remarks>
        /// <param name="messageId">The target message identity.</param>
        /// <param name="reportType">A server-advertised report type, not a hard-coded enum.</param>
        /// <param name="description">Optional context, required and nonempty for other; at most 1000 Unicode code points.</param>
        /// <returns>Success or explicit failure; no automatic retry occurs.</returns>
        public async Task<(bool success, string? details)> TryReport(
            ulong messageId, string reportType, string? description = null)
        {
            if (messageId == 0 || string.IsNullOrWhiteSpace(reportType) ||
                (reportType == "other" && string.IsNullOrEmpty(description)) ||
                (description != null && description.EnumerateRunes().Count() > 1000))
                return (false, "Messages.Report failed: a positive messageId and reportType are required; other requires a nonempty description, limited to 1000 Unicode code points.");
            Dictionary<string, string> data = new Dictionary<string, string> { { "report_type", reportType } };
            if (description != null) data.Add("description", description);
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post,
                "api/v1/messages/" + messageId.ToString(CultureInfo.InvariantCulture) + "/report", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "Messages.Report failed: " + response.GetFailureMessage());
        }

        /// <summary>Reports a message for moderation, throwing on failure (FL382).</summary>
        /// <param name="messageId">The target message.</param>
        /// <param name="reportType">A server-advertised report type.</param>
        /// <param name="description">Optional context, required for other, at most 1000 Unicode code points.</param>
        public async Task Report(ulong messageId, string reportType, string? description = null)
        {
            var result = await TryReport(messageId, reportType, description);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
