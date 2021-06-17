using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace zulip_cs_lib
{
    public class ZulipResponse
    {
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

        /// <summary>Gets or sets the response ID.</summary>
        [JsonPropertyName("id")]
        public ulong? Id { get; set; }

        /// <summary>Gets or sets the location.</summary>
        [JsonPropertyName("uri")]
        public Uri ReturnedUri { get; set; }
    }
}
