namespace zulip_cs_lib
{
    /// <summary>The link returned by a successful file upload.</summary>
    public sealed class FileUploadResult
    {
        /// <summary>Initializes an upload result.</summary>
        /// <param name="url">The required server-relative URL.</param>
        /// <param name="filename">The optional server-selected filename.</param>
        /// <param name="response">The completed request.</param>
        internal FileUploadResult(string url, string? filename, ZulipResponse response)
        {
            Url = url;
            Filename = filename;
            Response = response;
        }

        /// <summary>Gets the server-relative URL (feature level 272), ready to include in a message.</summary>
        public string Url { get; }

        /// <summary>Gets the server-selected filename when supplied (feature level 285).</summary>
        public string? Filename { get; }

        /// <summary>Gets this request's response, including warnings and unknown fields.</summary>
        public ZulipResponse Response { get; }
    }
}
