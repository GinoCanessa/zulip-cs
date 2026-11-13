using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using zulip_cs_lib;


namespace zulip_set_lib.tests
{
    /// <summary>Basic Zulip client tests.</summary>
    public class ZulipClientTests
    {
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

        [Theory]
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
                ProcessStartInfo start = curl.BuildCurlStartInfo(httpMethod, "api/v1/messages", data);
                start.ArgumentList.Add("--noproxy");
                start.ArgumentList.Add("*");
                start.ArgumentList.Add("--max-time");
                start.ArgumentList.Add("10");
                Task<LoopbackRequest> curlCapture = ReceiveLoopbackRequest(listener, timeout.Token);
                captures.Add(curlCapture);
                using Process process = Process.Start(start)!;
                try
                {
                    Task<string> stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
                    Task<string> stderr = process.StandardError.ReadToEndAsync(timeout.Token);
                    await process.WaitForExitAsync(timeout.Token);
                    string body = await stdout;
                    string errors = await stderr;
                    Assert.True(process.ExitCode == 0, errors);
                    Assert.Equal("{\"result\":\"success\",\"msg\":\"\"}", body);
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
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
                    }
                }
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

        [Fact]
        public async Task ZulipClient_CurlExitFailureRetainsDiagnostics()
        {
            ZulipClient curl = new ZulipClient("http://127.0.0.1", _email, _key, "curl");
            ZulipResponse response = await curl.DoZulipRequestCurl(
                HttpMethod.Get, "zulip-test-unsupported://127.0.0.1/", null)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(response.Success);
            Assert.Contains("curl exited with code", response.CaughtException);
            Assert.Contains("protocol", response.CaughtException!, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(string.Empty, response.HttpResponseBody);
            Assert.Same(response, curl.LastResponse);
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
        /// <returns>The request line, headers and body seen on the wire.</returns>
        private static async Task<LoopbackRequest> ReceiveLoopbackRequest(TcpListener listener, CancellationToken cancellationToken)
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
            const string json = "{\"result\":\"success\",\"msg\":\"\"}";
            byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " +
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
