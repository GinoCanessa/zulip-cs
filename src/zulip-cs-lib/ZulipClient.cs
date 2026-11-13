using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private string _site = null!;

        /// <summary>The user email.</summary>
        private string _userEmail = null!;

        /// <summary>The API key.</summary>
        private string _apiKey = null!;

        /// <summary>The HTTP client.</summary>
        private HttpClient? _httpClient;

        /// <summary>URI of the site.</summary>
        private Uri _siteUri = null!;

        /// <summary>The authentication header.</summary>
        private string _authHeader = null!;

        /// <summary>The messages.</summary>
        private Messages _messages = null!;

        /// <summary>The channels.</summary>
        private Channels _channels = null!;

        /// <summary>The users.</summary>
        private Users _users = null!;

        /// <summary>The server.</summary>
        private Server _server = null!;

        /// <summary>The events.</summary>
        private Events _events = null!;

        /// <summary>The scheduled messages.</summary>
        private ScheduledMessages _scheduledMessages = null!;

        /// <summary>The drafts.</summary>
        private Drafts _drafts = null!;

        /// <summary>The invitations.</summary>
        private Invitations _invitations = null!;

        /// <summary>The organization.</summary>
        private Organization _organization = null!;

        /// <summary>The saved snippets.</summary>
        private SavedSnippets _savedSnippets = null!;

        /// <summary>The reminders.</summary>
        private Reminders _reminders = null!;

        /// <summary>The navigation views.</summary>
        private NavigationViews _navigationViews = null!;

        /// <summary>The curl command.</summary>
        private string? _curlCommand;

        /// <summary>Gets diagnostics and warnings for the most recently completed request, or null before any request completes.</summary>
        /// <remarks>Concurrent callers must use detailed results for per-operation association; completion order is not invocation order.</remarks>
        public ZulipResponse? LastResponse { get; private set; }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="site">      The Zulip site URL.</param>
        /// <param name="userEmail"> The user email address.</param>
        /// <param name="apiKey">    The API key.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public ZulipClient(string site, string userEmail, string apiKey, HttpClient httpClient)
        {
            if (string.IsNullOrEmpty(site) ||
                (!Uri.TryCreate(site, UriKind.Absolute, out Uri? siteUri)) ||
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

            InitializeResources(DoZulipRequestHttpClient);
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
                (!Uri.TryCreate(site, UriKind.Absolute, out Uri? siteUri)) ||
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

            InitializeResources(DoZulipRequestCurl);
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
            InitializeResources(DoZulipRequestHttpClient);
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="zuliprcFilename">Filename of the zuliprc file.</param>
        private ZulipClient(string zuliprcFilename, bool createHttpClient)
        {
            if (!File.Exists(zuliprcFilename))
            {
                throw new FileNotFoundException(zuliprcFilename);
            }

            if (!IniParser.TryGetSectionDataFromFile(zuliprcFilename, "api", out Dictionary<string, string>? apiData))
            {
                throw new KeyNotFoundException($"File: {zuliprcFilename} does not contain [api] data!");
            }

            if (apiData == null || !apiData.ContainsKey("email"))
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
                (!Uri.TryCreate(site, UriKind.Absolute, out Uri? siteUri)) ||
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
            InitializeResources(DoZulipRequestHttpClient);
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="zuliprcFilename">Filename of the zuliprc file.</param>
        /// <param name="curlCommand">    The curl command.</param>
        public ZulipClient(string zuliprcFilename, string curlCommand)
        : this(zuliprcFilename, false)
        {
            _curlCommand = curlCommand;
            InitializeResources(DoZulipRequestCurl);
        }

        /// <summary>Gets the Messages Interface.</summary>
        public Messages Messages => _messages;

        /// <summary>Gets the Channels Interface.</summary>
        public Channels Channels => _channels;

        /// <summary>Gets the Users Interface.</summary>
        public Users Users => _users;

        /// <summary>Gets the Server Interface.</summary>
        public Server Server => _server;

        /// <summary>Gets the Events Interface.</summary>
        public Events Events => _events;

        /// <summary>Gets the Scheduled Messages Interface.</summary>
        public ScheduledMessages ScheduledMessages => _scheduledMessages;

        /// <summary>Gets the Drafts Interface.</summary>
        public Drafts Drafts => _drafts;

        /// <summary>Gets the Invitations Interface.</summary>
        public Invitations Invitations => _invitations;

        /// <summary>Gets the Organization Interface.</summary>
        public Organization Organization => _organization;

        /// <summary>Gets the Saved Snippets Interface.</summary>
        public SavedSnippets SavedSnippets => _savedSnippets;

        /// <summary>Gets the Reminders Interface.</summary>
        public Reminders Reminders => _reminders;

        /// <summary>Gets the Navigation Views Interface.</summary>
        public NavigationViews NavigationViews => _navigationViews;

        /// <summary>Initializes all resource classes.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        private void InitializeResources(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest)
        {
            _messages = new Messages(doZulipRequest);
            _channels = new Channels(doZulipRequest);
            _users = new Users(doZulipRequest);
            _server = new Server(doZulipRequest);
            _events = new Events(doZulipRequest);
            _scheduledMessages = new ScheduledMessages(doZulipRequest);
            _drafts = new Drafts(doZulipRequest);
            _invitations = new Invitations(doZulipRequest);
            _organization = new Organization(doZulipRequest);
            _savedSnippets = new SavedSnippets(doZulipRequest);
            _reminders = new Reminders(doZulipRequest);
            _navigationViews = new NavigationViews(doZulipRequest);
        }

        /// <summary>Fetches an API key from the server using username and password.</summary>
        /// <param name="site">The Zulip site URL.</param>
        /// <param name="username">The username (email).</param>
        /// <param name="password">The password.</param>
        /// <param name="httpClient">(Optional) The HTTP client to use.</param>
        /// <returns>An asynchronous result that yields (success, details, apiKey, email).</returns>
        public static async Task<(bool success, string? details, string? apiKey, string? email)> TryFetchApiKey(
            string site, string username, string password, HttpClient? httpClient = null)
        {
            HttpClient client = httpClient ?? new HttpClient();

            try
            {
                if (!Uri.TryCreate(site, UriKind.Absolute, out Uri? siteUri))
                {
                    return (false, "Invalid site URL", null, null);
                }

                Uri requestUri = new Uri(siteUri, "api/v1/fetch_api_key");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "username", username },
                    { "password", password },
                });

                HttpResponseMessage response = await client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                ZulipResponse zulipResponse = JsonSerializer.Deserialize<ZulipResponse>(body)
                    ?? throw new JsonException("The Zulip response must be a JSON object.");

                zulipResponse.HttpResponseCode = (int)response.StatusCode;
                if (ResponsePayload.TryRead(zulipResponse, "FetchApiKey", "api_key", JsonValueKind.String,
                    out string? apiKey, out string? details,
                    value => string.IsNullOrWhiteSpace(value.GetString()) ? "api_key must be nonempty." : null) &&
                    ResponsePayload.TryRead(zulipResponse, "FetchApiKey", "email", JsonValueKind.String,
                    out string? email, out details,
                    value => string.IsNullOrWhiteSpace(value.GetString()) ? "email must be nonempty." : null))
                {
                    return (true, null, apiKey!, email!);
                }

                return (false, details, null, null);
            }
            catch (Exception ex)
            {
                return (false, "FetchApiKey failed: " + ex.Message, null, null);
            }
            finally
            {
                if (httpClient == null)
                {
                    client.Dispose();
                }
            }
        }

        /// <summary>Executes the zulip request via curl.</summary>
        /// <param name="httpMethod"> The HTTP method.</param>
        /// <param name="relativeUrl">URL of the relative.</param>
        /// <param name="data">       The data.</param>
        /// <returns>An asynchronous result that yields a ZulipResponse.</returns>
        internal async Task<ZulipResponse> DoZulipRequestCurl(
            HttpMethod httpMethod,
            string relativeUrl,
            Dictionary<string, string>? data)
        {
            ZulipResponse zulipResponse;

            try
            {
                ProcessStartInfo startInfo = BuildCurlStartInfo(httpMethod, relativeUrl, data);
                using Process proc = Process.Start(startInfo)
                    ?? throw new InvalidOperationException("Unable to start curl.");
                Task<string> bodyTask = proc.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();
                string body = await bodyTask;
                string errors = await errorTask;

                if (proc.ExitCode != 0 || string.IsNullOrEmpty(body))
                {
                    zulipResponse = new ZulipResponse();
                    zulipResponse.CaughtException = proc.ExitCode != 0
                        ? $"curl exited with code {proc.ExitCode}: {errors}"
                        : string.IsNullOrEmpty(errors) ? "curl returned no data!" : errors;
                    zulipResponse.HttpResponseBody = body;
                    zulipResponse.Success = false;
                    LastResponse = zulipResponse;
                    return zulipResponse;
                }

                try
                {
                    zulipResponse = JsonSerializer.Deserialize<ZulipResponse>(body)
                        ?? throw new JsonException("The Zulip response must be a JSON object.");
                    zulipResponse.Success = zulipResponse.Result == ZulipResponse.ZulipResultSuccess &&
                        string.IsNullOrEmpty(zulipResponse.CaughtException);
                }
                catch (JsonException parseEx)
                {
                    zulipResponse = new ZulipResponse();
                    zulipResponse.CaughtException = parseEx.Message;
                    zulipResponse.Success = false;
                }

                zulipResponse.HttpResponseCode = 0;
                zulipResponse.HttpResponseBody = body;
            }
            catch (System.ComponentModel.Win32Exception processEx)
            {
                zulipResponse = new ZulipResponse
                {
                    CaughtException = "Unable to start curl: " + processEx.Message,
                    Success = false,
                };
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"ZulipClient <<< exception: {httpEx.Message}");
                Console.WriteLine($"ZulipClient <<< inner: {httpEx.InnerException}");
                zulipResponse = new ZulipResponse();
                zulipResponse.CaughtException = httpEx.Message;
                zulipResponse.Success = false;
            }

            LastResponse = zulipResponse;
            return zulipResponse;
        }

        /// <summary>Builds the production curl invocation with unambiguous argument boundaries.</summary>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="relativeUrl">The API-relative URL.</param>
        /// <param name="data">Form or query parameters.</param>
        /// <returns>The process start information.</returns>
        internal ProcessStartInfo BuildCurlStartInfo(
            HttpMethod httpMethod, string relativeUrl, Dictionary<string, string>? data)
        {
            Uri requestUri = new Uri(_siteUri, relativeUrl);
            if (httpMethod == HttpMethod.Get && data != null && data.Count != 0)
            {
                string query = string.Join("&", data.Select(
                    kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                requestUri = new Uri(requestUri, "?" + query);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = _curlCommand!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-sS");
            startInfo.ArgumentList.Add("-X");
            startInfo.ArgumentList.Add(httpMethod.Method);
            startInfo.ArgumentList.Add(requestUri.AbsoluteUri);
            startInfo.ArgumentList.Add("-u");
            startInfo.ArgumentList.Add(_userEmail + ":" + _apiKey);
            if (httpMethod != HttpMethod.Get && data != null)
            {
                foreach (KeyValuePair<string, string> pair in data)
                {
                    startInfo.ArgumentList.Add("--data-urlencode");
                    startInfo.ArgumentList.Add(pair.Key + "=" + pair.Value);
                }
            }

            return startInfo;
        }

        /// <summary>Post this message.</summary>
        /// <param name="httpMethod"> The HTTP method.</param>
        /// <param name="relativeUrl">URL of the relative.</param>
        /// <param name="data">       The data.</param>
        /// <returns>A HttpResponseMessage.</returns>
        internal async Task<ZulipResponse> DoZulipRequestHttpClient(
            HttpMethod httpMethod, 
            string relativeUrl, 
            Dictionary<string, string>? data)
        {
            ZulipResponse zulipResponse;

            try
            {
                Uri requestUri = new Uri(_siteUri, relativeUrl);

                HttpRequestMessage request;

                if (httpMethod == HttpMethod.Get && data != null && data.Count != 0)
                {
                    var query = string.Join("&", data.Select(
                        kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    requestUri = new Uri(requestUri, "?" + query);
                    request = new HttpRequestMessage(httpMethod, requestUri);
                }
                else
                {
                    request = new HttpRequestMessage(httpMethod, requestUri);

                    if ((data != null) && (data.Count != 0))
                    {
                        request.Content = new FormUrlEncodedContent(data);
                    }
                }

                request.Headers.Add("Authorization", _authHeader);

                HttpResponseMessage response = await _httpClient!.SendAsync(request);

                string body = await response.Content.ReadAsStringAsync();

                try
                {
                    zulipResponse = JsonSerializer.Deserialize<ZulipResponse>(body)
                        ?? throw new JsonException("The Zulip response must be a JSON object.");

                    zulipResponse.Success = response.IsSuccessStatusCode &&
                        zulipResponse.Result == ZulipResponse.ZulipResultSuccess &&
                        string.IsNullOrEmpty(zulipResponse.CaughtException);
                }
                catch (JsonException parseEx)
                {
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

            LastResponse = zulipResponse;
            return zulipResponse;
        }
    }
}
