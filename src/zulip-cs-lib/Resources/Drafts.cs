using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Drafts resource.</summary>
    public class Drafts
    {
        /// <summary>The API endpoint.</summary>
        private const string _endpoint = "api/v1/drafts";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Drafts class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Drafts(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all drafts.</summary>
        /// <remarks>Feature level 87: drafts APIs (list/create/edit/delete) were introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details, drafts).</returns>
        public async Task<(bool success, string details, List<DraftObject> drafts)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Drafts ?? new List<DraftObject>());
            }

            return (false, "Drafts.GetAll failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all drafts (throwing version).</summary>
        public async Task<List<DraftObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.drafts;
        }

        /// <summary>Creates drafts.</summary>
        /// <param name="draftsJson">JSON array of draft objects.</param>
        /// <remarks>Feature level 87: drafts creation endpoint was introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryCreate(string draftsJson)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "drafts", draftsJson }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _endpoint, data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Drafts.Create failed: " + response.GetFailureMessage());
        }

        /// <summary>Creates drafts (throwing version).</summary>
        public async Task Create(string draftsJson)
        {
            var result = await TryCreate(draftsJson);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Edits a draft.</summary>
        /// <param name="draftId">The draft ID.</param>
        /// <param name="draftJson">JSON representation of the draft.</param>
        /// <remarks>Feature level 87: draft edit endpoint was introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryEdit(int draftId, string draftJson)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "draft", draftJson }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_endpoint}/{draftId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Drafts.Edit failed: " + response.GetFailureMessage());
        }

        /// <summary>Edits a draft (throwing version).</summary>
        public async Task Edit(int draftId, string draftJson)
        {
            var result = await TryEdit(draftId, draftJson);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes a draft.</summary>
        /// <param name="draftId">The draft ID.</param>
        /// <remarks>Feature level 87: draft delete endpoint was introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDelete(int draftId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/{draftId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Drafts.Delete failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes a draft (throwing version).</summary>
        public async Task Delete(int draftId)
        {
            var result = await TryDelete(draftId);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
