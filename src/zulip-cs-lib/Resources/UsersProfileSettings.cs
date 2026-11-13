using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Personal preference, custom profile data and profile picture workflows.</summary>
    public partial class Users
    {
        /// <summary>Updates selected personal preferences, leaving omitted settings unchanged.</summary>
        /// <param name="options">The supplied preferences; an empty options object is allowed.</param>
        /// <remarks>Requires feature level 80 for the unified settings endpoint; EnterSends requires 81. Server validation and unsupported-parameter warnings remain authoritative.</remarks>
        /// <returns>Success or explicit failure details.</returns>
        public async Task<(bool success, string? details)> TryUpdateSettings(UserSettingsOptions options)
        {
            if (options == null) return (false, "Users.UpdateSettings failed: options are required.");
            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, "api/v1/settings", options.ToForm());
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "Users.UpdateSettings failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates selected personal preferences, throwing on failure.</summary>
        /// <param name="options">The supplied preferences.</param>
        /// <returns>The completed request.</returns>
        public async Task UpdateSettings(UserSettingsOptions options)
        {
            (bool success, string? details) = await TryUpdateSettings(options);
            if (!success) throw new Exception(details);
        }

        /// <summary>Updates custom profile data using strings, explicit nulls or arrays of user IDs.</summary>
        /// <param name="data">The field updates; an empty array is allowed.</param>
        /// <remarks>Field types, lengths and whether the current user may edit each field are enforced by the server.</remarks>
        /// <returns>Success or explicit failure details.</returns>
        public async Task<(bool success, string? details)> TryUpdateProfileData(ProfileDataInput[] data)
        {
            if (data == null) return (false, "Users.UpdateProfileData failed: data is required.");
            foreach (ProfileDataInput field in data)
            {
                string? error = field == null ? "profile updates must not contain null entries." : field.Validate();
                if (error != null) return (false, "Users.UpdateProfileData failed: " + error);
            }
            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_usersApiEndpoint}/me/profile_data",
                new Dictionary<string, string> { { "data", JsonSerializer.Serialize(data) } });
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "Users.UpdateProfileData failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates custom profile data, throwing on failure.</summary>
        /// <param name="data">The field updates.</param>
        /// <returns>The completed request.</returns>
        public async Task UpdateProfileData(ProfileDataInput[] data)
        {
            (bool success, string? details) = await TryUpdateProfileData(data);
            if (!success) throw new Exception(details);
        }

        /// <summary>Removes values for explicitly selected custom profile fields.</summary>
        /// <param name="fieldIds">The custom profile field IDs; an empty array is allowed.</param>
        /// <returns>Success or explicit failure details.</returns>
        public async Task<(bool success, string? details)> TryRemoveProfileData(int[] fieldIds)
        {
            if (fieldIds == null || Array.Exists(fieldIds, id => id <= 0))
                return (false, "Users.RemoveProfileData failed: fieldIds must be an array of positive integer IDs.");
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_usersApiEndpoint}/me/profile_data",
                new Dictionary<string, string> { { "data", JsonSerializer.Serialize(fieldIds) } });
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "Users.RemoveProfileData failed: " + response.GetFailureMessage());
        }

        /// <summary>Removes selected custom profile field values, throwing on failure.</summary>
        /// <param name="fieldIds">The custom profile field IDs.</param>
        /// <returns>The completed request.</returns>
        public async Task RemoveProfileData(int[] fieldIds)
        {
            (bool success, string? details) = await TryRemoveProfileData(fieldIds);
            if (!success) throw new Exception(details);
        }

        /// <summary>Uploads a profile picture as the documented multipart file field.</summary>
        /// <param name="stream">The caller-owned readable image stream.</param>
        /// <param name="filename">The multipart filename.</param>
        /// <param name="contentType">The MIME type, defaulting to application/octet-stream.</param>
        /// <param name="cancellationToken">Caller cancellation for the upload.</param>
        /// <remarks>The client's UploadTimeout applies. The stream remains caller-owned on all outcomes; its position may advance. Image formats, max_avatar_file_size_mib and organization restrictions are server-enforced.</remarks>
        /// <returns>Success and the new profile picture URL, or explicit failure details.</returns>
        public async Task<(bool success, string? details, string? avatarUrl)> TryUploadAvatar(
            Stream stream, string filename, string? contentType = null, CancellationToken cancellationToken = default)
        {
            string? error = MultipartUpload.Validate(stream, filename, contentType);
            if (error != null) return (false, "Users.UploadAvatar failed: " + error, null);
            if (_doUpload == null)
                return (false, "Users.UploadAvatar failed: a multipart upload delegate is not configured.", null);
            ZulipResponse response = await _doUpload($"{_usersApiEndpoint}/me/avatar", "file",
                stream, filename, contentType ?? "application/octet-stream", cancellationToken);
            return ReadAvatarUrl(response, "Users.UploadAvatar");
        }

        /// <summary>Uploads a caller-owned profile picture stream, throwing on failure.</summary>
        /// <param name="stream">The caller-owned readable image stream.</param>
        /// <param name="filename">The multipart filename.</param>
        /// <param name="contentType">The optional MIME type.</param>
        /// <param name="cancellationToken">Caller cancellation for the upload.</param>
        /// <returns>The new profile picture URL.</returns>
        public async Task<string> UploadAvatar(
            Stream stream, string filename, string? contentType = null, CancellationToken cancellationToken = default)
        {
            (bool success, string? details, string? avatarUrl) = await TryUploadAvatar(stream, filename, contentType, cancellationToken);
            if (!success) throw new Exception(details);
            return avatarUrl!;
        }

        /// <summary>Deletes the uploaded profile picture and returns the organization's default picture URL.</summary>
        /// <remarks>Idempotent from feature level 443. Organization restrictions on profile picture changes are server-enforced.</remarks>
        /// <returns>Success and the resulting profile picture URL, or explicit failure details.</returns>
        public async Task<(bool success, string? details, string? avatarUrl)> TryDeleteAvatar()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_usersApiEndpoint}/me/avatar", null);
            return ReadAvatarUrl(response, "Users.DeleteAvatar");
        }

        /// <summary>Deletes the uploaded profile picture, throwing on failure.</summary>
        /// <returns>The resulting default profile picture URL.</returns>
        public async Task<string> DeleteAvatar()
        {
            (bool success, string? details, string? avatarUrl) = await TryDeleteAvatar();
            if (!success) throw new Exception(details);
            return avatarUrl!;
        }

        /// <summary>Requires the actual nonempty avatar_url for both profile picture operations.</summary>
        /// <param name="response">The completed response.</param>
        /// <param name="operation">The operation name for failure details.</param>
        /// <returns>The required URL, or explicit failure details.</returns>
        private static (bool success, string? details, string? avatarUrl) ReadAvatarUrl(
            ZulipResponse response, string operation)
        {
            if (ResponsePayload.TryRead<string>(response, operation, "avatar_url", JsonValueKind.String,
                out string? url, out string? details, value => string.IsNullOrWhiteSpace(value.GetString())
                    ? "required field 'avatar_url' must be nonempty." : null))
                return (true, null, url);
            return (false, details, null);
        }
    }
}
