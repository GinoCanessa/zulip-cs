using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Globalization;
using System.Text.Json;
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
        private Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Reminders class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Reminders(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all reminders.</summary>
        /// <remarks>Feature level 399: reminder listing/deletion endpoints were added.</remarks>
        /// <returns>An asynchronous result that yields (success, details, reminders).</returns>
        public async Task<(bool success, string? details, List<ReminderObject>? reminders)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, null);

            if (ResponsePayload.TryRead<List<ReminderObject>>(response, "Reminders.GetAll",
                "reminders", JsonValueKind.Array, out List<ReminderObject>? reminders, out string? details,
                array => ResponsePayload.ValidateObjectIds(array, "reminder_id") ??
                    ResponsePayload.ValidateObjectIds(array, "reminder_target_message_id")))
            {
                return (true, null, reminders);
            }

            return (false, details, null);
        }

        /// <summary>Gets all reminders (throwing version).</summary>
        public async Task<List<ReminderObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.reminders!;
        }

        /// <summary>Creates a reminder.</summary>
        /// <param name="messageId">The message ID to remind about.</param>
        /// <param name="scheduledDeliveryTimestamp">Unix timestamp for reminder.</param>
        /// <remarks>
        /// Feature level 415: reminder creation gained optional <c>note</c> support.
        /// This wrapper currently sends the core scheduling fields.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryCreate(int messageId, long scheduledDeliveryTimestamp)
        {
            var result = await TryCreateWithId(messageId, scheduledDeliveryTimestamp);
            return (result.success, result.details);
        }

        /// <summary>Creates a reminder and returns its deletion identity.</summary>
        /// <param name="messageId">The target message ID.</param>
        /// <param name="scheduledDeliveryTimestamp">UTC Unix delivery time.</param>
        /// <param name="note">The optional reminder note (feature level 415).</param>
        /// <returns>The Try outcome and reminder ID.</returns>
        public async Task<(bool success, string? details, int reminderId)> TryCreateWithId(
            int messageId, long scheduledDeliveryTimestamp, string? note = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "message_id", messageId.ToString(CultureInfo.InvariantCulture) },
                { "scheduled_delivery_timestamp", scheduledDeliveryTimestamp.ToString(CultureInfo.InvariantCulture) }
            };
            if (note != null) data.Add("note", note);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _endpoint, data);

            if (ResponsePayload.TryRead<int>(response, "Reminders.Create",
                "reminder_id", JsonValueKind.Number, out int reminderId, out string? details))
            {
                return (true, null, reminderId);
            }

            return (false, details, 0);
        }

        /// <summary>Creates a reminder and returns its ID, throwing on failure.</summary>
        /// <param name="messageId">The target message ID.</param>
        /// <param name="scheduledDeliveryTimestamp">UTC Unix delivery time.</param>
        /// <param name="note">The optional reminder note.</param>
        /// <returns>The reminder ID.</returns>
        public async Task<int> CreateWithId(int messageId, long scheduledDeliveryTimestamp, string? note = null)
        {
            var result = await TryCreateWithId(messageId, scheduledDeliveryTimestamp, note);
            if (!result.success) throw new Exception(result.details);
            return result.reminderId;
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
        public async Task<(bool success, string? details)> TryDelete(int reminderId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/{reminderId}", null);

            if (ResponsePayload.IsSuccessful(response))
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
