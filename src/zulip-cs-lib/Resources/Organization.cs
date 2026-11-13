using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Organization settings resource.</summary>
    public partial class Organization
    {
        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>The optional bounded multipart request delegate.</summary>
        private readonly MultipartUploadDelegate? _doUpload;

        /// <summary>Initializes a new instance of the Organization class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        /// <param name="doUpload">The optional multipart request delegate.</param>
        internal Organization(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest,
            MultipartUploadDelegate? doUpload = null)
        {
            _doZulipRequest = doZulipRequest;
            _doUpload = doUpload;
        }

        /// <summary>Gets all linkifiers.</summary>
        /// <remarks>Feature level 54: linkifier listing endpoint is part of the long-standing organization settings API.</remarks>
        /// <returns>An asynchronous result that yields (success, details, linkifiers).</returns>
        public async Task<(bool success, string? details, List<LinkifierObject>? linkifiers)> TryGetLinkifiers()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/realm/linkifiers", null);

            if (ResponsePayload.TryRead<List<LinkifierObject>>(response, "Organization.GetLinkifiers", "linkifiers", JsonValueKind.Array,
                out List<LinkifierObject>? linkifiers, out string? details,
                linkifiers => ResponsePayload.ValidateObjectIds(linkifiers, "id")))
            {
                return (true, null, linkifiers);
            }

            return (false, details, null);
        }

        /// <summary>Gets all linkifiers (throwing version).</summary>
        public async Task<List<LinkifierObject>> GetLinkifiers()
        {
            var result = await TryGetLinkifiers();
            if (!result.success) throw new Exception(result.details);
            return result.linkifiers!;
        }

        /// <summary>Adds a linkifier.</summary>
        /// <param name="pattern">The regex pattern.</param>
        /// <param name="urlTemplate">The URL template.</param>
        /// <remarks>Feature level 176: linkifier create/update APIs were formalized for realm filters.</remarks>
        /// <returns>An asynchronous result that yields (success, details, filterId).</returns>
        public async Task<(bool success, string? details, int filterId)> TryAddLinkifier(string pattern, string urlTemplate)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "pattern", pattern },
                { "url_template", urlTemplate }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/realm/filters", data);

            if (ResponsePayload.TryRead<int>(response, "Organization.AddLinkifier", "id", JsonValueKind.Number,
                out int filterId, out string? details))
            {
                return (true, null, filterId);
            }

            return (false, details, 0);
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
        /// <remarks>Feature level 176: linkifier update endpoint is tracked as part of realm filter APIs.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryUpdateLinkifier(int filterId, string pattern, string urlTemplate)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "pattern", pattern },
                { "url_template", urlTemplate }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"api/v1/realm/filters/{filterId}", data);

            if (ResponsePayload.IsSuccessful(response))
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
        public async Task<(bool success, string? details)> TryRemoveLinkifier(int filterId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"api/v1/realm/filters/{filterId}", null);

            if (ResponsePayload.IsSuccessful(response))
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
        /// <remarks>Feature level 113: custom emoji listing endpoint is tracked in API feature history.</remarks>
        /// <returns>An asynchronous result that yields (success, details, emoji).</returns>
        public async Task<(bool success, string? details, Dictionary<string, EmojiObject>? emoji)> TryGetCustomEmoji()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/realm/emoji", null);

            if (ResponsePayload.TryRead<Dictionary<string, EmojiObject>>(response, "Organization.GetCustomEmoji", "emoji", JsonValueKind.Object,
                out Dictionary<string, EmojiObject>? emoji, out string? details, ResponsePayload.ValidateObjectValues))
            {
                return (true, null, emoji);
            }

            return (false, details, null);
        }

        /// <summary>Gets all custom emoji (throwing version).</summary>
        public async Task<Dictionary<string, EmojiObject>> GetCustomEmoji()
        {
            var result = await TryGetCustomEmoji();
            if (!result.success) throw new Exception(result.details);
            return result.emoji!;
        }

        /// <summary>Deactivates a custom emoji.</summary>
        /// <param name="emojiName">The emoji name.</param>
        /// <remarks>Feature level 190: custom emoji deactivation endpoint is tracked in changelog history.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryDeactivateCustomEmoji(string emojiName)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"api/v1/realm/emoji/{emojiName}", null);

            if (ResponsePayload.IsSuccessful(response))
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
        /// <remarks>Feature level 455: profile fields gained <c>use_for_user_matching</c> in API payloads.</remarks>
        /// <returns>An asynchronous result that yields (success, details, fields).</returns>
        public async Task<(bool success, string? details, List<ProfileFieldObject>? fields)> TryGetProfileFields()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/realm/profile_fields", null);

            if (ResponsePayload.TryRead<List<ProfileFieldObject>>(response, "Organization.GetProfileFields", "custom_fields", JsonValueKind.Array,
                out List<ProfileFieldObject>? fields, out string? details,
                fields => ResponsePayload.ValidateObjectIds(fields, "id")))
            {
                return (true, null, fields);
            }

            return (false, details, null);
        }

        /// <summary>Gets custom profile fields (throwing version).</summary>
        public async Task<List<ProfileFieldObject>> GetProfileFields()
        {
            var result = await TryGetProfileFields();
            if (!result.success) throw new Exception(result.details);
            return result.fields!;
        }

        /// <summary>Creates a custom profile field.</summary>
        /// <param name="fieldType">The field type.</param>
        /// <param name="name">The field name.</param>
        /// <param name="hint">(Optional) The field hint.</param>
        /// <remarks>Feature level 455: created profile field objects include newer matching metadata fields.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryCreateProfileField(int fieldType, string name, string? hint = null)
        {
            var result = await TryCreateProfileFieldWithId(fieldType, name, hint);
            return (result.success, result.details);
        }

        /// <summary>Creates a custom profile field and returns its identity; requires an organization administrator.</summary>
        /// <param name="fieldType">The documented profile field type.</param>
        /// <param name="name">The field name.</param>
        /// <param name="hint">Optional help text.</param>
        /// <param name="options">Optional field configuration, gated by the caller's server feature level.</param>
        /// <returns>The created field identity, or explicit failure details.</returns>
        public async Task<(bool success, string? details, int fieldId)> TryCreateProfileFieldWithId(
            int fieldType, string name, string? hint = null, ProfileFieldOptions? options = null)
        {
            if (options?.FieldData is JsonElement fieldData && fieldData.ValueKind != JsonValueKind.Object)
            {
                return (false, "Organization.CreateProfileField failed: field_data must be a JSON object.", 0);
            }

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "field_type", fieldType.ToString() },
                { "name", name }
            };

            if (hint != null) data.Add("hint", hint);
            if (options?.FieldData is JsonElement configuration) data.Add("field_data", configuration.GetRawText());
            if (options?.DisplayInProfileSummary is bool summary) data.Add("display_in_profile_summary", summary.ToString().ToLowerInvariant());
            if (options?.Required is bool required) data.Add("required", required.ToString().ToLowerInvariant());
            if (options?.EditableByUser is bool editable) data.Add("editable_by_user", editable.ToString().ToLowerInvariant());
            if (options?.UseForUserMatching is bool matching) data.Add("use_for_user_matching", matching.ToString().ToLowerInvariant());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/realm/profile_fields", data);

            if (ResponsePayload.TryRead<int>(response, "Organization.CreateProfileField", "id", JsonValueKind.Number,
                out int fieldId, out string? details))
            {
                return (true, null, fieldId);
            }

            return (false, details, 0);
        }

        /// <summary>Creates a custom profile field and returns its identity, throwing on failure.</summary>
        /// <param name="fieldType">The documented profile field type.</param>
        /// <param name="name">The field name.</param>
        /// <param name="hint">Optional help text.</param>
        /// <param name="options">Optional field configuration.</param>
        /// <returns>The created field identity.</returns>
        public async Task<int> CreateProfileFieldWithId(
            int fieldType, string name, string? hint = null, ProfileFieldOptions? options = null)
        {
            var result = await TryCreateProfileFieldWithId(fieldType, name, hint, options);
            if (!result.success) throw new Exception(result.details);
            return result.fieldId;
        }

        /// <summary>Creates a custom profile field (throwing version).</summary>
        public async Task CreateProfileField(int fieldType, string name, string? hint = null)
        {
            var result = await TryCreateProfileField(fieldType, name, hint);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
