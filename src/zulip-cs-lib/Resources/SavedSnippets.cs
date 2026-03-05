using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Saved snippets resource.</summary>
    public class SavedSnippets
    {
        /// <summary>The API endpoint.</summary>
        private const string _endpoint = "api/v1/saved_snippets";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the SavedSnippets class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal SavedSnippets(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all saved snippets.</summary>
        /// <returns>An asynchronous result that yields (success, details, snippets).</returns>
        public async Task<(bool success, string details, List<SavedSnippetObject> snippets)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.SavedSnippets ?? new List<SavedSnippetObject>());
            }

            return (false, "SavedSnippets.GetAll failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all saved snippets (throwing version).</summary>
        public async Task<List<SavedSnippetObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.snippets;
        }

        /// <summary>Creates a saved snippet.</summary>
        /// <param name="title">The snippet title.</param>
        /// <param name="content">The snippet content.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryCreate(string title, string content)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "title", title },
                { "content", content }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _endpoint, data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "SavedSnippets.Create failed: " + response.GetFailureMessage());
        }

        /// <summary>Creates a saved snippet (throwing version).</summary>
        public async Task Create(string title, string content)
        {
            var result = await TryCreate(title, content);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Edits a saved snippet.</summary>
        /// <param name="snippetId">The snippet ID.</param>
        /// <param name="title">(Optional) New title.</param>
        /// <param name="content">(Optional) New content.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryEdit(int snippetId, string title = null, string content = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (title != null) data.Add("title", title);
            if (content != null) data.Add("content", content);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_endpoint}/{snippetId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "SavedSnippets.Edit failed: " + response.GetFailureMessage());
        }

        /// <summary>Edits a saved snippet (throwing version).</summary>
        public async Task Edit(int snippetId, string title = null, string content = null)
        {
            var result = await TryEdit(snippetId, title, content);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes a saved snippet.</summary>
        /// <param name="snippetId">The snippet ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDelete(int snippetId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/{snippetId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "SavedSnippets.Delete failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes a saved snippet (throwing version).</summary>
        public async Task Delete(int snippetId)
        {
            var result = await TryDelete(snippetId);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
