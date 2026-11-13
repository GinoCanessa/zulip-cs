using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Documented organization administration workflows.</summary>
    public partial class Organization
    {
        /// <summary>Updates a custom profile field's mutable configuration; requires an organization administrator.</summary>
        /// <param name="fieldId">The profile field identity.</param>
        /// <param name="name">The optional new label.</param>
        /// <param name="hint">The optional new help text.</param>
        /// <param name="options">The optional field configuration.</param>
        /// <remarks>Partial updates require feature level 252; individual options retain their documented feature requirements.</remarks>
        /// <returns>The standard mutation result.</returns>
        public async Task<(bool success, string? details)> TryUpdateProfileField(
            int fieldId, string? name = null, string? hint = null, ProfileFieldOptions? options = null)
        {
            if (options?.FieldData is JsonElement fieldData && fieldData.ValueKind != JsonValueKind.Object)
                return (false, "Organization.UpdateProfileField failed: field_data must be a JSON object.");
            Dictionary<string, string> data = [];
            if (name != null) data.Add("name", name);
            if (hint != null) data.Add("hint", hint);
            if (options?.FieldData is JsonElement configuration) data.Add("field_data", configuration.GetRawText());
            if (options?.DisplayInProfileSummary is bool summary) data.Add("display_in_profile_summary", summary ? "true" : "false");
            if (options?.Required is bool required) data.Add("required", required ? "true" : "false");
            if (options?.EditableByUser is bool editable) data.Add("editable_by_user", editable ? "true" : "false");
            if (options?.UseForUserMatching is bool matching) data.Add("use_for_user_matching", matching ? "true" : "false");
            return await ExecuteAdminMutation(HttpMethod.Patch,
                "api/v1/realm/profile_fields/" + fieldId.ToString(CultureInfo.InvariantCulture),
                data, "Organization.UpdateProfileField");
        }

        /// <summary>Updates a custom profile field, throwing on failure.</summary>
        /// <param name="fieldId">The profile field identity.</param>
        /// <param name="name">The optional new label.</param>
        /// <param name="hint">The optional new help text.</param>
        /// <param name="options">The optional field configuration.</param>
        public async Task UpdateProfileField(
            int fieldId, string? name = null, string? hint = null, ProfileFieldOptions? options = null)
        {
            (bool success, string? details) result = await TryUpdateProfileField(fieldId, name, hint, options);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes a custom profile field and all users' values for it; requires an organization administrator.</summary>
        /// <param name="fieldId">The profile field identity.</param>
        /// <returns>The standard mutation result.</returns>
        public Task<(bool success, string? details)> TryDeleteProfileField(int fieldId)
        {
            return ExecuteAdminMutation(HttpMethod.Delete,
                "api/v1/realm/profile_fields/" + fieldId.ToString(CultureInfo.InvariantCulture),
                null, "Organization.DeleteProfileField");
        }

        /// <summary>Deletes a custom profile field and its stored values, throwing on failure.</summary>
        /// <param name="fieldId">The profile field identity.</param>
        public async Task DeleteProfileField(int fieldId)
        {
            (bool success, string? details) result = await TryDeleteProfileField(fieldId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Reorders all custom profile fields; requires an organization administrator.</summary>
        /// <param name="order">All field IDs in the desired order, including an explicit empty list when there are no fields.</param>
        /// <returns>The standard mutation result.</returns>
        public Task<(bool success, string? details)> TryReorderProfileFields(IReadOnlyList<int> order)
        {
            if (order == null)
                return Task.FromResult<(bool success, string? details)>((false,
                    "Organization.ReorderProfileFields failed: an explicit field order is required."));
            Dictionary<string, string> data = new Dictionary<string, string> { { "order", JsonSerializer.Serialize(order) } };
            return ExecuteAdminMutation(HttpMethod.Patch, "api/v1/realm/profile_fields", data, "Organization.ReorderProfileFields");
        }

        /// <summary>Reorders custom profile fields, throwing on failure.</summary>
        /// <param name="order">All field IDs in the desired order.</param>
        public async Task ReorderProfileFields(IReadOnlyList<int> order)
        {
            (bool success, string? details) result = await TryReorderProfileFields(order);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Reorders all linkifiers; requires an organization administrator and feature level 202.</summary>
        /// <param name="orderedLinkifierIds">All linkifier IDs in the desired match order.</param>
        /// <returns>The standard mutation result.</returns>
        public Task<(bool success, string? details)> TryReorderLinkifiers(IReadOnlyList<int> orderedLinkifierIds)
        {
            if (orderedLinkifierIds == null)
                return Task.FromResult<(bool success, string? details)>((false,
                    "Organization.ReorderLinkifiers failed: an explicit linkifier order is required."));
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "ordered_linkifier_ids", JsonSerializer.Serialize(orderedLinkifierIds) }
            };
            return ExecuteAdminMutation(HttpMethod.Patch, "api/v1/realm/linkifiers", data, "Organization.ReorderLinkifiers");
        }

        /// <summary>Reorders linkifiers, throwing on failure.</summary>
        /// <param name="orderedLinkifierIds">All linkifier IDs in the desired match order.</param>
        public async Task ReorderLinkifiers(IReadOnlyList<int> orderedLinkifierIds)
        {
            (bool success, string? details) result = await TryReorderLinkifiers(orderedLinkifierIds);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Uploads a custom emoji using a caller-owned stream and the shared bounded multipart transport.</summary>
        /// <param name="emojiName">The logical, unescaped emoji name.</param>
        /// <param name="stream">The caller-owned readable image stream.</param>
        /// <param name="filename">The multipart filename.</param>
        /// <param name="contentType">The MIME type, or null for application/octet-stream.</param>
        /// <param name="cancellationToken">Caller cancellation.</param>
        /// <remarks>The server enforces configured upload permissions, name rules and its emoji size limit (5 MiB by default). Uses UploadTimeout.</remarks>
        /// <returns>The standard mutation result, including an explicit failure when no multipart delegate is available.</returns>
        public async Task<(bool success, string? details)> TryUploadCustomEmoji(
            string emojiName, Stream stream, string filename, string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            if (!TryEncodeAdminSegment(emojiName, out string encodedName))
                return (false, "Organization.UploadCustomEmoji failed: a nonempty logical emoji name is required.");
            string? error = MultipartUpload.Validate(stream, filename, contentType);
            if (error != null) return (false, "Organization.UploadCustomEmoji failed: " + error);
            if (_doUpload == null)
                return (false, "Organization.UploadCustomEmoji failed: no multipart upload delegate is available.");
            ZulipResponse response = await _doUpload("api/v1/realm/emoji/" + encodedName,
                "filename", stream, filename, contentType ?? "application/octet-stream", cancellationToken);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "Organization.UploadCustomEmoji failed: " + response.GetFailureMessage());
        }

        /// <summary>Uploads a custom emoji, throwing on failure without taking ownership of the stream.</summary>
        /// <param name="emojiName">The logical, unescaped emoji name.</param>
        /// <param name="stream">The caller-owned readable image stream.</param>
        /// <param name="filename">The multipart filename.</param>
        /// <param name="contentType">The optional MIME type.</param>
        /// <param name="cancellationToken">Caller cancellation.</param>
        public async Task UploadCustomEmoji(
            string emojiName, Stream stream, string filename, string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            (bool success, string? details) result = await TryUploadCustomEmoji(
                emojiName, stream, filename, contentType, cancellationToken);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets the allowed email domains configured in the organization.</summary>
        /// <returns>The server's allowed domains, including an empty list, or explicit failure details.</returns>
        public async Task<(bool success, string? details, List<OrganizationAllowedDomain>? domains)> TryGetAllowedDomains()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/realm/domains", null);
            if (ResponsePayload.TryRead<List<OrganizationAllowedDomain>>(response, "Organization.GetAllowedDomains",
                "domains", JsonValueKind.Array, out List<OrganizationAllowedDomain>? domains, out string? details,
                ValidateAllowedDomains))
                return (true, null, domains);
            return (false, details, null);
        }

        /// <summary>Gets allowed email domains, throwing on failure.</summary>
        /// <returns>The server's allowed domains.</returns>
        public async Task<List<OrganizationAllowedDomain>> GetAllowedDomains()
        {
            (bool success, string? details, List<OrganizationAllowedDomain>? domains) result = await TryGetAllowedDomains();
            if (!result.success) throw new Exception(result.details);
            return result.domains!;
        }

        /// <summary>Adds an allowed email domain and returns its server identity; requires an organization owner.</summary>
        /// <param name="domain">The logical domain name.</param>
        /// <param name="allowSubdomains">Whether email addresses in subdomains are allowed.</param>
        /// <remarks>Unencoded string input requires feature level 63. Owner-only permissions apply from feature level 143.</remarks>
        /// <returns>The newly created domain identity and name, or explicit failure details.</returns>
        public async Task<(bool success, string? details, OrganizationAllowedDomainCreationResult? domain)> TryAddAllowedDomain(
            string domain, bool allowSubdomains)
        {
            if (!TryEncodeAdminSegment(domain, out _))
                return (false, "Organization.AddAllowedDomain failed: a nonempty logical domain is required.", null);
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "domain", domain },
                { "allow_subdomains", allowSubdomains ? "true" : "false" }
            };
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/realm/domains", data);
            if (ResponsePayload.TryRead<JsonElement>(response, "Organization.AddAllowedDomain", "new_domain",
                JsonValueKind.Array, out JsonElement created, out string? details, ValidateCreatedAllowedDomain))
                return (true, null, new OrganizationAllowedDomainCreationResult(created[0].GetInt32(), created[1].GetString()!));
            return (false, details, null);
        }

        /// <summary>Adds an allowed email domain, throwing on failure.</summary>
        /// <param name="domain">The logical domain name.</param>
        /// <param name="allowSubdomains">Whether subdomains are allowed.</param>
        /// <returns>The newly created domain identity and name.</returns>
        public async Task<OrganizationAllowedDomainCreationResult> AddAllowedDomain(string domain, bool allowSubdomains)
        {
            (bool success, string? details, OrganizationAllowedDomainCreationResult? domain) result =
                await TryAddAllowedDomain(domain, allowSubdomains);
            if (!result.success) throw new Exception(result.details);
            return result.domain!;
        }

        /// <summary>Updates an allowed domain's subdomain policy; requires an organization owner (feature level 143).</summary>
        /// <param name="domain">The logical, unescaped domain name.</param>
        /// <param name="allowSubdomains">Whether subdomains are allowed.</param>
        /// <returns>The standard mutation result.</returns>
        public Task<(bool success, string? details)> TryUpdateAllowedDomain(string domain, bool allowSubdomains)
        {
            if (!TryEncodeAdminSegment(domain, out string encodedDomain))
                return Task.FromResult<(bool success, string? details)>((false,
                    "Organization.UpdateAllowedDomain failed: a nonempty logical domain is required."));
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "allow_subdomains", allowSubdomains ? "true" : "false" }
            };
            return ExecuteAdminMutation(HttpMethod.Patch, "api/v1/realm/domains/" + encodedDomain,
                data, "Organization.UpdateAllowedDomain");
        }

        /// <summary>Updates an allowed domain, throwing on failure.</summary>
        /// <param name="domain">The logical domain name.</param>
        /// <param name="allowSubdomains">Whether subdomains are allowed.</param>
        public async Task UpdateAllowedDomain(string domain, bool allowSubdomains)
        {
            (bool success, string? details) result = await TryUpdateAllowedDomain(domain, allowSubdomains);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes an allowed email domain; requires an organization owner (feature level 143).</summary>
        /// <param name="domain">The logical, unescaped domain name.</param>
        /// <returns>The standard mutation result.</returns>
        public Task<(bool success, string? details)> TryDeleteAllowedDomain(string domain)
        {
            if (!TryEncodeAdminSegment(domain, out string encodedDomain))
                return Task.FromResult<(bool success, string? details)>((false,
                    "Organization.DeleteAllowedDomain failed: a nonempty logical domain is required."));
            return ExecuteAdminMutation(HttpMethod.Delete, "api/v1/realm/domains/" + encodedDomain,
                null, "Organization.DeleteAllowedDomain");
        }

        /// <summary>Deletes an allowed email domain, throwing on failure.</summary>
        /// <param name="domain">The logical domain name.</param>
        public async Task DeleteAllowedDomain(string domain)
        {
            (bool success, string? details) result = await TryDeleteAllowedDomain(domain);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets organization data export states; requires an organization administrator.</summary>
        /// <remarks>Supports documented string export types (feature level 449). No export polling or downloading is performed.</remarks>
        /// <returns>The export records, including an empty list, or explicit failure details.</returns>
        public async Task<(bool success, string? details, List<OrganizationExport>? exports)> TryGetExports()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/export/realm", null);
            if (ResponsePayload.TryRead<List<OrganizationExport>>(response, "Organization.GetExports", "exports",
                JsonValueKind.Array, out List<OrganizationExport>? exports, out string? details, ValidateOrganizationExports))
                return (true, null, exports);
            return (false, details, null);
        }

        /// <summary>Gets organization data export states, throwing on failure.</summary>
        /// <returns>The server's export records.</returns>
        public async Task<List<OrganizationExport>> GetExports()
        {
            (bool success, string? details, List<OrganizationExport>? exports) result = await TryGetExports();
            if (!result.success) throw new Exception(result.details);
            return result.exports!;
        }

        /// <summary>Queues a data export and returns its identity; requires an organization administrator.</summary>
        /// <param name="exportType">The optional selection; null omits the parameter and uses the server's public default.</param>
        /// <remarks>Returned IDs require feature level 182. Explicit string selections require 449; full_without_consent also requires owner_full_content_access. Success does not imply generation completed.</remarks>
        /// <returns>The queued export identity or explicit failure details.</returns>
        public async Task<(bool success, string? details, int exportId)> TryCreateExport(OrganizationExportType? exportType = null)
        {
            Dictionary<string, string>? data = null;
            if (exportType != null)
            {
                string? selection = exportType switch
                {
                    OrganizationExportType.Public => "public",
                    OrganizationExportType.FullWithConsent => "full_with_consent",
                    OrganizationExportType.FullWithoutConsent => "full_without_consent",
                    _ => null
                };
                if (selection == null) return (false, "Organization.CreateExport failed: invalid export type.", 0);
                data = new Dictionary<string, string> { { "export_type", selection } };
            }
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/export/realm", data);
            if (ResponsePayload.TryRead<int>(response, "Organization.CreateExport", "id", JsonValueKind.Number,
                out int exportId, out string? details))
                return (true, null, exportId);
            return (false, details, 0);
        }

        /// <summary>Queues a data export, throwing on failure.</summary>
        /// <param name="exportType">The optional export selection.</param>
        /// <returns>The queued export identity, not its completion outcome.</returns>
        public async Task<int> CreateExport(OrganizationExportType? exportType = null)
        {
            (bool success, string? details, int exportId) result = await TryCreateExport(exportType);
            if (!result.success) throw new Exception(result.details);
            return result.exportId;
        }

        /// <summary>Deletes a completed data export; requires an organization administrator.</summary>
        /// <param name="exportId">The export identity.</param>
        /// <remarks>The server rejects invalid, deleted, pending or unavailable prior-server exports. No state is inferred from a success envelope.</remarks>
        /// <returns>The standard mutation result.</returns>
        public Task<(bool success, string? details)> TryDeleteExport(int exportId)
        {
            return ExecuteAdminMutation(HttpMethod.Delete,
                "api/v1/export/realm/" + exportId.ToString(CultureInfo.InvariantCulture), null, "Organization.DeleteExport");
        }

        /// <summary>Deletes a completed data export, throwing on failure.</summary>
        /// <param name="exportId">The export identity.</param>
        public async Task DeleteExport(int exportId)
        {
            (bool success, string? details) result = await TryDeleteExport(exportId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets users' private-data export consent states; requires an organization administrator and feature level 295.</summary>
        /// <returns>The consent records, including an empty list, or explicit failure details.</returns>
        public async Task<(bool success, string? details, List<OrganizationExportConsent>? consents)> TryGetExportConsents()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/export/realm/consents", null);
            if (ResponsePayload.TryRead<List<OrganizationExportConsent>>(response, "Organization.GetExportConsents",
                "export_consents", JsonValueKind.Array, out List<OrganizationExportConsent>? consents, out string? details,
                ValidateExportConsents))
                return (true, null, consents);
            return (false, details, null);
        }

        /// <summary>Gets private-data export consent states, throwing on failure.</summary>
        /// <returns>The server's consent records.</returns>
        public async Task<List<OrganizationExportConsent>> GetExportConsents()
        {
            (bool success, string? details, List<OrganizationExportConsent>? consents) result = await TryGetExportConsents();
            if (!result.success) throw new Exception(result.details);
            return result.consents!;
        }

        /// <summary>Updates selected defaults for future new members, not existing users; requires an organization administrator.</summary>
        /// <param name="options">The explicitly selected notification and display defaults.</param>
        /// <remarks>Requires feature level 96 (24-hour time at 99). This type intentionally excludes unsupported profile, identity, language and timezone settings.</remarks>
        /// <returns>The standard mutation result; the endpoint returns no updated-settings payload.</returns>
        public Task<(bool success, string? details)> TryUpdateUserSettingsDefaults(RealmUserSettingsDefaultsOptions options)
        {
            if (options == null)
                return Task.FromResult<(bool success, string? details)>((false,
                    "Organization.UpdateUserSettingsDefaults failed: explicit settings options are required."));
            return ExecuteAdminMutation(HttpMethod.Patch, "api/v1/realm/user_settings_defaults",
                options.ToForm(), "Organization.UpdateUserSettingsDefaults");
        }

        /// <summary>Updates defaults for new organization members, throwing on failure.</summary>
        /// <param name="options">The selected notification and display defaults.</param>
        public async Task UpdateUserSettingsDefaults(RealmUserSettingsDefaultsOptions options)
        {
            (bool success, string? details) result = await TryUpdateUserSettingsDefaults(options);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Runs an organization mutation that requires only the documented success envelope.</summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="endpoint">The API-relative endpoint.</param>
        /// <param name="data">The optional form fields.</param>
        /// <param name="operation">The operation name for failure details.</param>
        /// <returns>The standard mutation result.</returns>
        private async Task<(bool success, string? details)> ExecuteAdminMutation(
            HttpMethod method, string endpoint, Dictionary<string, string>? data, string operation)
        {
            ZulipResponse response = await _doZulipRequest(method, endpoint, data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, operation + " failed: " + response.GetFailureMessage());
        }

        /// <summary>Encodes a logical name as exactly one route segment without normalizing or interpreting existing percent escapes.</summary>
        /// <param name="logical">The logical name.</param>
        /// <param name="encoded">The safely encoded segment.</param>
        /// <returns>Whether the name is nonempty, non-traversal and contains no controls.</returns>
        private static bool TryEncodeAdminSegment(string logical, out string encoded)
        {
            encoded = string.Empty;
            if (string.IsNullOrWhiteSpace(logical) || logical is "." or ".." || logical.Any(char.IsControl)) return false;
            encoded = Uri.EscapeDataString(logical);
            return true;
        }

        /// <summary>Validates domain names and actual boolean subdomain policies in returned domain records.</summary>
        /// <param name="domains">The domain records.</param>
        /// <returns>A validation error, or null.</returns>
        private static string? ValidateAllowedDomains(JsonElement domains)
        {
            foreach (JsonElement domain in domains.EnumerateArray())
            {
                string? error = ValidateAdminField(domain, "domain", JsonValueKind.String)
                    ?? ValidateAdminField(domain, "allow_subdomains", JsonValueKind.True, JsonValueKind.False);
                if (error != null) return error;
                if (string.IsNullOrWhiteSpace(domain.GetProperty("domain").GetString())) return "domain must be nonempty.";
            }
            return null;
        }

        /// <summary>Validates the documented heterogeneous created-domain array.</summary>
        /// <param name="created">The returned new_domain array.</param>
        /// <returns>A validation error, or null.</returns>
        private static string? ValidateCreatedAllowedDomain(JsonElement created)
        {
            return created.GetArrayLength() != 2 || created[0].ValueKind != JsonValueKind.Number ||
                !created[0].TryGetInt32(out _) || created[1].ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(created[1].GetString())
                ? "new_domain must contain an integer identity and a nonempty domain string."
                : null;
        }

        /// <summary>Requires export identities and state fields while preserving documented null download URLs and timestamps.</summary>
        /// <param name="exports">The returned export records.</param>
        /// <returns>A validation error, or null.</returns>
        private static string? ValidateOrganizationExports(JsonElement exports)
        {
            foreach (JsonElement export in exports.EnumerateArray())
            {
                string? error = ResponsePayload.ValidateObjectId(export, "id")
                    ?? ResponsePayload.ValidateObjectId(export, "acting_user_id")
                    ?? ValidateAdminField(export, "export_time", JsonValueKind.Number)
                    ?? ValidateAdminField(export, "deleted_timestamp", JsonValueKind.Number, JsonValueKind.Null)
                    ?? ValidateAdminField(export, "failed_timestamp", JsonValueKind.Number, JsonValueKind.Null)
                    ?? ValidateAdminField(export, "export_url", JsonValueKind.String, JsonValueKind.Null)
                    ?? ValidateAdminField(export, "pending", JsonValueKind.True, JsonValueKind.False)
                    ?? ValidateAdminField(export, "export_type", JsonValueKind.String);
                if (error != null) return error;
                if (string.IsNullOrWhiteSpace(export.GetProperty("export_type").GetString())) return "export_type must be nonempty.";
                if (export.TryGetProperty("export_from_prior_server", out _))
                {
                    error = ValidateAdminField(export, "export_from_prior_server", JsonValueKind.True, JsonValueKind.False);
                    if (error != null) return error;
                }
            }
            return null;
        }

        /// <summary>Requires explicit user identities and consent booleans, retaining optional older-server email visibility.</summary>
        /// <param name="consents">The returned consent records.</param>
        /// <returns>A validation error, or null.</returns>
        private static string? ValidateExportConsents(JsonElement consents)
        {
            foreach (JsonElement consent in consents.EnumerateArray())
            {
                string? error = ResponsePayload.ValidateObjectId(consent, "user_id")
                    ?? ValidateAdminField(consent, "consented", JsonValueKind.True, JsonValueKind.False);
                if (error != null) return error;
                if (consent.TryGetProperty("email_address_visibility", out _))
                {
                    error = ResponsePayload.ValidateObjectId(consent, "email_address_visibility");
                    if (error != null) return error;
                }
            }
            return null;
        }

        /// <summary>Requires a named object's actual wire field and one of its documented JSON kinds.</summary>
        /// <param name="item">The object.</param>
        /// <param name="field">The required field.</param>
        /// <param name="kind">The permitted JSON kind.</param>
        /// <param name="alternateKind">An optional second permitted JSON kind.</param>
        /// <returns>A validation error, or null.</returns>
        private static string? ValidateAdminField(JsonElement item, string field, JsonValueKind kind, JsonValueKind? alternateKind = null)
        {
            return item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(field, out JsonElement value) ||
                (value.ValueKind != kind && value.ValueKind != alternateKind)
                ? $"required object field '{field}' has a missing or invalid value."
                : null;
        }
    }
}
