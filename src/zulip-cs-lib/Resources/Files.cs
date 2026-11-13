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
    /// <summary>Streaming uploads and metadata/deletion for the requesting user's attachments.</summary>
    public sealed class Files
    {
        /// <summary>The required attachment text fields, in validation order.</summary>
        private static readonly string[] _attachmentTextFields = ["name", "path_id"];

        /// <summary>The required attachment integer fields, in validation order.</summary>
        private static readonly string[] _attachmentIntegerFields = ["size", "create_time"];

        /// <summary>The ordinary API request delegate.</summary>
        private readonly Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>The focused, caller-owned stream upload delegate.</summary>
        private readonly Func<Stream, string, string, CancellationToken, Task<ZulipResponse>> _doUpload;

        /// <summary>Initializes the files resource without changing ordinary request delegates.</summary>
        /// <param name="doZulipRequest">The ordinary request delegate.</param>
        /// <param name="doUpload">The streaming upload delegate.</param>
        internal Files(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest,
            Func<Stream, string, string, CancellationToken, Task<ZulipResponse>> doUpload)
        {
            _doZulipRequest = doZulipRequest;
            _doUpload = doUpload;
        }

        /// <summary>Uploads the stream's remaining bytes as multipart field filename.</summary>
        /// <remarks>
        /// The stream remains caller-owned and open on success, error and cancellation; its position advances.
        /// UploadTimeout defaults to 100 seconds on the client. Cancellation/timeout returns a Try failure.
        /// Requires feature level 272 for the required url field; the feature-level-285 filename is optional.
        /// No resumable upload, whole-upload buffering, temporary content files or automatic retry is performed.
        /// </remarks>
        /// <param name="stream">A readable caller-owned stream, including nonseekable streams.</param>
        /// <param name="filename">The multipart filename, without control characters.</param>
        /// <param name="contentType">The MIME content type, defaulting to application/octet-stream.</param>
        /// <param name="cancellationToken">Cancellation for this upload.</param>
        /// <returns>The Try outcome and uploaded link with request-local diagnostics.</returns>
        public async Task<(bool success, string? details, FileUploadResult? result)> TryUpload(
            Stream stream, string filename, string? contentType = null, CancellationToken cancellationToken = default)
        {
            string? error = MultipartUpload.Validate(stream, filename, contentType);
            if (error != null) return (false, "Files.Upload failed: " + error, null);
            string mediaType = contentType ?? "application/octet-stream";

            ZulipResponse response = await _doUpload(stream, filename, mediaType, cancellationToken);
            if (!ResponsePayload.TryRead<string>(response, "Files.Upload", "url", JsonValueKind.String,
                out string? url, out string? details, ValidateUploadUrl))
            {
                return (false, details, null);
            }

            string? storedFilename = null;
            if (response.RawPayload.TryGetProperty("filename", out _) &&
                !ResponsePayload.TryRead<string>(response, "Files.Upload", "filename", JsonValueKind.String,
                    out storedFilename, out details))
            {
                return (false, details, null);
            }
            return (true, null, new FileUploadResult(url!, storedFilename, response));
        }

        /// <summary>Uploads a caller-owned stream, throwing on failure.</summary>
        /// <param name="stream">The readable caller-owned stream.</param>
        /// <param name="filename">The multipart filename.</param>
        /// <param name="contentType">The optional MIME content type.</param>
        /// <param name="cancellationToken">Cancellation for this upload.</param>
        /// <returns>The uploaded link and request-local diagnostics.</returns>
        public async Task<FileUploadResult> Upload(
            Stream stream, string filename, string? contentType = null, CancellationToken cancellationToken = default)
        {
            (bool success, string? details, FileUploadResult? result) outcome =
                await TryUpload(stream, filename, contentType, cancellationToken);
            if (!outcome.success) throw new Exception(outcome.details);
            return outcome.result!;
        }

        /// <summary>Gets this user's attachments and organization-wide uploaded bytes.</summary>
        /// <remarks>Uses message_ids from feature level 472 and create_time in seconds from feature level 443.</remarks>
        /// <returns>The Try outcome and validated metadata, including an empty attachment list.</returns>
        public async Task<(bool success, string? details, AttachmentListResult? result)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/attachments", null);
            if (ResponsePayload.TryRead<List<AttachmentObject>>(response, "Files.GetAll", "attachments", JsonValueKind.Array,
                out List<AttachmentObject>? attachments, out string? details, ValidateAttachments) &&
                ResponsePayload.TryRead<long>(response, "Files.GetAll", "upload_space_used", JsonValueKind.Number,
                    out long used, out details, value => ValidateNonnegativeInteger(value, "upload_space_used")))
            {
                return (true, null, new AttachmentListResult(attachments!, used, response));
            }
            return (false, details, null);
        }

        /// <summary>Gets attachment metadata and storage usage, throwing on failure.</summary>
        /// <returns>The validated metadata and request-local diagnostics.</returns>
        public async Task<AttachmentListResult> GetAll()
        {
            (bool success, string? details, AttachmentListResult? result) outcome = await TryGetAll();
            if (!outcome.success) throw new Exception(outcome.details);
            return outcome.result!;
        }

        /// <summary>Deletes an uploaded file using its attachment ID, not its path or message ID.</summary>
        /// <param name="attachmentId">The attachment identity from GetAll.</param>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryDelete(int attachmentId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete,
                "api/v1/attachments/" + attachmentId.ToString(CultureInfo.InvariantCulture), null);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, "Files.Delete failed: " + response.GetFailureMessage());
        }

        /// <summary>Deletes an attachment, throwing on failure.</summary>
        /// <param name="attachmentId">The attachment identity from GetAll.</param>
        public async Task Delete(int attachmentId)
        {
            (bool success, string? details) outcome = await TryDelete(attachmentId);
            if (!outcome.success) throw new Exception(outcome.details);
        }

        /// <summary>Gets an immediately usable unauthenticated temporary URL for a logical attachment path (FL1).</summary>
        /// <remarks>Do not persist this URL. Its lifetime is server-configured and generally 60 seconds.</remarks>
        /// <param name="pathId">The complete logical path_id, including realm ID, not an upload URL.</param>
        /// <returns>The temporary URL, or explicit failure.</returns>
        public async Task<(bool success, string? details, string? url)> TryGetTemporaryUrl(string pathId)
        {
            if (!TryEncodeAttachmentPath(pathId, out string path))
                return (false, "Files.GetTemporaryUrl failed: pathId must contain a positive realm ID and nonempty, non-traversal file segments.", null);
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/user_uploads/" + path, null);
            return ResponsePayload.TryRead<string>(response, "Files.GetTemporaryUrl", "url", JsonValueKind.String,
                out string? url, out string? details,
                value => string.IsNullOrWhiteSpace(value.GetString()) ? "url must be nonempty." : null)
                ? (true, null, url)
                : (false, details, null);
        }

        /// <summary>Gets an ephemeral public URL, throwing on failure (FL1).</summary>
        /// <param name="pathId">The complete logical attachment path_id.</param>
        /// <returns>The temporary URL, which should be used immediately and not stored.</returns>
        public async Task<string> GetTemporaryUrl(string pathId)
        {
            var result = await TryGetTemporaryUrl(pathId);
            if (!result.success) throw new Exception(result.details);
            return result.url!;
        }

        /// <summary>Checks once whether an attachment thumbnail exists (FL479).</summary>
        /// <remarks>False is a successful not-ready state; polling policy belongs to the caller.</remarks>
        /// <param name="pathId">The complete logical attachment path_id.</param>
        /// <returns>The actual thumbnail state, or explicit failure.</returns>
        public async Task<(bool success, string? details, bool hasThumbnail)> TryCheckThumbnailStatus(string pathId)
        {
            if (!TryEncodeAttachmentPath(pathId, out string path))
                return (false, "Files.CheckThumbnailStatus failed: pathId must contain a positive realm ID and nonempty, non-traversal file segments.", false);
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/thumbnail/status/" + path, null);
            if (!ResponsePayload.TryRead<JsonElement>(response, "Files.CheckThumbnailStatus", null,
                JsonValueKind.Object, out JsonElement value, out string? details,
                root => root.TryGetProperty("has_thumbnail", out JsonElement thumbnail) &&
                    thumbnail.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? null : "required field 'has_thumbnail' must be a boolean."))
                return (false, details, false);
            return (true, null, value.GetProperty("has_thumbnail").GetBoolean());
        }

        /// <summary>Checks thumbnail readiness once, throwing on failure (FL479).</summary>
        /// <param name="pathId">The complete logical attachment path_id.</param>
        /// <returns>True when the thumbnail exists; false when not ready.</returns>
        public async Task<bool> CheckThumbnailStatus(string pathId)
        {
            var result = await TryCheckThumbnailStatus(pathId);
            if (!result.success) throw new Exception(result.details);
            return result.hasThumbnail;
        }

        /// <summary>Validates a complete logical attachment identity and encodes its catch-all path.</summary>
        /// <param name="pathId">The attachment identity.</param>
        /// <param name="encoded">The encoded identity.</param>
        /// <returns>Whether the path has an integer realm ID and valid file segments.</returns>
        private static bool TryEncodeAttachmentPath(string pathId, out string encoded)
        {
            encoded = string.Empty;
            if (pathId == null) return false;
            int slash = pathId.IndexOf('/');
            return slash > 0 && int.TryParse(pathId.AsSpan(0, slash), NumberStyles.None,
                CultureInfo.InvariantCulture, out int realmId) && realmId > 0 &&
                ApiPath.TryEncodeSegments(pathId, out encoded);
        }

        /// <summary>Requires a nonempty server-relative upload link rather than a legacy uri or absolute URL.</summary>
        /// <param name="value">The url field.</param>
        /// <returns>A validation error, or null.</returns>
        private static string? ValidateUploadUrl(JsonElement value)
        {
            string? url = value.GetString();
            return string.IsNullOrWhiteSpace(url) || !url.StartsWith("/", StringComparison.Ordinal) ||
                url.StartsWith("//", StringComparison.Ordinal) || url.Contains('\\') ||
                url.Any(char.IsControl) || !Uri.TryCreate(url, UriKind.Relative, out _)
                ? "url must be a nonempty server-relative URL."
                : null;
        }

        /// <summary>Validates current attachment metadata without inventing missing numeric defaults.</summary>
        /// <param name="array">The attachments array.</param>
        /// <returns>A validation error, or null.</returns>
        private static string? ValidateAttachments(JsonElement array)
        {
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("id", out JsonElement id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt32(out _) ||
                    !item.TryGetProperty("message_ids", out JsonElement ids) || ids.ValueKind != JsonValueKind.Array)
                {
                    return "attachments require integer id and message_ids array.";
                }
                foreach (JsonElement messageId in ids.EnumerateArray())
                {
                    if (messageId.ValueKind != JsonValueKind.Number || !messageId.TryGetInt32(out _))
                        return "message_ids must contain integer IDs.";
                }
                foreach (string field in _attachmentTextFields)
                {
                    if (!item.TryGetProperty(field, out JsonElement text) || text.ValueKind != JsonValueKind.String)
                        return $"attachments require string {field}.";
                }
                foreach (string field in _attachmentIntegerFields)
                {
                    if (!item.TryGetProperty(field, out JsonElement number) ||
                        ValidateNonnegativeInteger(number, field) != null)
                        return $"attachments require nonnegative integer {field}.";
                }
            }
            return null;
        }

        /// <summary>Requires a representable nonnegative byte count or timestamp.</summary>
        /// <param name="value">The numeric field.</param>
        /// <param name="field">Its wire name.</param>
        /// <returns>A validation error, or null.</returns>
        private static string? ValidateNonnegativeInteger(JsonElement value, string field) =>
            value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out long number) || number < 0
                ? $"{field} must be a nonnegative integer."
                : null;
    }
}
