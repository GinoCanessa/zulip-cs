using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        /// <summary>The files resource.</summary>
        private Files _files = null!;

        /// <summary>The bounded upload timeout.</summary>
        private TimeSpan _uploadTimeout = TimeSpan.FromSeconds(100);

        /// <summary>The curl command.</summary>
        private string? _curlCommand;

        /// <summary>Gets diagnostics and warnings for the most recently completed request, or null before any request completes.</summary>
        /// <remarks>Concurrent callers must use detailed results for per-operation association; completion order is not invocation order.</remarks>
        public ZulipResponse? LastResponse { get; private set; }

        /// <summary>Gets or sets the timeout for an entire upload, including reading its response; defaults to 100 seconds.</summary>
        /// <remarks>Must be positive and finite. Curl cleanup has an independent additional five-second bound.</remarks>
        public TimeSpan UploadTimeout
        {
            get => _uploadTimeout;
            set
            {
                if (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue - 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "UploadTimeout must be positive and finite.");
                _uploadTimeout = value;
            }
        }

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

            InitializeResources(DoZulipRequestHttpClient, DoZulipUploadHttpClient);
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

            InitializeResources(DoZulipRequestCurl, DoZulipUploadCurl);
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
            InitializeResources(DoZulipRequestHttpClient, DoZulipUploadHttpClient);
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
            InitializeResources(DoZulipRequestHttpClient, DoZulipUploadHttpClient);
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.ZulipClient class.</summary>
        /// <param name="zuliprcFilename">Filename of the zuliprc file.</param>
        /// <param name="curlCommand">    The curl command.</param>
        public ZulipClient(string zuliprcFilename, string curlCommand)
        : this(zuliprcFilename, false)
        {
            _curlCommand = curlCommand;
            InitializeResources(DoZulipRequestCurl, DoZulipUploadCurl);
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

        /// <summary>Gets the streaming upload and attachment interface.</summary>
        public Files Files => _files;

        /// <summary>Initializes all resource classes.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        /// <param name="doUpload">The focused streaming upload delegate.</param>
        private void InitializeResources(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest,
            Func<Stream, string, string, CancellationToken, Task<ZulipResponse>> doUpload)
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
            _files = new Files(doZulipRequest, doUpload);
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

        /// <summary>Creates identical streaming multipart content for both upload transports.</summary>
        /// <param name="stream">The caller-owned input stream.</param>
        /// <param name="filename">The multipart filename.</param>
        /// <param name="contentType">The validated MIME content type.</param>
        /// <returns>The disposable content, which never owns the input stream.</returns>
        private static MultipartFormDataContent CreateUploadContent(Stream stream, string filename, string contentType)
        {
            MultipartFormDataContent multipart = new MultipartFormDataContent();
            StreamContent file = new StreamContent(new NonDisposingStream(stream));
            try
            {
                file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                ContentDispositionHeaderValue disposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "filename"
                };
                // The FileName setter rejects embedded quotes even when escaped. Set the validated quoted parameter directly.
                string encodedFilename = filename.Any(character => character > 127)
                    ? "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(filename)) + "?="
                    : filename.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
                disposition.Parameters.Add(new NameValueHeaderValue("filename", "\"" + encodedFilename + "\""));
                disposition.FileNameStar = filename;
                file.Headers.ContentDisposition = disposition;
                multipart.Add(file);
                return multipart;
            }
            catch
            {
                file.Dispose();
                multipart.Dispose();
                throw;
            }
        }

        /// <summary>Uploads streaming multipart content through HttpClient with bounded cancellation.</summary>
        /// <param name="stream">The readable caller-owned stream.</param>
        /// <param name="filename">The multipart filename.</param>
        /// <param name="contentType">The validated MIME content type.</param>
        /// <param name="cancellationToken">Caller cancellation.</param>
        /// <returns>The response and actual HTTP diagnostics.</returns>
        private async Task<ZulipResponse> DoZulipUploadHttpClient(
            Stream stream, string filename, string contentType, CancellationToken cancellationToken)
        {
            TimeSpan timeout = UploadTimeout;
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            ZulipResponse result = new ZulipResponse();
            try
            {
                deadline.Token.ThrowIfCancellationRequested();
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(_siteUri, "api/v1/user_uploads"));
                request.Headers.Add("Authorization", _authHeader);
                request.Content = CreateUploadContent(stream, filename, contentType);
                using HttpResponseMessage response = await _httpClient!.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                result.HttpResponseCode = (int)response.StatusCode;
                string body = await response.Content.ReadAsStringAsync(deadline.Token);
                result = ParseUploadResponse(body, (int)response.StatusCode);
            }
            catch (OperationCanceledException exception)
            {
                result.CaughtException = deadline.IsCancellationRequested
                    ? UploadCancellationDetails(cancellationToken, timeout)
                    : "Upload canceled by HTTP transport: " + UploadExceptionDetails(exception);
                result.Success = false;
            }
            catch (Exception exception)
            {
                result.CaughtException = "Upload failed: " + UploadExceptionDetails(exception);
                result.Success = false;
            }
            LastResponse = result;
            return result;
        }

        /// <summary>Uploads the .NET multipart serialization directly to curl's binary stdin.</summary>
        /// <param name="stream">The readable caller-owned stream.</param>
        /// <param name="filename">The multipart filename.</param>
        /// <param name="contentType">The validated MIME content type.</param>
        /// <param name="cancellationToken">Caller cancellation.</param>
        /// <returns>The response with the actual body, HTTP status and process diagnostics.</returns>
        private async Task<ZulipResponse> DoZulipUploadCurl(
            Stream stream, string filename, string contentType, CancellationToken cancellationToken)
        {
            TimeSpan timeout = UploadTimeout;
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            using CancellationTokenSource serializer = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
            ZulipResponse result = new ZulipResponse();
            Process? process = null;
            bool started = false;
            bool receivedResponse = false;
            Task? completion = null;
            Task<(Exception? error, bool interrupted)>? write = null;
            try
            {
                deadline.Token.ThrowIfCancellationRequested();
                using MultipartFormDataContent multipart = CreateUploadContent(stream, filename, contentType);
                string suffix = "\nZULIP_UPLOAD_STATUS_" + Guid.NewGuid().ToString("N") + ":";
                ProcessStartInfo start = BuildCurlStartInfo(HttpMethod.Post, "api/v1/user_uploads", null);
                start.RedirectStandardInput = true;
                start.ArgumentList.Add("--http1.1");
                start.ArgumentList.Add("--upload-file");
                start.ArgumentList.Add(".");
                start.ArgumentList.Add("--header");
                start.ArgumentList.Add("Content-Type: " + multipart.Headers.ContentType);
                start.ArgumentList.Add("--header");
                start.ArgumentList.Add("Expect:");
                long? length = multipart.Headers.ContentLength;
                if (length.HasValue)
                {
                    start.ArgumentList.Add("--header");
                    start.ArgumentList.Add("Content-Length: " + length.Value.ToString(CultureInfo.InvariantCulture));
                    start.ArgumentList.Add("--header");
                    start.ArgumentList.Add("Transfer-Encoding:");
                }
                start.ArgumentList.Add("--max-time");
                start.ArgumentList.Add(timeout.TotalSeconds.ToString("0.#########", CultureInfo.InvariantCulture));
                start.ArgumentList.Add("--write-out");
                start.ArgumentList.Add(suffix + "%{http_code}");
                process = new Process { StartInfo = start };
                if (!process.Start()) throw new InvalidOperationException("Unable to start curl.");
                started = true;

                Task<string> stdout = process.StandardOutput.ReadToEndAsync();
                Task<string> stderr = process.StandardError.ReadToEndAsync();
                Task exited = process.WaitForExitAsync();
                Task received = Task.WhenAll(stdout, stderr, exited);
                write = WriteCurlUploadAsync(multipart, process, serializer.Token);
                completion = Task.WhenAll(received, write);
                await exited.WaitAsync(deadline.Token);
                Task stopSerializer = serializer.CancelAsync();
                completion = Task.WhenAll(received, write, stopSerializer);
                await received.WaitAsync(deadline.Token);
                deadline.Token.ThrowIfCancellationRequested();

                string output = await stdout;
                string errors = await stderr;
                int marker = output.LastIndexOf(suffix, StringComparison.Ordinal);
                bool hasStatus = marker >= 0 && output.Length == marker + suffix.Length + 3 &&
                    int.TryParse(output.AsSpan(marker + suffix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out _);
                int status = hasStatus
                    ? int.Parse(output.AsSpan(marker + suffix.Length), NumberStyles.None, CultureInfo.InvariantCulture)
                    : 0;
                string body = marker >= 0 ? output.Substring(0, marker) : output;
                result = ParseUploadResponse(body, status);
                receivedResponse = true;
                if (!hasStatus || status == 0 || process.ExitCode != 0)
                {
                    string failure = process.ExitCode == 28
                        ? UploadCancellationDetails(cancellationToken, timeout)
                        : process.ExitCode != 0
                            ? $"curl exited with code {process.ExitCode}: {errors.Trim()}"
                            : "curl returned no HTTP upload status.";
                    result.CaughtException = string.IsNullOrEmpty(result.CaughtException)
                        ? failure : result.CaughtException + " " + failure;
                    result.Success = false;
                }
            }
            catch (OperationCanceledException)
            {
                result.CaughtException = UploadCancellationDetails(cancellationToken, timeout);
                result.Success = false;
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                result.CaughtException = "Unable to start curl: " + exception.Message;
                result.Success = false;
            }
            catch (Exception exception)
            {
                result.CaughtException = "Upload failed: " + UploadExceptionDetails(exception);
                result.Success = false;
            }
            finally
            {
                if (process != null)
                {
                    Task stopSerializer = serializer.CancelAsync();
                    completion = completion == null ? stopSerializer : Task.WhenAll(completion, stopSerializer);
                    string? cleanupError = started ? await CleanupCurlUploadAsync(process, completion) : null;
                    if (cleanupError != null)
                    {
                        result.CaughtException = string.IsNullOrEmpty(result.CaughtException)
                            ? cleanupError : result.CaughtException + " " + cleanupError;
                        result.Success = false;
                    }
                    if (write != null)
                    {
                        string? writeFailure = null;
                        if (!write.IsCompletedSuccessfully && receivedResponse)
                        {
                            writeFailure = "Upload incomplete: curl exited before multipart serialization completed.";
                        }
                        else if (write.IsCompletedSuccessfully)
                        {
                            (Exception? error, bool interrupted) outcome = write.Result;
                            if (outcome.error != null && !outcome.interrupted)
                                writeFailure = "Upload stream failed: " + UploadExceptionDetails(outcome.error);
                            else if (outcome.interrupted && receivedResponse && result.HttpResponseCode is >= 200 and < 300)
                                writeFailure = "Upload incomplete: curl exited before multipart serialization completed.";
                        }
                        if (writeFailure != null)
                        {
                            result.CaughtException = string.IsNullOrEmpty(result.CaughtException)
                                ? writeFailure : result.CaughtException + " " + writeFailure;
                            result.Success = false;
                        }
                    }
                    process.Dispose();
                }
            }
            LastResponse = result;
            return result;
        }

        /// <summary>Serializes multipart incrementally and always sends binary stdin EOF, even on early rejection.</summary>
        /// <param name="multipart">The streaming multipart content.</param>
        /// <param name="process">The exact child process receiving binary stdin.</param>
        /// <param name="cancellationToken">The serializer deadline, also canceled when curl exits.</param>
        /// <returns>A write failure and whether it was interrupted by the serializer token.</returns>
        private static async Task<(Exception? error, bool interrupted)> WriteCurlUploadAsync(
            MultipartFormDataContent multipart, Process process, CancellationToken cancellationToken)
        {
            Exception? failure = null;
            bool interrupted = false;
            try
            {
                // -F @- buffers stdin; -T - blocks response processing. -T . streams while observing early server responses.
                await multipart.CopyToAsync(process.StandardInput.BaseStream, cancellationToken);
            }
            catch (Exception exception)
            {
                failure = exception;
                interrupted = exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
            }
            finally
            {
                try { process.StandardInput.Close(); }
                catch (Exception exception) { failure ??= exception; }
            }
            return (failure, interrupted);
        }

        /// <summary>Kills only the upload's child process tree and bounds pipe/exit cleanup independently of caller cancellation.</summary>
        /// <param name="process">The specific upload process.</param>
        /// <param name="completion">The concurrent serializer, pipe drains and exit wait.</param>
        /// <returns>A cleanup failure, or null.</returns>
        private static async Task<string?> CleanupCurlUploadAsync(Process process, Task? completion)
        {
            try
            {
                if (!process.HasExited)
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch (InvalidOperationException) when (process.HasExited) { }
                }
                Task pending = completion ?? process.WaitForExitAsync();
                await pending.WaitAsync(TimeSpan.FromSeconds(5));
                return null;
            }
            catch (TimeoutException)
            {
                if (completion != null)
                    _ = completion.ContinueWith(task => _ = task.Exception, TaskContinuationOptions.OnlyOnFaulted);
                return "Upload process cleanup exceeded five seconds.";
            }
            catch (Exception exception)
            {
                return "Upload process cleanup failed: " + exception.Message;
            }
            finally
            {
                try { process.StandardInput.Close(); }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
        }

        /// <summary>Parses an upload envelope without losing its actual HTTP status or malformed body.</summary>
        /// <param name="body">The actual response body without curl's write-out suffix.</param>
        /// <param name="status">The actual HTTP response status.</param>
        /// <returns>The completed response.</returns>
        private static ZulipResponse ParseUploadResponse(string body, int status)
        {
            ZulipResponse result;
            try
            {
                result = JsonSerializer.Deserialize<ZulipResponse>(body)
                    ?? throw new JsonException("The Zulip response must be a JSON object.");
                result.Success = status is >= 200 and < 300 &&
                    result.Result == ZulipResponse.ZulipResultSuccess && string.IsNullOrEmpty(result.CaughtException);
            }
            catch (JsonException exception)
            {
                result = new ZulipResponse { CaughtException = exception.Message, Success = false };
            }
            result.HttpResponseCode = status;
            result.HttpResponseBody = body;
            return result;
        }

        /// <summary>Distinguishes caller cancellation from the configured upload deadline without exposing invocation arguments.</summary>
        /// <param name="cancellationToken">The caller's token.</param>
        /// <param name="timeout">The configured timeout.</param>
        /// <returns>The failure details.</returns>
        private static string UploadCancellationDetails(CancellationToken cancellationToken, TimeSpan timeout) =>
            cancellationToken.IsCancellationRequested
                ? "Upload canceled."
                : "Upload timed out after " + timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) + " seconds.";

        /// <summary>Preserves underlying stream/transport failures without logging commands, arguments or credentials.</summary>
        /// <param name="exception">The failed stream or transport operation.</param>
        /// <returns>The exception messages, including wrapped I/O failures.</returns>
        private static string UploadExceptionDetails(Exception exception)
        {
            string details = exception.Message;
            Exception? inner = exception.InnerException;
            for (int depth = 0; inner != null && depth < 4; depth++)
            {
                if (!string.Equals(exception.Message, inner.Message, StringComparison.Ordinal))
                    details += " " + inner.Message;
                inner = inner.InnerException;
            }
            return details;
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
