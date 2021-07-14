using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Resources;

namespace zulip_cs_lib
{
    /// <summary>A Zulip client.</summary>
    public class ZulipClient
    {
        /// <summary>The site.</summary>
        private string _site;

        /// <summary>The user email.</summary>
        private string _userEmail;

        /// <summary>The API key.</summary>
        private string _apiKey;

        /// <summary>The HTTP client.</summary>
        private HttpClient _httpClient;

        /// <summary>URI of the site.</summary>
        private Uri _siteUri;

        /// <summary>The authentication header.</summary>
        private string _authHeader;

        /// <summary>The messages.</summary>
        private Messages _messages;

        /// <summary>The curl command.</summary>
        private string _curlCommand;

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="site">      The Zulip site URL.</param>
        /// <param name="userEmail"> The user email address.</param>
        /// <param name="apiKey">    The API key.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public ZulipClient(string site, string userEmail, string apiKey, HttpClient httpClient)
        {
            if (string.IsNullOrEmpty(site) ||
                (!Uri.TryCreate(site, UriKind.Absolute, out Uri siteUri)) ||
                ((siteUri.Scheme != Uri.UriSchemeHttp) && (siteUri.Scheme != Uri.UriSchemeHttps)))
            {
                throw new ArgumentException(nameof(site));
            };

            if (string.IsNullOrEmpty(userEmail) ||
                (!userEmail.Contains('@')))
            {
                throw new ArgumentException(nameof(userEmail));
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException(nameof(apiKey));
            }

            _site = site;
            _siteUri = siteUri;
            _userEmail = userEmail;
            _apiKey = apiKey;
            _httpClient = httpClient;
            _curlCommand = null;

            string authHeader = _userEmail + ":" + _apiKey;
            _authHeader = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authHeader));

