using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Reminders resource.</summary>
    public class Reminders
    {
        /// <summary>The API endpoint.</summary>
        private const string _endpoint = "api/v1/reminders";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Reminders class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Reminders(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all reminders.</summary>
        /// <remarks>Feature level 399: reminder listing/deletion endpoints were added.</remarks>
        /// <returns>An asynchronous result that yields (success, details, reminders).</returns>
        public async Task<(bool success, string details, List<ReminderObject> reminders)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Reminders ?? new List<ReminderObject>());
            }

            return (false, "Reminders.GetAll failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all reminders (throwing version).</summary>
        public async Task<List<ReminderObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.reminders;
        }

        /// <summary>Creates a reminder.</summary>
        /// <param name="messageId">The message ID to remind about.</param>
        /// <param name="scheduledDeliveryTimestamp">Unix timestamp for reminder.</param>
        /// <remarks>
        /// Feature level 415: reminder creation gained optional <c>note</c> support.
        /// This wrapper currently sends the core scheduling fields.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryCreate(int messageId, long scheduledDeliveryTimestamp)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "message_id", messageId.ToString() },
                { "scheduled_delivery_timestamp", scheduledDeliveryTimestamp.ToString() }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _endpoint, data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Reminders.Create failed: " + response.GetFailureMessage());
        }

        /// <summary>Creates a reminder (throwing version).</summary>
        public async Task Create(int messageId, long scheduledDeliveryTimestamp)
        {
            var result = await TryCreate(messageId, scheduledDeliveryTimestamp);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes a reminder.</summary>
        /// <param name="reminderId">The reminder ID.</param>
        /// <remarks>Feature level 399: reminder deletion endpoint was introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDelete(int reminderId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/{reminderId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Reminders.Delete failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes a reminder (throwing version).</summary>
        public async Task Delete(int reminderId)
        {
            var result = await TryDelete(reminderId);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
