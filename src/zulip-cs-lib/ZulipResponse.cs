using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace zulip_cs_lib
{
    /// <summary>Deserialization target for Zulip API call responses.</summary>
    internal class ZulipResponse
    {
        /// <summary>The zulip result success.</summary>
        public const string ZulipResultSuccess = "success";

        /// <summary>Gets or sets a value indicating whether the success.</summary>
        [JsonIgnore]
        public bool Success { get; set; }

        /// <summary>Gets or sets the caught exception.</summary>
        [JsonIgnore]
        public string CaughtException { get; set; }

        /// <summary>Gets or sets the HTTP response code.</summary>
        [JsonIgnore]
        public int HttpResponseCode { get; set; }

        /// <summary>Gets or sets the HTTP response body.</summary>
        [JsonIgnore]
        public string HttpResponseBody { get; set; }

        /// <summary>Gets or sets the result.</summary>
        [JsonPropertyName("result")]
        public string Result { get; set; }

        /// <summary>Gets or sets the message.</summary>
        [JsonPropertyName("msg")]
        public string Message { get; set; }

        /// <summary>Gets or sets the rendered message.</summary>
        [JsonPropertyName("rendered")]
        public string RenderedMessage { get; set; }

        /// <summary>Gets or sets the error code.</summary>
        [JsonPropertyName("code")]
        public string ErrorCode { get; set; }

        /// <summary>Gets or sets the response ID.</summary>
        [JsonPropertyName("id")]
        public ulong? Id { get; set; }

        /// <summary>
        /// Gets or sets the deliver at time.
        /// Present for scheduled messages, encodes the time when the message will be sent.
        /// Note that scheduled messages ("Send later") is a beta API and may change before 
        /// it's a finished feature..
        /// </summary>
        [JsonPropertyName("deliver_at")]
        public string DeliverAt { get; set; }

        /// <summary>Gets or sets the location.</summary>
        [JsonPropertyName("uri")]
        public Uri ReturnedUri { get; set; }

        /// <summary>Gets or sets the name of the stream.</summary>
        [JsonPropertyName("stream")]
        public string StreamName { get; set; }

        /// <summary>Builds error message.</summary>
        /// <param name="basicMessage">Basic message used to start the message (e.g., prefix).</param>
        /// <returns>A string.</returns>
        public string GetFailureMessage()
        {
            if (!string.IsNullOrEmpty(CaughtException))
            {
                return CaughtException;
            }

            if (string.IsNullOrEmpty(Result))
            {
                return $"HTTP request failed: {HttpResponseCode}";
            }

            return $"result: {Result}," +
                $" code: {ErrorCode}," +
                $" message: {Message}";
        }
    }
}
