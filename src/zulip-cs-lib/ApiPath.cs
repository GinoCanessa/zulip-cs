using System;

namespace zulip_cs_lib
{
    /// <summary>Encodes logical slash-delimited API paths without traversal or query injection.</summary>
    internal static class ApiPath
    {
        /// <summary>Encodes each segment of a catch-all API path.</summary>
        /// <param name="path">The logical path, not an already escaped URL.</param>
        /// <param name="encoded">The encoded slash-delimited path.</param>
        /// <returns>Whether all segments are nonempty and non-traversal.</returns>
        internal static bool TryEncodeSegments(string path, out string encoded)
        {
            encoded = string.Empty;
            if (string.IsNullOrWhiteSpace(path)) return false;
            string[] segments = path.Split('/');
            for (int index = 0; index < segments.Length; index++)
            {
                if (string.IsNullOrEmpty(segments[index]) || segments[index] is "." or "..") return false;
                segments[index] = Uri.EscapeDataString(segments[index]);
            }
            encoded = string.Join("/", segments);
            return true;
        }
    }
}
