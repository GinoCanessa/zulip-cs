using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
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
        private Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the NavigationViews class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal NavigationViews(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all navigation views.</summary>
        /// <remarks>
        /// Feature level 390: introduced navigation views APIs and corresponding event types.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details, views).</returns>
        public async Task<(bool success, string? details, List<NavigationViewObject>? views)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, null);

            if (ResponsePayload.TryRead<List<NavigationViewObject>>(response, "NavigationViews.GetAll",
                "navigation_views", JsonValueKind.Array, out List<NavigationViewObject>? views, out string? details,
                ValidateViews))
            {
                return (true, null, views);
            }

            return (false, details, null);
        }

        /// <summary>Gets all navigation views (throwing version).</summary>
        public async Task<List<NavigationViewObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.views!;
        }

        /// <summary>Creates a navigation view.</summary>
        /// <param name="name">The view name.</param>
        /// <remarks>Feature level 390: added the create navigation-view endpoint.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        [Obsolete("Use TryCreate(fragment, isPinned, name); a name alone cannot identify a navigation view.")]
        public Task<(bool success, string? details)> TryCreate(string name)
        {
            return Task.FromResult<(bool success, string? details)>((false,
                "NavigationViews.Create failed: Use TryCreate(fragment, isPinned, name); a name alone cannot identify a view."));
        }

        /// <summary>Creates a navigation view identified by its URL fragment.</summary>
        /// <param name="fragment">The unique URL fragment.</param>
        /// <param name="isPinned">Whether the view is pinned.</param>
        /// <param name="name">The optional custom display name; omit for built-in views.</param>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryCreate(string fragment, bool isPinned, string? name = null)
        {
            if (!TryEncodeFragment(fragment, out _))
            {
                return (false, "NavigationViews.Create failed: fragment must contain nonempty, non-traversal URL segments.");
            }

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "fragment", fragment },
                { "is_pinned", isPinned.ToString().ToLowerInvariant() }
            };
            if (name != null) data.Add("name", name);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _endpoint, data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "NavigationViews.Create failed: " + response.GetFailureMessage());
        }

        /// <summary>Creates a navigation view (throwing version).</summary>
        [Obsolete("Use Create(fragment, isPinned, name); a name alone cannot identify a navigation view.")]
        public Task Create(string name)
        {
            return Task.FromException(new Exception(
                "NavigationViews.Create failed: Use Create(fragment, isPinned, name); a name alone cannot identify a view."));
        }

        /// <summary>Creates a navigation view, throwing on failure.</summary>
        /// <param name="fragment">The unique URL fragment.</param>
        /// <param name="isPinned">Whether the view is pinned.</param>
        /// <param name="name">The optional custom display name.</param>
        public async Task Create(string fragment, bool isPinned, string? name = null)
        {
            var result = await TryCreate(fragment, isPinned, name);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Updates a navigation view.</summary>
        /// <param name="viewId">The view ID.</param>
        /// <param name="name">(Optional) New name.</param>
        /// <remarks>Feature level 390: added the update navigation-view endpoint.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        [Obsolete("Use TryUpdate(fragment, name, isPinned); navigation views do not have numeric IDs.")]
        public Task<(bool success, string? details)> TryUpdate(int viewId, string? name = null)
        {
            return Task.FromResult<(bool success, string? details)>((false,
                "NavigationViews.Update failed: Use TryUpdate(fragment, name, isPinned); navigation views do not have numeric IDs."));
        }

        /// <summary>Updates a view's name or pin state.</summary>
        /// <param name="fragment">The unique URL fragment.</param>
        /// <param name="name">The optional new display name.</param>
        /// <param name="isPinned">The optional pin state.</param>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryUpdate(string fragment, string? name = null, bool? isPinned = null)
        {
            if (!TryEncodeFragment(fragment, out string encoded) || (name == null && isPinned == null))
            {
                return (false, "NavigationViews.Update failed: Provide a valid fragment and at least one of name or isPinned.");
            }

            Dictionary<string, string> data = [];
            if (name != null) data.Add("name", name);
            if (isPinned != null) data.Add("is_pinned", isPinned.Value.ToString().ToLowerInvariant());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_endpoint}/{encoded}", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "NavigationViews.Update failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a navigation view (throwing version).</summary>
        [Obsolete("Use Update(fragment, name, isPinned); navigation views do not have numeric IDs.")]
        public Task Update(int viewId, string? name = null)
        {
            return Task.FromException(new Exception(
                "NavigationViews.Update failed: Use Update(fragment, name, isPinned); navigation views do not have numeric IDs."));
        }

        /// <summary>Updates a view, throwing on failure.</summary>
        /// <param name="fragment">The unique URL fragment.</param>
        /// <param name="name">The optional display name.</param>
        /// <param name="isPinned">The optional pin state.</param>
        public async Task Update(string fragment, string? name = null, bool? isPinned = null)
        {
            var result = await TryUpdate(fragment, name, isPinned);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes a navigation view.</summary>
        /// <param name="viewId">The view ID.</param>
        /// <remarks>Feature level 390: added the delete navigation-view endpoint.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        [Obsolete("Use TryDelete(fragment); navigation views do not have numeric IDs.")]
        public Task<(bool success, string? details)> TryDelete(int viewId)
        {
            return Task.FromResult<(bool success, string? details)>((false,
                "NavigationViews.Delete failed: Use TryDelete(fragment); navigation views do not have numeric IDs."));
        }

        /// <summary>Deletes a view by its unique fragment.</summary>
        /// <param name="fragment">The URL fragment.</param>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryDelete(string fragment)
        {
            if (!TryEncodeFragment(fragment, out string encoded))
            {
                return (false, "NavigationViews.Delete failed: fragment must contain nonempty, non-traversal URL segments.");
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_endpoint}/{encoded}", null);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "NavigationViews.Delete failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes a navigation view (throwing version).</summary>
        [Obsolete("Use Delete(fragment); navigation views do not have numeric IDs.")]
        public Task Delete(int viewId)
        {
            return Task.FromException(new Exception(
                "NavigationViews.Delete failed: Use Delete(fragment); navigation views do not have numeric IDs."));
        }

        /// <summary>Deletes a view by fragment, throwing on failure.</summary>
        /// <param name="fragment">The unique URL fragment.</param>
        public async Task Delete(string fragment)
        {
            var result = await TryDelete(fragment);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Encodes a catch-all fragment path without permitting URI traversal or query injection.</summary>
        /// <param name="fragment">The logical fragment.</param>
        /// <param name="encoded">The encoded slash-delimited path.</param>
        /// <returns>Whether the fragment can be represented safely.</returns>
        private static bool TryEncodeFragment(string fragment, out string encoded)
        {
            encoded = string.Empty;
            if (string.IsNullOrWhiteSpace(fragment)) return false;
            string[] segments = fragment.Split('/');
            for (int index = 0; index < segments.Length; index++)
            {
                if (string.IsNullOrEmpty(segments[index]) || segments[index] is "." or "..") return false;
                segments[index] = Uri.EscapeDataString(segments[index]);
            }

            encoded = string.Join("/", segments);
            return true;
        }

        /// <summary>Requires the fragment identity and pin state on listed views.</summary>
        /// <param name="views">The views array.</param>
        /// <returns>An error or null.</returns>
        private static string? ValidateViews(JsonElement views)
        {
            foreach (JsonElement view in views.EnumerateArray())
            {
                if (view.ValueKind != JsonValueKind.Object ||
                    !view.TryGetProperty("fragment", out JsonElement fragment) ||
                    fragment.ValueKind != JsonValueKind.String ||
                    !view.TryGetProperty("is_pinned", out JsonElement pinned) ||
                    pinned.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return "navigation views require a string fragment and boolean is_pinned.";
                }
            }

            return null;
        }
    }
}
