using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Invitations resource.</summary>
    public class Invitations
    {
        /// <summary>The API endpoint.</summary>
        private const string _endpoint = "api/v1/invites";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Invitations class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Invitations(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all invitations.</summary>
        /// <returns>An asynchronous result that yields (success, details, invites).</returns>
        public async Task<(bool success, string details, List<InviteObject> invites)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Invites ?? new List<InviteObject>());
            }

            return (false, "Invitations.GetAll failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all invitations (throwing version).</summary>
        public async Task<List<InviteObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.invites;
        }

        /// <summary>Sends invitations.</summary>
        /// <param name="inviteeEmails">Comma-separated emails.</param>
        /// <param name="streamIds">JSON array of stream IDs.</param>
        /// <param name="inviteAs">(Optional) Role for invitees.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TrySend(
            string inviteeEmails,
            string streamIds,
            int? inviteAs = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "invitee_emails", inviteeEmails },
                { "stream_ids", streamIds }
            };

            if (inviteAs != null) data.Add("invite_as", inviteAs.Value.ToString());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _endpoint, data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Invitations.Send failed: " + response.GetFailureMessage());
        }

        /// <summary>Sends invitations (throwing version).</summary>
        public async Task Send(string inviteeEmails, string streamIds, int? inviteAs = null)
        {
            var result = await TrySend(inviteeEmails, streamIds, inviteAs);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Creates a reusable invite link.</summary>
        /// <param name="streamIds">JSON array of stream IDs.</param>
        /// <param name="inviteAs">(Optional) Role for invitees.</param>
        /// <returns>An asynchronous result that yields (success, details, linkUrl).</returns>
        public async Task<(bool success, string details, string linkUrl)> TryCreateLink(
            string streamIds,
            int? inviteAs = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream_ids", streamIds },
                { "invite_as", (inviteAs ?? 400).ToString() }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_endpoint}/multiuse", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.InviteLinkUrl);
            }

            return (false, "Invitations.CreateLink failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Creates a reusable invite link (throwing version).</summary>
        public async Task<string> CreateLink(string streamIds, int? inviteAs = null)
        {
            var result = await TryCreateLink(streamIds, inviteAs);
            if (!result.success) throw new Exception(result.details);
            return result.linkUrl;
        }

        /// <summary>Resends an invitation.</summary>
        /// <param name="inviteId">The invitation ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryResend(int inviteId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_endpoint}/{inviteId}/resend", new Dictionary<string, string>());

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Invitations.Resend failed: " + response.GetFailureMessage());
        }

        /// <summary>Resends an invitation (throwing version).</summary>
        public async Task Resend(int inviteId)
        {
            var result = await TryResend(inviteId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Revokes an invitation.</summary>
        /// <param name="inviteId">The invitation ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryRevoke(int inviteId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/{inviteId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Invitations.Revoke failed: " + response.GetFailureMessage());
        }

        /// <summary>Revokes an invitation (throwing version).</summary>
        public async Task Revoke(int inviteId)
        {
            var result = await TryRevoke(inviteId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Revokes a multiuse invite link.</summary>
        /// <param name="inviteId">The multiuse invite ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryRevokeLink(int inviteId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/multiuse/{inviteId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Invitations.RevokeLink failed: " + response.GetFailureMessage());
        }

        /// <summary>Revokes a multiuse invite link (throwing version).</summary>
        public async Task RevokeLink(int inviteId)
        {
            var result = await TryRevokeLink(inviteId);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
