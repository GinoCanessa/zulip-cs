using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using zulip_cs_lib;


namespace zulip_set_lib.tests
{
    /// <summary>Isolates tests that temporarily capture the process-wide console.</summary>
    [CollectionDefinition("Console isolation", DisableParallelization = true)]
    public sealed class ConsoleIsolationCollection
    {
    }

    /// <summary>Basic Zulip client tests.</summary>
    [Collection("Console isolation")]
    public class ZulipClientTests
    {
        /// <summary>Both public owned construction routes dispose the actual underlying transport.</summary>
        /// <param name="fromConfig">Whether to construct using a configuration file.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZulipClient_PublicOwnedConstructorsDisposeTransport(bool fromConfig)
        {
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, $"[api]\nsite={_site}\nemail={_email}\nkey={_key}\n");
                using ZulipClient client = fromConfig
                    ? new ZulipClient(file)
                    : new ZulipClient(_site, _email, _key);
                FieldInfo field = typeof(ZulipClient).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic)!;
                HttpClient transport = Assert.IsType<HttpClient>(field.GetValue(client));
                client.Dispose();
                await Assert.ThrowsAsync<ObjectDisposedException>(
                    () => transport.GetAsync("relative-without-base", TestContext.Current.CancellationToken));
            }
            finally
            {
                File.Delete(file);
            }
        }

        /// <summary>Owned transports are disposed once while injected transports remain reusable.</summary>
        /// <param name="owned">Whether the wrapper owns the HTTP client.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZulipClient_DisposalPreservesTransportOwnership(bool owned)
        {
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK));
            using HttpClient http = new HttpClient(handler.Object);
            ZulipClient client = new ZulipClient(_site, _email, _key, http, owned);
            client.Dispose();
            client.Dispose();
            handler.Protected().Verify("Dispose", owned ? Times.Once() : Times.Never(), ItExpr.Is<bool>(value => value));
            if (owned)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(() => http.GetAsync(_site, TestContext.Current.CancellationToken));
            }
            else
            {
                using HttpResponseMessage response = await http.GetAsync(_site, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => client.Messages.TrySendStream("hello", "topic", "channel"));
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => client.Messages.SendStream("hello", "topic", "channel"));
            using MemoryStream input = new MemoryStream([1, 2, 3]);
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => client.Files.TryUpload(input, "file.bin", cancellationToken: TestContext.Current.CancellationToken));
            Assert.True(input.CanRead);
        }

        /// <summary>A disposed curl wrapper rejects requests and uploads before starting a process.</summary>
        [Fact]
        public async Task ZulipClient_DisposedCurlRejectsTransportOperations()
        {
            using ZulipClient client = new ZulipClient(_site, _email, _key, "missing-curl");
            client.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => client.Events.TryGetEvents("queue", -1));
            using MemoryStream input = new MemoryStream([1, 2, 3]);
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => client.Files.TryUpload(input, "file.bin", cancellationToken: TestContext.Current.CancellationToken));
            Assert.True(input.CanRead);
        }

        /// <summary>Config wrappers do not take ownership of an injected HTTP client.</summary>
        [Fact]
        public async Task ZulipClient_ConfigDisposalPreservesInjectedClient()
        {
            string file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, $"[api]\nsite={_site}\nemail={_email}\nkey={_key}\n");
                Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
                handler.Protected().Setup<Task<HttpResponseMessage>>(
                    "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK));
                using HttpClient http = new HttpClient(handler.Object);
                using ZulipClient client = new ZulipClient(file, http);
                client.Dispose();
                using HttpResponseMessage response = await http.GetAsync(_site, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            finally
            {
                File.Delete(file);
            }
        }

        /// <summary>Bootstrap content is disposed on success and failure while injected clients stay reusable.</summary>
        /// <param name="body">The bootstrap response.</param>
        /// <param name="success">Whether the credentials are valid.</param>
        [Theory]
        [InlineData("{\"result\":\"success\",\"api_key\":\"key\",\"email\":\"a@example.org\"}", true)]
        [InlineData("{\"result\":\"success\"}", false)]
        [InlineData("not JSON", false)]
        public async Task ZulipClient_FetchApiKeyDisposesContentAndPreservesInjectedClient(string body, bool success)
        {
            List<HttpRequestMessage> requests = [];
            List<MemoryStream> streams = [];
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    requests.Add(request);
                    MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
                    streams.Add(stream);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) });
                });
            using HttpClient http = new HttpClient(handler.Object);
            for (int index = 0; index < 2; index++)
            {
                (bool actualSuccess, string? details, string? apiKey, string? email) actual =
                    await ZulipClient.TryFetchApiKey(_site, _email, "password", http);
                Assert.Equal(success, actual.actualSuccess);
                Assert.False(streams[index].CanRead);
                await Assert.ThrowsAsync<ObjectDisposedException>(
                    () => requests[index].Content!.ReadAsStringAsync(TestContext.Current.CancellationToken));
            }
        }

        /// <summary>Network failures retain wrapped details without writing to the host's console.</summary>
        [Fact]
        public async Task ZulipClient_HttpNetworkFailureReturnsDiagnosticsWithoutConsoleOutput()
        {
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected().SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = Utils.ContentForJsonString("{\"result\":\"success\",\"id\":31}")
                })
                .ThrowsAsync(new HttpRequestException("connection failed", new IOException("fixture source error")));
            using HttpClient http = new HttpClient(handler.Object);
            using ZulipClient client = new ZulipClient(_site, _email, _key, http);
            Assert.True((await client.Messages.TrySendStream("hello", "topic", "channel")).success);
            ZulipResponse previous = client.LastResponse!;
            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;
            using StringWriter output = new StringWriter();
            using StringWriter error = new StringWriter();
            try
            {
                Console.SetOut(output);
                Console.SetError(error);
                (bool success, string? details, ulong messageId) actual =
                    await client.Messages.TrySendStream("hello", "topic", "channel");
                Assert.False(actual.success);
                Assert.Contains("connection failed", actual.details);
                Assert.Contains("fixture source error", actual.details);
                Assert.NotSame(previous, client.LastResponse);
                Assert.False(client.LastResponse!.Success);
                Assert.Equal(string.Empty, output.ToString());
                Assert.Equal(string.Empty, error.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }

        /// <summary>Gets whether the external executable required by real-process tests is available.</summary>
        public static bool CurlAvailable
        {
            get
            {
                IEnumerable<string> directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Prepend(Environment.CurrentDirectory);
                if (OperatingSystem.IsWindows()) directories = directories.Prepend(Environment.SystemDirectory);
                string name = OperatingSystem.IsWindows() ? "curl.exe" : "curl";
                return directories.Any(directory => File.Exists(Path.Combine(directory.Trim('"'), name)));
            }
        }

        /// <summary>Startup errors remain explicit and available through diagnostics without requiring installed curl.</summary>
        [Fact]
        public async Task ZulipClient_MissingCurlRetainsStartupFailure()
        {
            string executable = Path.Combine(Path.GetTempPath(), "zulip-missing-curl-" + Guid.NewGuid().ToString("N") + ".exe");
            ZulipClient client = new ZulipClient(_site, _email, _key, executable);
            ZulipResponse response = await client.DoZulipRequestCurl(HttpMethod.Get, "api/v1/messages", null);
            Assert.False(response.Success);
            Assert.Contains("Unable to start curl:", response.CaughtException);
            Assert.Same(response, client.LastResponse);
        }

        /// <summary>Non-object responses produce explicit Try failures instead of null dereferences.</summary>
        /// <param name="body">The invalid response root.</param>
        [Theory]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("true")]
        public async Task ZulipClient_NonObjectBodyFailsExplicitly(string body)
        {
            ZulipClient client = Utils.CreateCapturingClient([], body);
            (bool success, string? details, ulong messageId) actual = await client.Messages.TrySendStream("Hello", "Topic", "Channel");
            Assert.False(actual.success);
            Assert.Contains("JSON object", actual.details);
            Assert.Equal(body, client.LastResponse!.HttpResponseBody);
            Assert.False(client.LastResponse.Success);
        }

        /// <summary>API-key bootstrap validates both credentials rather than returning successful nulls.</summary>
        /// <param name="fields">The invalid credential fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"api_key\":\"key\"")]
        [InlineData(",\"email\":\"a@example.org\"")]
        [InlineData(",\"api_key\":null,\"email\":\"a@example.org\"")]
        public async Task ZulipClient_FetchApiKeyRequiresCredentials(string fields)
        {
            Assert.True(Utils.TryGetMockedClient(HttpStatusCode.OK, Utils.ContentForJsonString($"{{\"result\":\"success\"{fields}}}"),
                out _, out HttpClient httpClient, out _));
            using (httpClient)
            {
                (bool success, string? details, string? apiKey, string? email) actual =
                    await ZulipClient.TryFetchApiKey("https://example.org", "a@example.org", "password", httpClient);
                Assert.False(actual.success);
                Assert.Contains("FetchApiKey failed:", actual.details);
                Assert.Null(actual.apiKey);
                Assert.Null(actual.email);
            }
        }

        private const string _email = "zulip@example.org";
        private const string _key = "KeyInBase64";
        private const string _site = "https://zulip.example.org";

        /// <summary>Ordinary cancellation returns current Try diagnostics and preserves throwing behavior.</summary>
        [Fact]
        public async Task ZulipClient_TimeoutReturnsFailureAndUpdatesLastResponse()
        {
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected().SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = Utils.ContentForJsonString("{\"result\":\"success\",\"id\":31}")
                })
                .ThrowsAsync(new TaskCanceledException("fixture deadline expired"))
                .ThrowsAsync(new TaskCanceledException("fixture deadline expired"));
            using HttpClient http = new HttpClient(handler.Object);
            ZulipClient client = new ZulipClient(_site, _email, _key, http);
            Assert.True((await client.Messages.TrySendStream("hello", "topic", "channel")).success);
            ZulipResponse previous = client.LastResponse!;
            (bool success, string? details, ulong messageId) failed =
                await client.Messages.TrySendStream("hello", "topic", "channel");
            Assert.False(failed.success);
            Assert.Contains("fixture deadline expired", failed.details);
            Assert.NotSame(previous, client.LastResponse);
            Assert.False(client.LastResponse!.Success);
            Assert.Contains("fixture deadline expired", client.LastResponse.CaughtException);
            Exception exception = await Assert.ThrowsAsync<Exception>(
                () => client.Messages.SendStream("hello", "topic", "channel"));
            Assert.Contains("fixture deadline expired", exception.Message);
        }

        /// <summary>Request-local content is disposed after success or parsing failure without closing the client.</summary>
        /// <param name="body">The actual response body.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}")]
        [InlineData("not JSON")]
        public async Task ZulipClient_DisposesOrdinaryContentAndPreservesInjectedClient(string body)
        {
            List<HttpRequestMessage> requests = [];
            List<MemoryStream> streams = [];
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    requests.Add(request);
                    MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
                    streams.Add(stream);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) });
                });
            using HttpClient http = new HttpClient(handler.Object);
            ZulipClient client = new ZulipClient(_site, _email, _key, http);
            for (int index = 0; index < 2; index++)
            {
                ZulipResponse response = await client.DoZulipRequestHttpClient(
                    HttpMethod.Post, "api/v1/messages", new Dictionary<string, string> { ["content"] = "hello" });
                Assert.Equal(body, response.HttpResponseBody);
                Assert.False(streams[index].CanRead);
                await Assert.ThrowsAsync<ObjectDisposedException>(
                    () => requests[index].Content!.ReadAsStringAsync(TestContext.Current.CancellationToken));
            }
            Assert.Equal(2, requests.Count);
        }


        [Fact]
        public void ZulipClient_CreateMockHttp()
        {
            Dictionary<string, dynamic> mockResponse = new Dictionary<string, dynamic>()
            {
                { "result", "success" },
                { "msg", "mocked message" },
            };

            bool success = Utils.GetMockHttpClient(
                HttpStatusCode.OK,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> httpHandler,
                out HttpClient httpClient);

            Assert.True(success, "Failed to create mock HttpClient");
        }

        [Fact]
        public void ZulipClient_Create_Success()
        {
            ZulipClient client = new ZulipClient(_site, _email, _key);
        }

        [Fact]
        public void ZulipClient_GetMockedClient()
        {
            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK,
                new StringContent("OK"),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get a valid mocked client!");
        }

        [Fact]
        public void ZulipClient_CreateSiteEmpty_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient(string.Empty, _email, _key));
            Assert.Equal("site", ex.Message);
        }

        [Fact]
        public void ZulipClient_CreateSiteInvalid_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient("uri://invalid.me", _email, _key));
            Assert.Equal("site", ex.Message);
        }

        [Fact]
        public void ZulipClient_CreateEmailEmpty_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient(_site, string.Empty, _key));
            Assert.Equal("userEmail", ex.Message);
        }

        [Fact]
        public void ZulipClient_CreateEmailInvalid_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient(_site, "NotAnEmail", _key));
            Assert.Equal("userEmail", ex.Message);
        }

        [Fact]
        public void ZulipClient_CreateKeyEmpty_Throws()
        {
            ZulipClient client;

            Exception ex = Assert.Throws<ArgumentException>(() => client = new ZulipClient(_site, _email, string.Empty));
            Assert.Equal("apiKey", ex.Message);
        }

        /// <summary>Exposes nonfatal compatibility warnings after convenience calls.</summary>
        [Fact]
        public async Task ZulipClient_LastResponseRetainsNonfatalWarningsFromConvenienceCalls()
        {
            Assert.True(Utils.TryGetMockedClient(HttpStatusCode.OK,
                Utils.ContentForJsonString("{\"result\":\"success\",\"msg\":\"\",\"ignored_parameters_unsupported\":[\"future_option\"],\"future\":{\"value\":42}}"),
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient));
            Assert.Null(zulipClient.LastResponse);

            await zulipClient.Users.Update(10, fullName: "Updated");
            ZulipResponse response = zulipClient.LastResponse!;
            Assert.True(response.Success);
            Assert.Equal(new string[] { "future_option" }, response.IgnoredParametersUnsupported);
            Assert.Equal(42, response.ExtensionData!["future"].GetProperty("value").GetInt32());
            Assert.Equal(200, response.HttpResponseCode);
            Assert.Contains("future_option", response.HttpResponseBody);
        }

        /// <summary>API-level failures retain the server error and do not set transport success.</summary>
        [Fact]
        public async Task ZulipClient_ApiErrorsDoNotSetSuccessAndPreserveServerDetails()
        {
            Assert.True(Utils.TryGetMockedClient(HttpStatusCode.OK,
                Utils.ContentForJsonString("{\"result\":\"error\",\"code\":\"BAD_REQUEST\",\"msg\":\"Actual server message\"}"),
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient));
            (bool success, string? details, zulip_cs_lib.Models.UserObject? user) = await zulipClient.Users.TryGetUser(10);
            Assert.False(success);
            Assert.Null(user);
            Assert.False(zulipClient.LastResponse!.Success);
            Assert.Equal("BAD_REQUEST", zulipClient.LastResponse.ErrorCode);
            Assert.Equal("Actual server message", zulipClient.LastResponse.Message);
            Assert.Contains("BAD_REQUEST", details);
            Assert.Contains("Actual server message", details);
        }

        /// <summary>Retains diagnostics when response JSON cannot be parsed.</summary>
        [Fact]
        public async Task ZulipClient_DeserializationFailureStillCompletesDiagnostics()
        {
            Assert.True(Utils.TryGetMockedClient(HttpStatusCode.OK, Utils.ContentForJsonString("not JSON"),
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient));
            Assert.False((await zulipClient.Users.TryGetAll()).success);
            Assert.False(zulipClient.LastResponse!.Success);
            Assert.NotNull(zulipClient.LastResponse.CaughtException);
            Assert.Equal("not JSON", zulipClient.LastResponse.HttpResponseBody);
        }

        /// <summary>Associates LastResponse with completion order without altering request-local responses.</summary>
        [Fact]
        public async Task ZulipClient_LastResponseTracksCompletionOrder()
        {
            TaskCompletionSource<HttpResponseMessage> first = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<HttpResponseMessage> second = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.AbsolutePath.EndsWith("/1")),
                ItExpr.IsAny<CancellationToken>()).Returns(first.Task);
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.AbsolutePath.EndsWith("/2")),
                ItExpr.IsAny<CancellationToken>()).Returns(second.Task);
            using HttpClient httpClient = new HttpClient(handler.Object);
            ZulipClient client = new ZulipClient(_site, _email, _key, httpClient);
            Task<ZulipResponse> firstRequest = client.DoZulipRequestHttpClient(HttpMethod.Get, "api/v1/users/1", null);
            Task<ZulipResponse> secondRequest = client.DoZulipRequestHttpClient(HttpMethod.Get, "api/v1/users/2", null);
            Assert.Null(client.LastResponse);
            second.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = Utils.ContentForJsonString("{\"result\":\"success\",\"ignored_parameters_unsupported\":[\"second\"]}")
            });
            ZulipResponse secondResponse = await secondRequest;
            Assert.Same(secondResponse, client.LastResponse);
            first.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = Utils.ContentForJsonString("{\"result\":\"success\",\"ignored_parameters_unsupported\":[\"first\"]}")
            });
            ZulipResponse firstResponse = await firstRequest;
            Assert.Same(firstResponse, client.LastResponse);
            Assert.Equal(new string[] { "second" }, secondResponse.IgnoredParametersUnsupported);
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("GET")]
        public async Task ZulipClient_ProductionCurlArgumentsMatchDecodedHttpValues(string method)
        {
            const string key = "key \"quoted\" + \u00e9";
            Utils.GetMockHttpClientCapture(HttpStatusCode.OK, Utils.ContentForJsonString("{\"result\":\"success\"}"),
                out Func<HttpRequestMessage?> capture, out Mock<HttpMessageHandler> handler, out HttpClient httpClient);
            ZulipClient http = new ZulipClient(_site, _email, key, httpClient);
            ZulipClient curl = new ZulipClient(_site, _email, key, "curl");
            Dictionary<string, string> data = TransportValues();
            HttpMethod httpMethod = new HttpMethod(method);
            Assert.True((await http.DoZulipRequestHttpClient(httpMethod, "api/v1/messages", data)).Success);
            HttpRequestMessage request = Assert.IsType<HttpRequestMessage>(capture());
            ProcessStartInfo start = curl.BuildCurlStartInfo(httpMethod, "api/v1/messages", data);
            string credential = _email + ":" + key;
            List<string> expected = ["-sS", "-X", method, request.RequestUri!.AbsoluteUri, "-u", credential];
            if (method != "GET")
            {
                expected.AddRange(["--data-urlencode", "subscriptions=" + data["subscriptions"],
                    "--data-urlencode", "content=" + data["content"], "--data-urlencode", "to=" + data["to"]]);
                Assert.Equal(data, await Utils.ReadForm(request));
            }
            else
            {
                Assert.Null(request.Content);
                Assert.Equal(data, Utils.DecodeForm(new Uri(start.ArgumentList[3]).Query));
                Assert.Empty(new Uri(start.ArgumentList[3]).Fragment);
            }
            Assert.Equal(expected, start.ArgumentList);
            Assert.Equal(string.Empty, start.Arguments);
            Assert.False(start.UseShellExecute);
            Assert.True(start.RedirectStandardOutput);
            Assert.True(start.RedirectStandardError);
            Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credential)),
                request.Headers.Authorization!.ToString());
        }

        [Theory(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        [InlineData("POST")]
        [InlineData("GET")]
        public async Task ZulipClient_RealCurlAndHttpClientPreserveLoopbackWireValues(string method)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            List<Task<LoopbackRequest>> captures = [];
            try
            {
                string site = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture);
                const string key = "key \"with spaces\" + \u00e9";
                Dictionary<string, string> data = TransportValues();
                HttpMethod httpMethod = new HttpMethod(method);
                using HttpClient httpClient = new HttpClient(new HttpClientHandler { UseProxy = false });
                ZulipClient http = new ZulipClient(site, _email, key, httpClient);
                Task<LoopbackRequest> httpCapture = ReceiveLoopbackRequest(listener, timeout.Token);
                captures.Add(httpCapture);
                ZulipResponse httpResponse = await http.DoZulipRequestHttpClient(httpMethod, "api/v1/messages", data)
                    .WaitAsync(timeout.Token);
                Assert.True(httpResponse.Success, httpResponse.CaughtException);
                LoopbackRequest httpRequest = await httpCapture;

                ZulipClient curl = new ZulipClient(site, _email, key, "curl");
                Task<LoopbackRequest> curlCapture = ReceiveLoopbackRequest(listener, timeout.Token);
                captures.Add(curlCapture);
                ZulipResponse curlResponse = await curl.DoZulipRequestCurl(
                    httpMethod, "api/v1/messages", data, ConfigureIsolatedCurl, timeout.Token);
                Assert.True(curlResponse.Success, curlResponse.CaughtException);
                Assert.Equal("{\"result\":\"success\",\"msg\":\"\"}", curlResponse.HttpResponseBody);
                LoopbackRequest curlRequest = await curlCapture;
                Assert.Equal(httpRequest.Method, curlRequest.Method);
                Assert.Equal(method, curlRequest.Method);
                Assert.Equal(httpRequest.Target, curlRequest.Target);
                string credential = _email + ":" + key;
                Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credential)),
                    curlRequest.Headers["Authorization"]);
                Assert.Equal(httpRequest.Headers["Authorization"], curlRequest.Headers["Authorization"]);
                Dictionary<string, string> httpValues = method == "GET"
                    ? Utils.DecodeForm(new Uri(site + httpRequest.Target).Query) : Utils.DecodeForm(httpRequest.Body);
                Dictionary<string, string> curlValues = method == "GET"
                    ? Utils.DecodeForm(new Uri(site + curlRequest.Target).Query) : Utils.DecodeForm(curlRequest.Body);
                Assert.Equal(data, httpValues);
                Assert.Equal(data, curlValues);
            }
            finally
            {
                timeout.Cancel();
                listener.Stop();
                foreach (Task<LoopbackRequest> capture in captures)
                {
                    try
                    {
                        await capture;
                    }
                    catch (OperationCanceledException) { }
                    catch (SocketException) { }
                    catch (IOException) { }
                }
            }
        }

        [Fact(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        public async Task ZulipClient_CurlExitFailureRetainsDiagnostics()
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            ZulipClient curl = new ZulipClient("http://127.0.0.1", _email, _key, "curl");
            ZulipResponse response = await curl.DoZulipRequestCurl(
                HttpMethod.Get, "zulip-test-unsupported://127.0.0.1/", null, ConfigureIsolatedCurl, timeout.Token);
            Assert.False(response.Success);
            Assert.Contains("curl exited with code", response.CaughtException);
            Assert.Contains("protocol", response.CaughtException!, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(string.Empty, response.HttpResponseBody);
            Assert.Same(response, curl.LastResponse);
        }

        /// <summary>The actual ordinary curl executor retains HTTP precedence and malformed bodies.</summary>
        /// <param name="status">The fixture HTTP status.</param>
        /// <param name="body">The fixture response body.</param>
        /// <param name="success">Whether the response is successful.</param>
        [Theory(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        [InlineData(200, "{\"result\":\"success\",\"id\":31}", true)]
        [InlineData(502, "{\"result\":\"success\",\"id\":31}", false)]
        [InlineData(400, "not JSON", false)]
        public async Task ZulipClient_OrdinaryCurlPreservesHttpStatus(int status, string body, bool success)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            Task<LoopbackRequest> capture = ReceiveLoopbackRequest(listener, timeout.Token, body, status);
            try
            {
                string site = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture);
                ZulipClient client = new ZulipClient(site, _email, _key, "curl");
                ZulipResponse response = await client.DoZulipRequestCurl(
                    HttpMethod.Get, "api/v1/messages", null, ConfigureIsolatedCurl, timeout.Token);
                Assert.Equal(success, response.Success);
                Assert.Equal(status, response.HttpResponseCode);
                Assert.Equal(body, response.HttpResponseBody);
                Assert.Same(response, client.LastResponse);
                if (status == 502) Assert.Contains("502", response.GetFailureMessage());
                Assert.Equal("/api/v1/messages", (await capture).Target);
            }
            finally
            {
                timeout.Cancel();
                listener.Stop();
                try { await capture; }
                catch (OperationCanceledException) { }
                catch (SocketException) { }
                catch (IOException) { }
            }
        }

        /// <summary>A local stalled response cannot outlive the ordinary curl execution deadline.</summary>
        [Fact(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        public async Task ZulipClient_OrdinaryCurlDeadlineClosesOwnedConnectionBeforeReturning()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            Task<ZulipResponse>? operation = null;
            try
            {
                string site = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture);
                ZulipClient client = new ZulipClient(site, _email, _key, "curl");
                operation = client.DoZulipRequestCurl(
                    HttpMethod.Get, "api/v1/messages", null, ConfigureIsolatedCurl, deadline.Token);
                using TcpClient connection = await listener.AcceptTcpClientAsync(timeout.Token);
                using NetworkStream stream = connection.GetStream();
                byte[] buffer = new byte[16384];
                Assert.True(await stream.ReadAsync(buffer, timeout.Token) > 0);
                Stopwatch elapsed = Stopwatch.StartNew();
                deadline.CancelAfter(TimeSpan.FromMilliseconds(100));
                ZulipResponse response = await operation.WaitAsync(TimeSpan.FromSeconds(8), timeout.Token);
                Assert.False(response.Success);
                Assert.Equal("curl request canceled.", response.CaughtException);
                Assert.Same(response, client.LastResponse);
                Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(6), "curl exceeded its independent cleanup bound.");
                try
                {
                    while (await stream.ReadAsync(buffer, timeout.Token) != 0) { }
                }
                catch (IOException exception) when (exception.InnerException is SocketException
                    { SocketErrorCode: SocketError.ConnectionReset }) { }
            }
            finally
            {
                deadline.Cancel();
                listener.Stop();
                if (operation != null)
                {
                    using CancellationTokenSource cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    await operation.WaitAsync(cleanup.Token);
                }
            }
        }

        /// <summary>Isolates curl fixtures from user configuration and proxies and bounds child execution.</summary>
        /// <param name="start">The production invocation before the child is started.</param>
        private static void ConfigureIsolatedCurl(ProcessStartInfo start)
        {
            start.ArgumentList.Insert(0, "--disable");
            start.ArgumentList.Add("--noproxy");
            start.ArgumentList.Add("*");
            start.ArgumentList.Add("--max-time");
            start.ArgumentList.Add("10");
        }

        /// <summary>Provides structured and Unicode values sensitive to Windows argument quoting.</summary>
        /// <returns>The logical API form fields.</returns>
        private static Dictionary<string, string> TransportValues()
        {
            return new Dictionary<string, string>
            {
                { "subscriptions", "[{\"name\":\"channel \\\"quoted\\\" \u96ea\",\"description\":\"space + % & = # ?\"}]" },
                { "content", "  a \"quoted\" message \u00e9 \u96ea + % & = # ? \\tail\\\nnext line  " },
                { "to", "\"123\"" }
            };
        }

        /// <summary>Receives one bounded local HTTP request and replies without contacting a Zulip server.</summary>
        /// <param name="listener">The loopback listener on an ephemeral port.</param>
        /// <param name="cancellationToken">The deadline for all socket operations.</param>
        /// <param name="json">The response body.</param>
        /// <param name="status">The response status.</param>
        /// <returns>The request line, headers and body seen on the wire.</returns>
        private static async Task<LoopbackRequest> ReceiveLoopbackRequest(TcpListener listener, CancellationToken cancellationToken,
            string json = "{\"result\":\"success\",\"msg\":\"\"}", int status = 200)
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync(cancellationToken);
            using NetworkStream stream = connection.GetStream();
            byte[] header = new byte[16384];
            int length = 0;
            while (length < header.Length)
            {
                int count = await stream.ReadAsync(header.AsMemory(length, 1), cancellationToken);
                if (count == 0) throw new IOException("Loopback client closed before sending headers.");
                length += count;
                if (length >= 4 && header[length - 4] == '\r' && header[length - 3] == '\n' &&
                    header[length - 2] == '\r' && header[length - 1] == '\n') break;
            }
            if (length == header.Length) throw new IOException("Loopback request headers exceeded the fixture limit.");
            string[] lines = Encoding.ASCII.GetString(header, 0, length).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            string[] requestLine = lines[0].Split(' ', 3);
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 1; index < lines.Length; index++)
            {
                string[] parts = lines[index].Split(':', 2);
                headers.Add(parts[0], parts[1].Trim());
            }
            int bodyLength = headers.TryGetValue("Content-Length", out string? contentLength)
                ? int.Parse(contentLength, CultureInfo.InvariantCulture) : 0;
            if (bodyLength < 0 || bodyLength > 16384) throw new IOException("Loopback request body exceeded the fixture limit.");
            byte[] body = new byte[bodyLength];
            await stream.ReadExactlyAsync(body, cancellationToken);
            byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 " + status.ToString(CultureInfo.InvariantCulture) +
                " Fixture\r\nContent-Type: application/json\r\nContent-Length: " +
                Encoding.UTF8.GetByteCount(json).ToString(CultureInfo.InvariantCulture) + "\r\nConnection: close\r\n\r\n" + json);
            await stream.WriteAsync(response, cancellationToken);
            return new LoopbackRequest(requestLine[0], requestLine[1], headers, Encoding.UTF8.GetString(body));
        }

        /// <summary>The local fixture's captured wire request.</summary>
        /// <param name="Method">The HTTP method.</param>
        /// <param name="Target">The path and query from the request line.</param>
        /// <param name="Headers">The HTTP headers.</param>
        /// <param name="Body">The UTF-8 request body.</param>
        private sealed record LoopbackRequest(string Method, string Target, Dictionary<string, string> Headers, string Body);

    }
}
