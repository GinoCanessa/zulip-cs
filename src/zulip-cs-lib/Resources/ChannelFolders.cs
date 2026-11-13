using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Channel-folder lifecycle and display ordering (feature level 389).</summary>
    /// <remarks>Creation, update and reorder require an organization administrator. Name and description length limits are supplied by the register endpoint.</remarks>
    public sealed class ChannelFolders
    {
        /// <summary>The channel-folder API endpoint.</summary>
        private const string _endpoint = "api/v1/channel_folders";

        /// <summary>The ordinary Zulip request delegate.</summary>
        private readonly Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes the channel-folder resource.</summary>
        /// <param name="doZulipRequest">The ordinary request delegate.</param>
        internal ChannelFolders(Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Creates a folder and returns its required channel_folder_id.</summary>
        /// <param name="name">The nonempty folder name.</param>
        /// <param name="description">The required description; an empty string is allowed.</param>
        /// <returns>Success and the created folder ID, or explicit failure details.</returns>
        public async Task<(bool success, string? details, int channelFolderId)> TryCreate(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name) || description == null)
                return (false, "ChannelFolders.Create failed: a nonempty name and a description (which may be empty) are required.", 0);
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_endpoint}/create",
                new Dictionary<string, string> { { "name", name }, { "description", description } });
            if (ResponsePayload.TryRead<int>(response, "ChannelFolders.Create", "channel_folder_id", JsonValueKind.Number,
                out int id, out string? details))
                return (true, null, id);
            return (false, details, 0);
        }

        /// <summary>Creates a folder, throwing on failure.</summary>
        /// <param name="name">The nonempty folder name.</param>
        /// <param name="description">The required description, or an empty string.</param>
        /// <returns>The created folder ID.</returns>
        public async Task<int> Create(string name, string description)
        {
            (bool success, string? details, int channelFolderId) = await TryCreate(name, description);
            if (!success) throw new Exception(details);
            return channelFolderId;
        }

        /// <summary>Gets folders in server-provided display order.</summary>
        /// <param name="includeArchived">Whether to include archived folders; null omits the parameter and uses the server default false.</param>
        /// <remarks>Sorted by display order from feature level 414, and by ID on earlier servers.</remarks>
        /// <returns>Success and the folder list, including an empty list, or explicit failure details.</returns>
        public async Task<(bool success, string? details, List<ChannelFolderObject>? channelFolders)> TryGetAll(bool? includeArchived = null)
        {
            Dictionary<string, string>? data = includeArchived.HasValue
                ? new Dictionary<string, string> { { "include_archived", includeArchived.Value ? "true" : "false" } }
                : null;
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _endpoint, data);
            if (ResponsePayload.TryRead<List<ChannelFolderObject>>(response, "ChannelFolders.GetAll",
                "channel_folders", JsonValueKind.Array, out List<ChannelFolderObject>? folders, out string? details, ValidateFolders))
                return (true, null, folders);
            return (false, details, null);
        }

        /// <summary>Gets folders in server order, throwing on failure.</summary>
        /// <param name="includeArchived">Whether to include archived folders, or omit the setting.</param>
        /// <returns>The folder list.</returns>
        public async Task<List<ChannelFolderObject>> GetAll(bool? includeArchived = null)
        {
            (bool success, string? details, List<ChannelFolderObject>? channelFolders) = await TryGetAll(includeArchived);
            if (!success) throw new Exception(details);
            return channelFolders!;
        }

        /// <summary>Reorders all folders, including archived folders (feature level 414).</summary>
        /// <param name="order">All of the organization's folder IDs in the desired order; an empty array is allowed for an organization with no folders.</param>
        /// <remarks>The server validates that every folder is represented. No implicit list request or retry is performed.</remarks>
        /// <returns>Success or explicit failure details.</returns>
        public async Task<(bool success, string? details)> TryReorder(int[] order)
        {
            if (order == null || Array.Exists(order, id => id <= 0) || new HashSet<int>(order).Count != order.Length)
                return (false, "ChannelFolders.Reorder failed: order must contain distinct positive folder IDs.");
            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, _endpoint,
                new Dictionary<string, string> { { "order", JsonSerializer.Serialize(order) } });
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "ChannelFolders.Reorder failed: " + response.GetFailureMessage());
        }

        /// <summary>Reorders all folders, throwing on failure (feature level 414).</summary>
        /// <param name="order">All folder IDs, including archived folders.</param>
        /// <returns>The completed request.</returns>
        public async Task Reorder(int[] order)
        {
            (bool success, string? details) = await TryReorder(order);
            if (!success) throw new Exception(details);
        }

        /// <summary>Updates a folder, including archiving or unarchiving it.</summary>
        /// <param name="channelFolderId">The target folder ID.</param>
        /// <param name="name">The optional new nonempty name.</param>
        /// <param name="description">The optional description; an empty string clears it.</param>
        /// <param name="isArchived">True to archive, false to unarchive, or null to leave unchanged.</param>
        /// <returns>Success or explicit failure details.</returns>
        public async Task<(bool success, string? details)> TryUpdate(
            int channelFolderId, string? name = null, string? description = null, bool? isArchived = null)
        {
            if (channelFolderId <= 0 || (name != null && string.IsNullOrWhiteSpace(name)))
                return (false, "ChannelFolders.Update failed: a positive folder ID and a nonempty name when supplied are required.");
            Dictionary<string, string> data = [];
            if (name != null) data.Add("name", name);
            if (description != null) data.Add("description", description);
            if (isArchived.HasValue) data.Add("is_archived", isArchived.Value ? "true" : "false");
            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch,
                $"{_endpoint}/{channelFolderId.ToString(CultureInfo.InvariantCulture)}", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "ChannelFolders.Update failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates or archives/unarchives a folder, throwing on failure.</summary>
        /// <param name="channelFolderId">The target folder ID.</param>
        /// <param name="name">The optional new name.</param>
        /// <param name="description">The optional new description.</param>
        /// <param name="isArchived">True to archive, false to unarchive, or null to leave unchanged.</param>
        /// <returns>The completed request.</returns>
        public async Task Update(int channelFolderId, string? name = null, string? description = null, bool? isArchived = null)
        {
            (bool success, string? details) = await TryUpdate(channelFolderId, name, description, isArchived);
            if (!success) throw new Exception(details);
        }

        /// <summary>Requires folder identities and meaningful display/archive data without requiring the newer order field.</summary>
        /// <param name="folders">The folder array.</param>
        /// <returns>An invalid-payload error, or null.</returns>
        private static string? ValidateFolders(JsonElement folders)
        {
            foreach (JsonElement folder in folders.EnumerateArray())
            {
                string? error = ResponsePayload.ValidateObjectId(folder, "id");
                if (error != null) return error;
                foreach (string field in new string[] { "name", "description", "rendered_description" })
                {
                    if (!folder.TryGetProperty(field, out JsonElement value) || value.ValueKind != JsonValueKind.String)
                        return $"required folder field '{field}' must be String.";
                }
                if (!folder.TryGetProperty("is_archived", out JsonElement archived) ||
                    archived.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return "required folder field 'is_archived' must be Boolean.";
            }
            return null;
        }
    }
}
