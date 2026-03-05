using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Navigation views resource.</summary>
    public class NavigationViews
    {
        /// <summary>The API endpoint.</summary>
        private const string _endpoint = "api/v1/navigation_views";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the NavigationViews class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal NavigationViews(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all navigation views.</summary>
        /// <remarks>
        /// Feature level 390: introduced navigation views APIs and corresponding event types.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details, views).</returns>
        public async Task<(bool success, string details, List<NavigationViewObject> views)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.NavigationViews ?? new List<NavigationViewObject>());
            }

            return (false, "NavigationViews.GetAll failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all navigation views (throwing version).</summary>
        public async Task<List<NavigationViewObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.views;
        }

        /// <summary>Creates a navigation view.</summary>
        /// <param name="name">The view name.</param>
        /// <remarks>Feature level 390: added the create navigation-view endpoint.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryCreate(string name)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "name", name }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _endpoint, data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "NavigationViews.Create failed: " + response.GetFailureMessage());
        }

        /// <summary>Creates a navigation view (throwing version).</summary>
        public async Task Create(string name)
        {
            var result = await TryCreate(name);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Updates a navigation view.</summary>
        /// <param name="viewId">The view ID.</param>
        /// <param name="name">(Optional) New name.</param>
        /// <remarks>Feature level 390: added the update navigation-view endpoint.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryUpdate(int viewId, string name = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (name != null) data.Add("name", name);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_endpoint}/{viewId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "NavigationViews.Update failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a navigation view (throwing version).</summary>
        public async Task Update(int viewId, string name = null)
        {
            var result = await TryUpdate(viewId, name);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes a navigation view.</summary>
        /// <param name="viewId">The view ID.</param>
        /// <remarks>Feature level 390: added the delete navigation-view endpoint.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDelete(int viewId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/{viewId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "NavigationViews.Delete failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes a navigation view (throwing version).</summary>
        public async Task Delete(int viewId)
        {
            var result = await TryDelete(viewId);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
