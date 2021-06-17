using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace zulip_cs_lib
{
    /// <summary>A Zulip client.</summary>
    public partial class ZulipClient
    {
        private string _site;
        private string _userEmail;
        private string _apiKey;

        /// <summary>The HTTP client.</summary>
        private HttpClient _httpClient;

        private Uri _siteUri;

        private string _authHeader;

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="site">      The Zulip site URL.</param>
        /// <param name="userEmail"> The user email address.</param>
        /// <param name="apiKey">    The API key.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public ZulipClient(string site, string userEmail, string apiKey, HttpClient httpClient)
        {
            _site = site;
            _siteUri = new Uri(site);
            _userEmail = userEmail;
            _apiKey = apiKey;
            _httpClient = httpClient;

            string authHeader = _userEmail + ":" + _apiKey;
            _authHeader = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authHeader));
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="site">     The Zulip site URL.</param>
        /// <param name="userEmail">The user email address.</param>
        /// <param name="apiKey">   The API key.</param>
        public ZulipClient(string site, string userEmail, string apiKey)
        : this(site, userEmail, apiKey, new HttpClient())
        {
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="zuliprcFilename">Filename of the zuliprc file.</param>
        public ZulipClient(string zuliprcFilename)
        : this(zuliprcFilename, new HttpClient())
        {
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <exception cref="FileNotFoundException">Thrown when the requested file is not present.</exception>
        /// <exception cref="KeyNotFoundException"> Thrown when a Key Not Found error condition occurs.</exception>
        /// <param name="zuliprcFilename">   Filename of the zuliprc file.</param>
        /// <param name="httpMessageHandler">The HTTP message handler.</param>
        public ZulipClient(string zuliprcFilename, HttpClient httpClient)
        {
            if (!File.Exists(zuliprcFilename))
            {
                throw new FileNotFoundException(zuliprcFilename);
            }

            if (!IniParser.TryGetSectionDataFromFile(zuliprcFilename, "api", out Dictionary<string, string> apiData))
            {
                throw new KeyNotFoundException($"File: {zuliprcFilename} does not contain [api] data!");
            }

            if (!apiData.ContainsKey("email"))
            {
                throw new KeyNotFoundException($"File: {zuliprcFilename} does not contain an `email` value!");
            }

            if (!apiData.ContainsKey("key"))
            {
                throw new KeyNotFoundException($"File: {zuliprcFilename} does not contain a `key` value!");
            }

            if (!apiData.ContainsKey("site"))
            {
                throw new KeyNotFoundException($"File: {zuliprcFilename} does not contain a `site` value!");
            }

            _site = apiData["site"];
            _siteUri = new Uri(_site);
            _userEmail = apiData["email"];
            _apiKey = apiData["key"];
            _httpClient = httpClient;

            string authHeader = _userEmail + ":" + _apiKey;
            _authHeader = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authHeader));
        }

        /// <summary>Post this message.</summary>
        /// <param name="relativeUrl">URL of the relative.</param>
        /// <param name="data">       The data.</param>
        /// <returns>A HttpResponseMessage.</returns>
        internal async Task<ZulipResponse> PostAsync(string relativeUrl, Dictionary<string, string> data)
        {
            Uri requestUri = new Uri(_siteUri, relativeUrl);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            request.Content = new FormUrlEncodedContent(data);
            request.Headers.Add("Authorization", _authHeader);

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            string body = await response.Content.ReadAsStringAsync();
            ZulipResponse zulipResponse;

            try
            {
                zulipResponse = JsonSerializer.Deserialize<ZulipResponse>(body);
            }
            catch (Exception)
            {
                // ignore parse exceptions for now
                zulipResponse = new ZulipResponse();
            }

            zulipResponse.HttpResponseCode = ((int)response.StatusCode);
            zulipResponse.HttpResponseBody = body;

            return zulipResponse;
        }
    }
}