            _messages = new Messages(DoZulipRequestHttpClient);
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or
        ///  illegal values.</exception>
        /// <param name="site">       The Zulip site URL.</param>
        /// <param name="userEmail">  The user email address.</param>
        /// <param name="apiKey">     The API key.</param>
        /// <param name="curlCommand">The curl command.</param>
        public ZulipClient(string site, string userEmail, string apiKey, string curlCommand)
        {
            if (string.IsNullOrEmpty(site) ||
                (!Uri.TryCreate(site, UriKind.Absolute, out Uri siteUri)) ||
                ((siteUri.Scheme != Uri.UriSchemeHttp) && (siteUri.Scheme != Uri.UriSchemeHttps)))
            {
                throw new ArgumentException(nameof(site));
            };

            if (string.IsNullOrEmpty(userEmail) ||
                (!userEmail.Contains('@')))
            {
                throw new ArgumentException(nameof(userEmail));
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException(nameof(apiKey));
            }

            _site = site;
            _siteUri = siteUri;
            _userEmail = userEmail;
            _apiKey = apiKey;
            _httpClient = null;
            _curlCommand = curlCommand;

            string authHeader = _userEmail + ":" + _apiKey;
            _authHeader = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authHeader));

            _messages = new Messages(DoZulipRequestCurl);
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
        : this(zuliprcFilename, true)
        {
            _messages = new Messages(DoZulipRequestHttpClient);
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="zuliprcFilename">Filename of the zuliprc file.</param>
        private ZulipClient(string zuliprcFilename, bool createHttpClient)
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

            string site = apiData["site"];
            string userEmail = apiData["email"];
            string apiKey = apiData["key"];

            if (string.IsNullOrEmpty(site) ||
                (!Uri.TryCreate(site, UriKind.Absolute, out Uri siteUri)) ||
                ((siteUri.Scheme != Uri.UriSchemeHttp) && (siteUri.Scheme != Uri.UriSchemeHttps)))
            {
                throw new ArgumentException("site");
            };

            if (string.IsNullOrEmpty(userEmail) ||
                (!userEmail.Contains('@')))
            {
                throw new ArgumentException("email");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("key");
            }

            _site = site;
            _siteUri = siteUri;
            _userEmail = userEmail;
            _apiKey = apiKey;
            _curlCommand = null;

            if (createHttpClient)
            {
                _httpClient = new HttpClient();
            }
            else
            {
                _httpClient = null;
            }

            string authHeader = _userEmail + ":" + _apiKey;
            _authHeader = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authHeader));
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <exception cref="FileNotFoundException">Thrown when the requested file is not present.</exception>
        /// <exception cref="KeyNotFoundException"> Thrown when a Key Not Found error condition occurs.</exception>
        /// <param name="zuliprcFilename">   Filename of the zuliprc file.</param>
        /// <param name="httpMessageHandler">The HTTP message handler.</param>
        public ZulipClient(string zuliprcFilename, HttpClient httpClient)
        : this(zuliprcFilename, false)
        {
            _httpClient = httpClient;
            _messages = new Messages(DoZulipRequestHttpClient);
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="zuliprcFilename">Filename of the zuliprc file.</param>
        /// <param name="curlCommand">    The curl command.</param>
        public ZulipClient(string zuliprcFilename, string curlCommand)
        : this(zuliprcFilename, false)
        {
            _curlCommand = curlCommand;
            _messages = new Messages(DoZulipRequestCurl);
        }

        /// <summary>Gets the Messages Interface.</summary>
        public Messages Messages => _messages;

        /// <summary>Executes the zulip request via curl.</summary>
        /// <param name="httpMethod"> The HTTP method.</param>
        /// <param name="relativeUrl">URL of the relative.</param>
        /// <param name="data">       The data.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        internal async Task<ZulipResponse> DoZulipRequestCurl(
            HttpMethod httpMethod,
            string relativeUrl,
            Dictionary<string, string> data)
        {
            ZulipResponse zulipResponse;

            try
            {
                Uri requestUri = new Uri(_siteUri, relativeUrl);

                string args =
                    $"-X {httpMethod.Method.ToUpperInvariant()}" +
                    $" {requestUri.AbsoluteUri}" +
                    $" -u {_userEmail}:{_apiKey}";

                foreach (KeyValuePair<string, string> kvp in data)
                {
                    args += $" --data-urlencode \"{kvp.Key}={kvp.Value}\"";
                }

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = _curlCommand,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                Process proc = Process.Start(startInfo);
                string body = await proc.StandardOutput.ReadToEndAsync();
                string errors = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                if (string.IsNullOrEmpty(body))
                {
                    zulipResponse = new ZulipResponse();

                    if (!string.IsNullOrEmpty(errors))
                    {
                        zulipResponse.CaughtException = errors;
                    }
                    else
                    {
                        zulipResponse.CaughtException = "curl returned no data!";
                    }

                    zulipResponse.Success = false;
                    return zulipResponse;
                }

                if (body[0] != '{')
                {
                    zulipResponse = new ZulipResponse();
                    zulipResponse.CaughtException = "curl returned non-JSON data!";
                    zulipResponse.HttpResponseBody = body;
                    zulipResponse.Success = false;
                    return zulipResponse;
                }

                try
                {
                    zulipResponse = JsonSerializer.Deserialize<ZulipResponse>(body);
                    zulipResponse.Success = true;
                }
                catch (Exception parseEx)
                {
                    // ignore parse exceptions for now
                    zulipResponse = new ZulipResponse();
                    zulipResponse.CaughtException = parseEx.Message;
                    zulipResponse.Success = false;
                }

                zulipResponse.HttpResponseCode = 0;
                zulipResponse.HttpResponseBody = body;
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"ZulipClient <<< exception: {httpEx.Message}");
                Console.WriteLine($"ZulipClient <<< inner: {httpEx.InnerException}");
                zulipResponse = new ZulipResponse();
                zulipResponse.CaughtException = httpEx.Message;
                zulipResponse.Success = false;
            }

            return zulipResponse;
        }

        /// <summary>Post this message.</summary>
        /// <param name="httpMethod"> The HTTP method.</param>
        /// <param name="relativeUrl">URL of the relative.</param>
        /// <param name="data">       The data.</param>
        /// <returns>A HttpResponseMessage.</returns>
        internal async Task<ZulipResponse> DoZulipRequestHttpClient(
            HttpMethod httpMethod, 
            string relativeUrl, 
            Dictionary<string, string> data)
        {
            ZulipResponse zulipResponse;

            try
            {
                Uri requestUri = new Uri(_siteUri, relativeUrl);

                HttpRequestMessage request = new HttpRequestMessage(httpMethod, requestUri);

                if ((data != null) && (data.Count != 0))
                {
                    request.Content = new FormUrlEncodedContent(data);
                }

                request.Headers.Add("Authorization", _authHeader);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                string body = await response.Content.ReadAsStringAsync();

                try
                {
                    zulipResponse = JsonSerializer.Deserialize<ZulipResponse>(body);

                    zulipResponse.Success = true;
                }
                catch (Exception parseEx)
                {
                    // ignore parse exceptions for now
                    zulipResponse = new ZulipResponse();
                    zulipResponse.CaughtException = parseEx.Message;
                    zulipResponse.Success = false;
                }

                zulipResponse.HttpResponseCode = ((int)response.StatusCode);
                zulipResponse.HttpResponseBody = body;
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"ZulipClient <<< exception: {httpEx.Message}");
                Console.WriteLine($"ZulipClient <<< inner: {httpEx.InnerException}");
                zulipResponse = new ZulipResponse();
                zulipResponse.CaughtException = httpEx.Message;
                zulipResponse.Success = false;
            }

            return zulipResponse;
        }
    }
}
