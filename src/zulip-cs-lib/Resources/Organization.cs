using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Organization settings resource.</summary>
    public class Organization
    {
        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Organization class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Organization(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all linkifiers.</summary>
        /// <returns>An asynchronous result that yields (success, details, linkifiers).</returns>
        public async Task<(bool success, string details, List<LinkifierObject> linkifiers)> TryGetLinkifiers()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/realm/linkifiers", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Linkifiers ?? new List<LinkifierObject>());
            }

            return (false, "Organization.GetLinkifiers failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all linkifiers (throwing version).</summary>
        public async Task<List<LinkifierObject>> GetLinkifiers()
        {
            var result = await TryGetLinkifiers();
            if (!result.success) throw new Exception(result.details);
            return result.linkifiers;
        }

        /// <summary>Adds a linkifier.</summary>
        /// <param name="pattern">The regex pattern.</param>
        /// <param name="urlTemplate">The URL template.</param>
        /// <returns>An asynchronous result that yields (success, details, filterId).</returns>
        public async Task<(bool success, string details, int filterId)> TryAddLinkifier(string pattern, string urlTemplate)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "pattern", pattern },
                { "url_template", urlTemplate }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/realm/filters", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.FilterId);
            }

            return (false, "Organization.AddLinkifier failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Adds a linkifier (throwing version).</summary>
        public async Task<int> AddLinkifier(string pattern, string urlTemplate)
        {
            var result = await TryAddLinkifier(pattern, urlTemplate);
            if (!result.success) throw new Exception(result.details);
            return result.filterId;
        }

        /// <summary>Updates a linkifier.</summary>
        /// <param name="filterId">The filter ID.</param>
        /// <param name="pattern">The new regex pattern.</param>
        /// <param name="urlTemplate">The new URL template.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryUpdateLinkifier(int filterId, string pattern, string urlTemplate)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "pattern", pattern },
                { "url_template", urlTemplate }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"api/v1/realm/filters/{filterId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Organization.UpdateLinkifier failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a linkifier (throwing version).</summary>
        public async Task UpdateLinkifier(int filterId, string pattern, string urlTemplate)
        {
            var result = await TryUpdateLinkifier(filterId, pattern, urlTemplate);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Removes a linkifier.</summary>
        /// <param name="filterId">The filter ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryRemoveLinkifier(int filterId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"api/v1/realm/filters/{filterId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Organization.RemoveLinkifier failed: " + response.GetFailureMessage());
        }

        /// <summary>Removes a linkifier (throwing version).</summary>
        public async Task RemoveLinkifier(int filterId)
        {
            var result = await TryRemoveLinkifier(filterId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets all custom emoji.</summary>
        /// <returns>An asynchronous result that yields (success, details, emoji).</returns>
        public async Task<(bool success, string details, Dictionary<string, EmojiObject> emoji)> TryGetCustomEmoji()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/realm/emoji", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Emoji ?? new Dictionary<string, EmojiObject>());
            }

            return (false, "Organization.GetCustomEmoji failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all custom emoji (throwing version).</summary>
        public async Task<Dictionary<string, EmojiObject>> GetCustomEmoji()
        {
            var result = await TryGetCustomEmoji();
            if (!result.success) throw new Exception(result.details);
            return result.emoji;
        }

        /// <summary>Deactivates a custom emoji.</summary>
        /// <param name="emojiName">The emoji name.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDeactivateCustomEmoji(string emojiName)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"api/v1/realm/emoji/{emojiName}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Organization.DeactivateCustomEmoji failed: " + response.GetFailureMessage());
        }

        /// <summary>Deactivates a custom emoji (throwing version).</summary>
        public async Task DeactivateCustomEmoji(string emojiName)
        {
            var result = await TryDeactivateCustomEmoji(emojiName);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets custom profile fields.</summary>
        /// <returns>An asynchronous result that yields (success, details, fields).</returns>
        public async Task<(bool success, string details, List<ProfileFieldObject> fields)> TryGetProfileFields()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/realm/profile_fields", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.CustomFields ?? new List<ProfileFieldObject>());
            }

            return (false, "Organization.GetProfileFields failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets custom profile fields (throwing version).</summary>
        public async Task<List<ProfileFieldObject>> GetProfileFields()
        {
            var result = await TryGetProfileFields();
            if (!result.success) throw new Exception(result.details);
            return result.fields;
        }

        /// <summary>Creates a custom profile field.</summary>
        /// <param name="fieldType">The field type.</param>
        /// <param name="name">The field name.</param>
        /// <param name="hint">(Optional) The field hint.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryCreateProfileField(int fieldType, string name, string hint = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "field_type", fieldType.ToString() },
                { "name", name }
            };

            if (hint != null) data.Add("hint", hint);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/realm/profile_fields", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Organization.CreateProfileField failed: " + response.GetFailureMessage());
        }

        /// <summary>Creates a custom profile field (throwing version).</summary>
        public async Task CreateProfileField(int fieldType, string name, string hint = null)
        {
            var result = await TryCreateProfileField(fieldType, name, hint);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
