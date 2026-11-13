using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace zulip_cs_lib
{
    /// <summary>Posts a caller-owned stream to a focused multipart endpoint.</summary>
    /// <param name="endpoint">The API-relative endpoint.</param>
    /// <param name="fieldName">The multipart file field.</param>
    /// <param name="stream">The caller-owned readable stream.</param>
    /// <param name="filename">The multipart filename.</param>
    /// <param name="contentType">The MIME type.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>The response envelope and transport diagnostics.</returns>
    internal delegate Task<ZulipResponse> MultipartUploadDelegate(
        string endpoint, string fieldName, Stream stream, string filename, string contentType,
        CancellationToken cancellationToken);

    /// <summary>Shared input validation for actual multipart file endpoints.</summary>
    internal static class MultipartUpload
    {
        /// <summary>Validates multipart inputs without reading or taking ownership of the stream.</summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="contentType">The optional MIME type.</param>
        /// <returns>An explicit error, or null.</returns>
        internal static string? Validate(Stream stream, string filename, string? contentType)
        {
            if (stream == null || !stream.CanRead) return "stream must be readable.";
            if (string.IsNullOrWhiteSpace(filename) || filename.Any(char.IsControl))
                return "filename must be nonempty and contain no control characters.";
            string mediaType = contentType ?? "application/octet-stream";
            return mediaType.Any(char.IsControl) || !MediaTypeHeaderValue.TryParse(mediaType, out _)
                ? "contentType must be a valid MIME type without control characters."
                : null;
        }
    }
}
