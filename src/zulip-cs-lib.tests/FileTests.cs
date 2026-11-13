using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>File contract, stream ownership and real local upload transport tests.</summary>
    public class FileTests
    {
        /// <summary>Gets whether real-process upload tests can run.</summary>
        public static bool CurlAvailable => ZulipClientTests.CurlAvailable;

        /// <summary>The successful upload envelope; filename is deliberately absent.</summary>
        private const string UploadBody = "{\"result\":\"success\",\"msg\":\"\",\"url\":\"/user_uploads/1/ab/binary.dat\",\"ignored_parameters_unsupported\":[\"future\"]}";

        /// <summary>The test-only identity, never live credentials.</summary>
        private const string Email = "upload@example.org";

        /// <summary>The test-only key sensitive to command-line argument quoting.</summary>
        private const string Key = "test \"key with spaces\" + \u00e9";

        /// <summary>File helpers preserve reserved filename characters and actual false/true readiness without polling.</summary>
        [Fact]
        public async Task Files_TemporaryUrlAndThumbnailPreservePathAndState()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"url\":\"/user_uploads/temporary/token\"}",
                "{\"result\":\"success\",\"has_thumbnail\":false}",
                "{\"result\":\"success\",\"has_thumbnail\":true}");
            const string path = "1/ab/token/file + #?.png";
            Assert.Equal("/user_uploads/temporary/token", await client.Files.GetTemporaryUrl(path));
            Assert.False(await client.Files.CheckThumbnailStatus(path));
            Assert.True(await client.Files.CheckThumbnailStatus(path));
            Assert.Equal(3, requests.Count);
            Assert.Equal("/api/v1/user_uploads/1/ab/token/file%20%2B%20%23%3F.png",
                requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("/api/v1/thumbnail/status/1/ab/token/file%20%2B%20%23%3F.png",
                requests[1].RequestUri!.AbsolutePath);
            Assert.All(requests, request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Empty(request.RequestUri!.Query);
                Assert.Null(request.Content);
            });
        }

        /// <summary>Missing/wrong helper results fail explicitly rather than inventing a URL or false success.</summary>
        /// <param name="json">The response envelope.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}")]
        [InlineData("{\"result\":\"success\",\"url\":\"\",\"has_thumbnail\":null}")]
        [InlineData("{\"result\":\"success\",\"url\":false,\"has_thumbnail\":\"false\"}")]
        [InlineData("{\"result\":\"error\",\"msg\":\"No access\"}")]
        public async Task Files_HelpersRejectMalformedOrFailedResults(string json)
        {
            ZulipClient client = Utils.CreateCapturingClient([], json, json, json, json);
            Assert.False((await client.Files.TryGetTemporaryUrl("1/a/file.png")).success);
            await Assert.ThrowsAsync<Exception>(() => client.Files.GetTemporaryUrl("1/a/file.png"));
            Assert.False((await client.Files.TryCheckThumbnailStatus("1/a/file.png")).success);
            await Assert.ThrowsAsync<Exception>(() => client.Files.CheckThumbnailStatus("1/a/file.png"));
        }

        /// <summary>Invalid attachment paths never reach either endpoint.</summary>
        /// <param name="path">The invalid logical attachment path.</param>
        [Theory]
        [InlineData("/user_uploads/1/a/file.png")]
        [InlineData("0/a/file.png")]
        [InlineData("1")]
        [InlineData("1//file.png")]
        [InlineData("1/a/../file.png")]
        public async Task Files_HelpersRejectInvalidPathsWithoutRequests(string path)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            Assert.False((await client.Files.TryGetTemporaryUrl(path)).success);
            Assert.False((await client.Files.TryCheckThumbnailStatus(path)).success);
            Assert.Empty(requests);
        }

        /// <summary>HTTP serializes exact binary multipart bytes and disposes content without disposing the caller stream.</summary>
        /// <param name="seekable">Whether the caller exposes its remaining length.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Files_HttpStreamsExactMultipartAndPreservesOwnership(bool seekable)
        {
            byte[] payload = BinaryPayload();
            using UploadStream input = new UploadStream(payload, seekable);
            byte[] remaining = seekable ? payload[2..] : payload;
            if (seekable) input.Position = 2;
            HttpRequestMessage? captured = null;
            TrackingContent responseContent = new TrackingContent(UploadBody);
            using HttpClient http = new HttpClient(new CallbackHandler(async (request, token) =>
            {
                captured = request;
                Assert.Equal(0, input.BytesRead);
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/v1/user_uploads", request.RequestUri!.AbsolutePath);
                Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Email + ":" + Key)),
                    request.Headers.Authorization!.ToString());
                Assert.Equal(seekable, request.Content!.Headers.ContentLength.HasValue);
                MultipartFormDataContent content = Assert.IsType<MultipartFormDataContent>(request.Content);
                ContentDispositionHeaderValue disposition = Assert.Single(content).Headers.ContentDisposition!;
                Assert.Equal("filename", disposition.Name);
                Assert.Equal("binary \"\u96ea\".dat", disposition.FileName);
                Assert.Equal("binary \"\u96ea\".dat", disposition.FileNameStar);
                byte[] actual = await request.Content.ReadAsByteArrayAsync(token);
                byte[] expected = await ExpectedMultipart(request.Content.Headers.ContentType!, remaining, token);
                Assert.Equal(expected, actual);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };
            }));
            ZulipClient client = new ZulipClient("https://example.org", Email, Key, http);
            FileUploadResult result = await client.Files.Upload(input, "binary \"\u96ea\".dat", "application/octet-stream",
                TestContext.Current.CancellationToken);
            Assert.Equal("/user_uploads/1/ab/binary.dat", result.Url);
            Assert.Null(result.Filename);
            Assert.Same(client.LastResponse, result.Response);
            Assert.Equal(new string[] { "future" }, result.Response.IgnoredParametersUnsupported);
            Assert.Equal(remaining.Length, input.BytesRead);
            Assert.False(input.Disposed);
            Assert.True(responseContent.Disposed);
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                captured!.Content!.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        }

        /// <summary>A server-selected filename is retained without deriving or replacing it from the URL.</summary>
        [Fact]
        public async Task Files_UploadRetainsOptionalFilename()
        {
            using MemoryStream input = new MemoryStream([0, 255]);
            ZulipClient client = Utils.CreateCapturingClient([],
                "{\"result\":\"success\",\"url\":\"/user_uploads/1/escaped.dat\",\"filename\":\"stored [name].dat\"}");
            FileUploadResult result = await client.Files.Upload(input, "sent.dat",
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("stored [name].dat", result.Filename);
            Assert.True(input.CanRead);
        }

        /// <summary>ASCII quotes and backslashes remain one valid quoted filename parameter rather than corrupting multipart headers.</summary>
        /// <param name="filename">The filename requiring quoted-string escaping.</param>
        [Theory]
        [InlineData("binary \"snow\".dat")]
        [InlineData("path\\name.dat")]
        [InlineData("\"bounding quotes\"")]
        public async Task Files_UploadEscapesAsciiFilename(string filename)
        {
            using UploadStream input = new UploadStream([0, 255], true);
            using HttpClient http = new HttpClient(new CallbackHandler(async (request, token) =>
            {
                MultipartFormDataContent multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
                HttpContent file = Assert.Single(multipart);
                ContentDispositionHeaderValue parsed = ContentDispositionHeaderValue.Parse(file.Headers.ContentDisposition!.ToString());
                Assert.Equal("filename", parsed.Name);
                Assert.Equal(filename, parsed.FileNameStar);
                Assert.Equal(3, parsed.Parameters.Count);
                string escaped = filename.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
                Assert.Equal("\"" + escaped + "\"", parsed.Parameters.Single(parameter => parameter.Name == "filename").Value);
                Assert.Equal(new byte[] { 0, 255 }, await file.ReadAsByteArrayAsync(token));
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(UploadBody) };
            }));
            ZulipClient client = new ZulipClient("https://example.org", Email, Key, http);
            FileUploadResult result = await client.Files.Upload(input, filename,
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.False(input.Disposed);
        }

        /// <summary>Missing, malformed, legacy-only or non-server-relative upload URLs never produce successful defaults.</summary>
        /// <param name="fields">The malformed success fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"uri\":\"/user_uploads/1/legacy.dat\"")]
        [InlineData(",\"url\":\"\"")]
        [InlineData(",\"url\":null")]
        [InlineData(",\"url\":123")]
        [InlineData(",\"url\":\"https://example.org/file\"")]
        [InlineData(",\"url\":\"//example.org/file\"")]
        [InlineData(",\"url\":\"/\\\\example.org/file\"")]
        [InlineData(",\"url\":\"/user_uploads/1/file\",\"filename\":10")]
        public async Task Files_UploadRequiresRealCurrentUrl(string fields)
        {
            using UploadStream input = new UploadStream([0, 255], false);
            string body = "{\"result\":\"success\"" + fields + "}";
            ZulipClient client = Utils.CreateCapturingClient([], body, body);
            (bool success, string? details, FileUploadResult? result) outcome =
                await client.Files.TryUpload(input, "file.dat", cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(outcome.success);
            Assert.Null(outcome.result);
            Assert.Contains("Files.Upload failed:", outcome.details);
            Assert.False(input.Disposed);
            await Assert.ThrowsAsync<Exception>(() =>
                client.Files.Upload(input, "file.dat", cancellationToken: TestContext.Current.CancellationToken));
            Assert.False(input.Disposed);
        }

        /// <summary>Control-character filenames and MIME headers fail before the request delegate can run.</summary>
        /// <param name="filename">The supplied filename.</param>
        /// <param name="contentType">The supplied MIME type.</param>
        [Theory]
        [InlineData("", null)]
        [InlineData("bad\r\nInjected: yes", null)]
        [InlineData("bad\0.dat", null)]
        [InlineData("good.dat", "text/plain\r\nInjected: yes")]
        [InlineData("good.dat", "text/plain\t")]
        [InlineData("good.dat", "not a mime type")]
        public async Task Files_UploadRejectsHeaderInjection(string filename, string? contentType)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, UploadBody);
            using UploadStream input = new UploadStream([0], true);
            (bool success, string? details, FileUploadResult? result) outcome =
                await client.Files.TryUpload(input, filename, contentType, TestContext.Current.CancellationToken);
            Assert.False(outcome.success);
            Assert.Contains("Files.Upload failed:", outcome.details);
            Assert.Empty(requests);
            Assert.Equal(0, input.BytesRead);
            Assert.False(input.Disposed);
        }

        /// <summary>Unreadable input is rejected before launching even a missing curl executable.</summary>
        [Fact]
        public async Task Files_UploadRejectsUnreadableStream()
        {
            MemoryStream input = new MemoryStream();
            input.Dispose();
            ZulipClient client = new ZulipClient("https://example.org", Email, Key, "nonexistent-curl");
            (bool success, string? details, FileUploadResult? result) outcome =
                await client.Files.TryUpload(input, "file.dat", cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(outcome.success);
            Assert.Contains("stream must be readable", outcome.details);
            Assert.Null(client.LastResponse);
        }

        /// <summary>HTTP failures preserve real response diagnostics and do not transfer stream ownership.</summary>
        /// <param name="status">The actual HTTP status.</param>
        /// <param name="body">The actual response body.</param>
        /// <param name="failure">The required error detail.</param>
        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "{\"result\":\"error\",\"code\":\"BAD_REQUEST\",\"msg\":\"Upload quota exceeded\"}", "Upload quota exceeded")]
        [InlineData(HttpStatusCode.Forbidden, UploadBody, "403")]
        [InlineData(HttpStatusCode.OK, "not JSON", "failed")]
        [InlineData(HttpStatusCode.OK, "null", "JSON object")]
        public async Task Files_HttpFailurePreservesActualResponse(HttpStatusCode status, string body, string failure)
        {
            TrackingContent content = new TrackingContent(body);
            using HttpClient http = new HttpClient(new CallbackHandler((request, token) =>
                Task.FromResult(new HttpResponseMessage(status) { Content = content })));
            ZulipClient client = new ZulipClient("https://example.org", Email, Key, http);
            using UploadStream input = new UploadStream([0, 255], false);
            (bool success, string? details, FileUploadResult? result) outcome =
                await client.Files.TryUpload(input, "file.dat", cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(outcome.success);
            Assert.Null(outcome.result);
            Assert.Contains(failure, outcome.details);
            Assert.Equal((int)status, client.LastResponse!.HttpResponseCode);
            Assert.Equal(body, client.LastResponse.HttpResponseBody);
            Assert.False(client.LastResponse.Success);
            Assert.True(content.Disposed);
            Assert.False(input.Disposed);
        }

        /// <summary>Caller cancellation and upload deadlines both bound HTTP operations while leaving streams open.</summary>
        /// <param name="callerCancellation">Whether the caller cancels rather than the client timeout.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Files_HttpCancellationAndTimeoutAreBounded(bool callerCancellation)
        {
            TaskCompletionSource entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using HttpClient http = new HttpClient(new CallbackHandler(async (request, token) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }));
            ZulipClient client = new ZulipClient("https://example.org", Email, Key, http)
            {
                UploadTimeout = callerCancellation ? TimeSpan.FromSeconds(10) : TimeSpan.FromMilliseconds(100)
            };
            using CancellationTokenSource cancellation =
                CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            using UploadStream input = new UploadStream([0, 255], false);
            Task<(bool success, string? details, FileUploadResult? result)> operation =
                client.Files.TryUpload(input, "file.dat", cancellationToken: cancellation.Token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            if (callerCancellation) cancellation.Cancel();
            (bool success, string? details, FileUploadResult? result) outcome =
                await operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(outcome.success);
            Assert.Contains(callerCancellation ? "canceled" : "timed out", outcome.details);
            Assert.False(input.Disposed);
            Assert.False(client.LastResponse!.Success);
        }

        /// <summary>Lists current message IDs, second-based timestamps, 64-bit sizes and organization-wide usage.</summary>
        [Fact]
        public async Task Files_GetAllRetainsCurrentMetadataAndEmptyLists()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"attachments\":[{\"id\":7,\"name\":\"binary.dat\",\"path_id\":\"1/ab/binary.dat\"," +
                "\"size\":4294967296,\"create_time\":1588145417,\"message_ids\":[102,103]}],\"upload_space_used\":8589934592," +
                "\"ignored_parameters_unsupported\":[\"future\"]}",
                "{\"result\":\"success\",\"attachments\":[],\"upload_space_used\":0}");
            AttachmentListResult result = await client.Files.GetAll();
            Assert.Single(result.Attachments);
            Assert.Equal(7, result.Attachments[0].Id);
            Assert.Equal("binary.dat", result.Attachments[0].Name);
            Assert.Equal("1/ab/binary.dat", result.Attachments[0].PathId);
            Assert.Equal(4294967296L, result.Attachments[0].Size);
            Assert.Equal(1588145417L, result.Attachments[0].CreateTime);
            Assert.Equal(new int[] { 102, 103 }, result.Attachments[0].MessageIds);
            Assert.Equal(8589934592L, result.UploadSpaceUsed);
            Assert.Same(client.LastResponse, result.Response);
            Assert.Equal(new string[] { "future" }, result.Response.IgnoredParametersUnsupported);
            AttachmentListResult empty = await client.Files.GetAll();
            Assert.Empty(empty.Attachments);
            Assert.Equal(0, empty.UploadSpaceUsed);
            Assert.All(requests, request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v1/attachments", request.RequestUri!.AbsolutePath);
                Assert.Null(request.Content);
            });
        }

        /// <summary>Required attachment metadata is validated rather than replaced by CLR defaults or the old messages field.</summary>
        /// <param name="body">The invalid current response.</param>
        [Theory]
        [InlineData("{\"result\":\"success\",\"upload_space_used\":0}")]
        [InlineData("{\"result\":\"success\",\"attachments\":[]}")]
        [InlineData("{\"result\":\"success\",\"attachments\":[],\"upload_space_used\":-1}")]
        [InlineData("{\"result\":\"success\",\"attachments\":[],\"upload_space_used\":1.5}")]
        [InlineData("{\"result\":\"success\",\"attachments\":{},\"upload_space_used\":0}")]
        [InlineData("{\"result\":\"success\",\"attachments\":[{\"id\":\"7\",\"message_ids\":[]}],\"upload_space_used\":0}")]
        [InlineData("{\"result\":\"success\",\"attachments\":[{\"id\":7,\"messages\":[]}],\"upload_space_used\":0}")]
        [InlineData("{\"result\":\"success\",\"attachments\":[{\"id\":7,\"message_ids\":[\"102\"]}],\"upload_space_used\":0}")]
        [InlineData("{\"result\":\"success\",\"attachments\":[{\"id\":7,\"name\":\"f\",\"path_id\":\"1/f\",\"message_ids\":[],\"size\":0}],\"upload_space_used\":0}")]
        [InlineData("{\"result\":\"success\",\"attachments\":[{\"id\":7,\"name\":\"f\",\"path_id\":\"1/f\",\"message_ids\":[],\"size\":null,\"create_time\":0}],\"upload_space_used\":0}")]
        [InlineData("not JSON")]
        [InlineData("{\"result\":\"error\",\"code\":\"UNAUTHORIZED\",\"msg\":\"Actual list failure\"}")]
        public async Task Files_GetAllRejectsMalformedOrErrorPayload(string body)
        {
            ZulipClient client = Utils.CreateCapturingClient([], body, body);
            (bool success, string? details, AttachmentListResult? result) outcome = await client.Files.TryGetAll();
            Assert.False(outcome.success);
            Assert.Null(outcome.result);
            Assert.Contains("Files.GetAll failed:", outcome.details);
            if (body.Contains("Actual list failure", StringComparison.Ordinal))
                Assert.Contains("Actual list failure", outcome.details);
            await Assert.ThrowsAsync<Exception>(() => client.Files.GetAll());
        }

        /// <summary>Deletion uses the attachment identity and preserves the server's actual error.</summary>
        [Fact]
        public async Task Files_DeleteUsesAttachmentIdAndPropagatesErrors()
        {
            List<HttpRequestMessage> requests = [];
            const string failure = "{\"result\":\"error\",\"code\":\"BAD_REQUEST\",\"msg\":\"Invalid attachment\"}";
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"msg\":\"\"}", failure, failure);
            await client.Files.Delete(7);
            (bool success, string? details) outcome = await client.Files.TryDelete(8);
            Assert.False(outcome.success);
            Assert.Contains("BAD_REQUEST", outcome.details);
            Assert.Contains("Invalid attachment", outcome.details);
            await Assert.ThrowsAsync<Exception>(() => client.Files.Delete(9));
            Assert.Equal(new string[] { "/api/v1/attachments/7", "/api/v1/attachments/8", "/api/v1/attachments/9" },
                requests.Select(request => request.RequestUri!.AbsolutePath));
            Assert.All(requests, request =>
            {
                Assert.Equal(HttpMethod.Delete, request.Method);
                Assert.Null(request.Content);
            });
        }

        /// <summary>Both production upload backends send identical serialized binary multipart with known-length or chunked streaming.</summary>
        /// <param name="seekable">Whether both inputs expose a computable length.</param>
        [Theory(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Files_RealCurlMatchesHttpMultipartOnLoopback(bool seekable)
        {
            using CancellationTokenSource deadline = TestDeadline();
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                string site = LoopbackSite(listener);
                byte[] payload = BinaryPayload();
                using HttpClient http = new HttpClient(new HttpClientHandler { UseProxy = false });
                ZulipClient httpClient = new ZulipClient(site, Email, Key, http) { UploadTimeout = TimeSpan.FromSeconds(10) };
                ZulipClient curlClient = new ZulipClient(site, Email, Key, "curl") { UploadTimeout = TimeSpan.FromSeconds(10) };
                using UploadStream httpInput = new UploadStream(payload, seekable);
                using UploadStream curlInput = new UploadStream(payload, seekable);
                Task<WireRequest> firstCapture = ReceiveAndRespond(listener, UploadBody, 200, deadline.Token);
                FileUploadResult first = await httpClient.Files.Upload(httpInput, "binary \"\u96ea\".dat", cancellationToken: deadline.Token);
                WireRequest httpRequest = await firstCapture;
                Task<WireRequest> secondCapture = ReceiveAndRespond(listener, UploadBody, 200, deadline.Token);
                FileUploadResult second = await curlClient.Files.Upload(curlInput, "binary \"\u96ea\".dat", cancellationToken: deadline.Token);
                WireRequest curlRequest = await secondCapture;
                Assert.Equal(httpRequest.Method, curlRequest.Method);
                Assert.Equal("POST", curlRequest.Method);
                Assert.Equal("/api/v1/user_uploads", curlRequest.Target);
                Assert.Equal(httpRequest.Target, curlRequest.Target);
                Assert.Equal(httpRequest.Headers["Authorization"], curlRequest.Headers["Authorization"]);
                Assert.Equal(await ExpectedMultipart(MediaTypeHeaderValue.Parse(curlRequest.Headers["Content-Type"]), payload, deadline.Token),
                    curlRequest.Body);
                Assert.Equal(NormalizedMultipart(httpRequest), NormalizedMultipart(curlRequest));
                Assert.Equal(seekable, curlRequest.Headers.ContainsKey("Content-Length"));
                if (!seekable) Assert.Equal("chunked", curlRequest.Headers["Transfer-Encoding"]);
                Assert.Equal(first.Url, second.Url);
                Assert.Equal(200, second.Response.HttpResponseCode);
                Assert.Equal(UploadBody, second.Response.HttpResponseBody);
                Assert.DoesNotContain("ZULIP_UPLOAD_STATUS_", second.Response.HttpResponseBody);
                Assert.False(httpInput.Disposed);
                Assert.False(curlInput.Disposed);
            }
            finally
            {
                deadline.Cancel();
                listener.Stop();
            }
        }

        /// <summary>Avatar/emoji use the actual focused multipart route on both backends without upload-only result requirements.</summary>
        /// <param name="avatar">Whether to upload an avatar rather than emoji.</param>
        [Theory(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Files_ImageUploadsPreserveBackendRoutingAndOwnership(bool avatar)
        {
            using CancellationTokenSource deadline = TestDeadline();
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                string site = LoopbackSite(listener);
                byte[] payload = BinaryPayload();
                string body = avatar
                    ? "{\"result\":\"success\",\"avatar_url\":\"/user_avatars/1/image.png\"}"
                    : "{\"result\":\"success\"}";
                using HttpClient http = new HttpClient(new HttpClientHandler { UseProxy = false });
                ZulipClient httpClient = new ZulipClient(site, Email, Key, http) { UploadTimeout = TimeSpan.FromSeconds(10) };
                ZulipClient curlClient = new ZulipClient(site, Email, Key, "curl") { UploadTimeout = TimeSpan.FromSeconds(10) };
                using UploadStream httpInput = new UploadStream(payload, false);
                using UploadStream curlInput = new UploadStream(payload, false);
                Task<WireRequest> firstCapture = ReceiveAndRespond(listener, body, 200, deadline.Token);
                await UploadImage(httpClient, avatar, httpInput, deadline.Token);
                WireRequest first = await firstCapture;
                Task<WireRequest> secondCapture = ReceiveAndRespond(listener, body, 200, deadline.Token);
                await UploadImage(curlClient, avatar, curlInput, deadline.Token);
                WireRequest second = await secondCapture;
                Assert.Equal("POST", second.Method);
                Assert.Equal(avatar ? "/api/v1/users/me/avatar" : "/api/v1/realm/emoji/smile%2Btest", second.Target);
                Assert.Equal(first.Target, second.Target);
                Assert.Equal(NormalizedMultipart(first), NormalizedMultipart(second));
                string disposition = Encoding.UTF8.GetString(second.Body).Split("\r\n")
                    .Single(line => line.StartsWith("Content-Disposition: ", StringComparison.Ordinal));
                Assert.Equal(avatar ? "file" : "filename", ContentDispositionHeaderValue.Parse(
                    disposition.Substring("Content-Disposition: ".Length)).Name);
                Assert.Equal(200, curlClient.LastResponse!.HttpResponseCode);
                Assert.Equal(body, curlClient.LastResponse.HttpResponseBody);
                Assert.False(httpInput.Disposed);
                Assert.False(curlInput.Disposed);
            }
            finally
            {
                deadline.Cancel();
                listener.Stop();
            }
        }

        /// <summary>Runs the public image upload without depending on its result type.</summary>
        /// <param name="client">The selected real backend.</param>
        /// <param name="avatar">Whether to upload an avatar.</param>
        /// <param name="stream">The caller-owned stream.</param>
        /// <param name="token">The bounded test cancellation token.</param>
        private static async Task UploadImage(ZulipClient client, bool avatar, Stream stream, CancellationToken token)
        {
            if (avatar)
                await client.Users.UploadAvatar(stream, "binary \"\u96ea\".dat", cancellationToken: token);
            else
                await client.Organization.UploadCustomEmoji("smile+test", stream, "binary \"\u96ea\".dat", cancellationToken: token);
        }

        /// <summary>An early HTTP rejection takes precedence over a broken binary stdin pipe and retains the actual error body/status.</summary>
        [Fact(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        public async Task Files_CurlEarlyRejectionRetainsActualHttpFailure()
        {
            using CancellationTokenSource deadline = TestDeadline();
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                const string body = "{\"result\":\"error\",\"code\":\"BAD_REQUEST\",\"msg\":\"Actual early upload rejection\"}";
                Task server = RejectBeforeBody(listener, body, deadline.Token);
                ZulipClient client = new ZulipClient(LoopbackSite(listener), Email, Key, "curl")
                {
                    UploadTimeout = TimeSpan.FromSeconds(10)
                };
                using UploadStream input = new UploadStream([], false, endless: true);
                (bool success, string? details, FileUploadResult? result) outcome =
                    await client.Files.TryUpload(input, "large.dat", cancellationToken: deadline.Token);
                await server;
                Assert.False(outcome.success);
                Assert.Contains("Actual early upload rejection", outcome.details);
                Assert.Equal(413, client.LastResponse!.HttpResponseCode);
                Assert.Equal(body, client.LastResponse.HttpResponseBody);
                Assert.Equal("BAD_REQUEST", client.LastResponse.ErrorCode);
                Assert.False(input.Disposed);
            }
            finally
            {
                deadline.Cancel();
                listener.Stop();
            }
        }

        /// <summary>Curl exit cancels a pending cooperative source read without discarding the received HTTP response or independent source failures.</summary>
        /// <param name="status">The early response status.</param>
        /// <param name="sourceFault">Whether the source fails independently instead of being canceled by process exit.</param>
        [Theory(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        [InlineData(413, false)]
        [InlineData(200, false)]
        [InlineData(200, true)]
        public async Task Files_CurlEarlyResponseInterruptsPendingReadAndPreservesFailures(int status, bool sourceFault)
        {
            using CancellationTokenSource deadline = TestDeadline();
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using BlockingUploadStream input = new BlockingUploadStream();
            string body = status == 413
                ? "{\"result\":\"error\",\"code\":\"BAD_REQUEST\",\"msg\":\"Actual blocked-source rejection\"}"
                : UploadBody;
            Task server = RejectBeforeBody(listener, body, deadline.Token,
                sourceFault ? input.Faulted.Task : input.Waiting.Task, status);
            Task<(bool success, string? details, FileUploadResult? result)>? operation = null;
            try
            {
                ZulipClient client = new ZulipClient(LoopbackSite(listener), Email, Key, "curl")
                {
                    UploadTimeout = TimeSpan.FromSeconds(100)
                };
                operation = client.Files.TryUpload(input, "pending.dat", cancellationToken: deadline.Token);
                await input.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), deadline.Token);
                if (sourceFault) input.Release.TrySetResult();
                (bool success, string? details, FileUploadResult? result) outcome =
                    await operation.WaitAsync(TimeSpan.FromSeconds(8), deadline.Token);
                await server.WaitAsync(TimeSpan.FromSeconds(5), deadline.Token);
                Assert.False(outcome.success);
                Assert.Null(outcome.result);
                Assert.Equal(status, client.LastResponse!.HttpResponseCode);
                Assert.Equal(body, client.LastResponse.HttpResponseBody);
                Assert.False(input.Disposed);
                Assert.False(client.LastResponse.Success);
                Assert.DoesNotContain("timed out", outcome.details);
                Assert.DoesNotContain("cleanup exceeded", outcome.details);
                if (sourceFault)
                {
                    Assert.Contains("Actual independent source failure", outcome.details);
                    Assert.False(input.CancellationObserved);
                }
                else
                {
                    Assert.True(input.CancellationObserved);
                    Assert.Contains(status == 413 ? "Actual blocked-source rejection" : "Upload incomplete", outcome.details);
                    Assert.DoesNotContain("Upload canceled", outcome.details);
                }
            }
            finally
            {
                deadline.Cancel();
                listener.Stop();
                if (operation != null)
                {
                    try { await operation.WaitAsync(TimeSpan.FromSeconds(8), TestContext.Current.CancellationToken); }
                    catch (OperationCanceledException) { }
                    catch (TimeoutException) { }
                }
                try { await server.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken); }
                catch (OperationCanceledException) { }
                catch (SocketException) { }
                catch (IOException) { }
                catch (TimeoutException) { }
            }
        }

        /// <summary>Curl preserves malformed envelopes and rejects success-shaped bodies on failed HTTP requests.</summary>
        /// <param name="status">The actual HTTP response status.</param>
        /// <param name="body">The malformed envelope.</param>
        [Theory(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        [InlineData(200, "null")]
        [InlineData(200, "not JSON")]
        [InlineData(403, UploadBody)]
        public async Task Files_CurlFailureRetainsBodyAndStatus(int status, string body)
        {
            using CancellationTokenSource deadline = TestDeadline();
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                Task<WireRequest> server = ReceiveAndRespond(listener, body, status, deadline.Token);
                ZulipClient client = new ZulipClient(LoopbackSite(listener), Email, Key, "curl");
                using UploadStream input = new UploadStream([0, 255], true);
                (bool success, string? details, FileUploadResult? result) outcome =
                    await client.Files.TryUpload(input, "file.dat", cancellationToken: deadline.Token);
                await server;
                Assert.False(outcome.success);
                Assert.Contains("Files.Upload failed:", outcome.details);
                Assert.Equal(status, client.LastResponse!.HttpResponseCode);
                Assert.Equal(body, client.LastResponse.HttpResponseBody);
                if (status == 200) Assert.NotNull(client.LastResponse.CaughtException);
                else Assert.Contains(status.ToString(CultureInfo.InvariantCulture), outcome.details);
                Assert.False(input.Disposed);
            }
            finally
            {
                deadline.Cancel();
                listener.Stop();
            }
        }

        /// <summary>Cancellation and deadlines stop real curl and its serializer, independently of a stalled local receiver.</summary>
        /// <param name="callerCancellation">Whether the caller cancels instead of timing out.</param>
        [Theory(Skip = "Requires curl on PATH.", SkipUnless = nameof(CurlAvailable))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Files_CurlCancellationAndTimeoutStopStreaming(bool callerCancellation)
        {
            using CancellationTokenSource deadline = TestDeadline();
            using CancellationTokenSource cancellation =
                CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            TaskCompletionSource entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task server = ReceiveUntilChildCloses(listener, entered, release, deadline.Token);
            try
            {
                ZulipClient client = new ZulipClient(LoopbackSite(listener), Email, Key, "curl")
                {
                    UploadTimeout = callerCancellation ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(1)
                };
                using UploadStream input = new UploadStream([], false, endless: true);
                Task<(bool success, string? details, FileUploadResult? result)> operation =
                    client.Files.TryUpload(input, "large.dat", cancellationToken: cancellation.Token);
                await entered.Task.WaitAsync(deadline.Token);
                if (callerCancellation) cancellation.Cancel();
                (bool success, string? details, FileUploadResult? result) outcome = await operation.WaitAsync(deadline.Token);
                Assert.False(outcome.success);
                Assert.Contains(callerCancellation ? "canceled" : "timed out", outcome.details);
                Assert.False(input.Disposed);
                long finalReads = input.BytesRead;
                release.TrySetResult();
                await server.WaitAsync(deadline.Token);
                await Task.Delay(50, deadline.Token);
                Assert.Equal(finalReads, input.BytesRead);
                Assert.DoesNotContain("cleanup exceeded", outcome.details);
            }
            finally
            {
                cancellation.Cancel();
                release.TrySetResult();
                deadline.Cancel();
                listener.Stop();
                try { await server; }
                catch (OperationCanceledException) { }
                catch (SocketException) { }
                catch (IOException) { }
            }
        }

        /// <summary>Startup failures remain explicit without requiring curl or touching a content file.</summary>
        [Fact]
        public async Task Files_MissingCurlRetainsStartupErrorAndOwnership()
        {
            string executable = Path.Combine(Environment.CurrentDirectory, "missing-upload-curl-" + Guid.NewGuid().ToString("N") + ".exe");
            ZulipClient client = new ZulipClient("https://example.org", Email, Key, executable);
            using UploadStream input = new UploadStream([0, 255], false);
            (bool success, string? details, FileUploadResult? result) outcome =
                await client.Files.TryUpload(input, "file.dat", cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(outcome.success);
            Assert.Contains("Unable to start curl:", outcome.details);
            Assert.Contains("Unable to start curl:", client.LastResponse!.CaughtException);
            Assert.DoesNotContain(Key, outcome.details);
            Assert.False(input.Disposed);
            Assert.Equal(0, input.BytesRead);
        }

        /// <summary>Creates bytes that reveal text encoding or newline transformations.</summary>
        /// <returns>The binary test payload.</returns>
        private static byte[] BinaryPayload() => Enumerable.Range(0, 256).Select(value => (byte)value).Concat(new byte[] { 0, 13, 10, 255 }).ToArray();

        /// <summary>Serializes a test-only in-memory reference using the actual boundary and documented multipart field.</summary>
        /// <param name="type">The production multipart content type.</param>
        /// <param name="payload">The expected file bytes.</param>
        /// <param name="token">The fixture deadline.</param>
        /// <returns>The exact expected multipart bytes.</returns>
        private static async Task<byte[]> ExpectedMultipart(MediaTypeHeaderValue type, byte[] payload, CancellationToken token)
        {
            string boundary = type.Parameters.Single(parameter => parameter.Name == "boundary").Value!.Trim('"');
            using MultipartFormDataContent multipart = new MultipartFormDataContent(boundary);
            StreamContent file = new StreamContent(new MemoryStream(payload));
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            const string filename = "binary \"\u96ea\".dat";
            file.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "filename",
                FileName = "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(filename)) + "?=",
                FileNameStar = filename
            };
            multipart.Add(file);
            return await multipart.ReadAsByteArrayAsync(token);
        }

        /// <summary>Normalizes only the randomized ASCII boundary while preserving every binary byte.</summary>
        /// <param name="request">The captured request.</param>
        /// <returns>The comparable multipart bytes.</returns>
        private static byte[] NormalizedMultipart(WireRequest request)
        {
            MediaTypeHeaderValue type = MediaTypeHeaderValue.Parse(request.Headers["Content-Type"]);
            string boundary = type.Parameters.Single(parameter => parameter.Name == "boundary").Value!.Trim('"');
            return Encoding.Latin1.GetBytes(Encoding.Latin1.GetString(request.Body).Replace(boundary, "BOUNDARY", StringComparison.Ordinal));
        }

        /// <summary>Creates an independent, bounded deadline for all fixture sockets and operations.</summary>
        /// <returns>The fixture cancellation source.</returns>
        private static CancellationTokenSource TestDeadline()
        {
            CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(20));
            return deadline;
        }

        /// <summary>Gets the listener's ephemeral loopback URL.</summary>
        /// <param name="listener">The started fixture listener.</param>
        /// <returns>The site URL.</returns>
        private static string LoopbackSite(TcpListener listener) =>
            "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture);

        /// <summary>Captures a bounded request, including chunked bodies, and returns a local JSON response.</summary>
        /// <param name="listener">The loopback listener.</param>
        /// <param name="body">The response body.</param>
        /// <param name="status">The response status.</param>
        /// <param name="token">The fixture deadline.</param>
        /// <returns>The captured request.</returns>
        private static async Task<WireRequest> ReceiveAndRespond(TcpListener listener, string body, int status, CancellationToken token)
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync(token);
            using NetworkStream stream = connection.GetStream();
            WireRequest headers = await ReadHeaders(stream, token);
            if (headers.Headers.TryGetValue("Expect", out string? expect) && expect.Equals("100-continue", StringComparison.OrdinalIgnoreCase))
                await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n"), token);
            using MemoryStream content = new MemoryStream();
            if (headers.Headers.TryGetValue("Content-Length", out string? length))
            {
                int count = int.Parse(length, CultureInfo.InvariantCulture);
                if (count < 0 || count > 1048576) throw new IOException("Fixture body limit exceeded.");
                byte[] bytes = new byte[count];
                await stream.ReadExactlyAsync(bytes, token);
                content.Write(bytes);
            }
            else if (headers.Headers.TryGetValue("Transfer-Encoding", out string? transfer) && transfer == "chunked")
            {
                while (true)
                {
                    int count = int.Parse((await ReadLine(stream, token)).Split(';')[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if (count == 0)
                    {
                        while ((await ReadLine(stream, token)).Length != 0) { }
                        break;
                    }
                    if (count < 0 || content.Length + count > 1048576) throw new IOException("Fixture chunked body limit exceeded.");
                    byte[] bytes = new byte[count];
                    await stream.ReadExactlyAsync(bytes, token);
                    content.Write(bytes);
                    if ((await ReadLine(stream, token)).Length != 0) throw new IOException("Invalid chunk terminator.");
                }
            }
            await SendResponse(stream, body, status, token);
            return headers with { Body = content.ToArray() };
        }

        /// <summary>Rejects an upload before consuming its body to exercise broken-stdin recovery.</summary>
        /// <param name="listener">The loopback listener.</param>
        /// <param name="body">The actual error body.</param>
        /// <param name="token">The fixture deadline.</param>
        /// <param name="beforeResponse">An optional source-read signal to await before responding.</param>
        /// <param name="status">The early HTTP response status.</param>
        private static async Task RejectBeforeBody(
            TcpListener listener, string body, CancellationToken token, Task? beforeResponse = null, int status = 413)
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync(token);
            using NetworkStream stream = connection.GetStream();
            await ReadHeaders(stream, token);
            if (beforeResponse != null) await beforeResponse.WaitAsync(token);
            await SendResponse(stream, body, status, token);
            if (status is >= 200 and < 300) connection.Client.Shutdown(SocketShutdown.Send);
            // Drain until the client observes rejection so closing with unread data does not reset the response.
            byte[] buffer = new byte[16384];
            try { while (await stream.ReadAsync(buffer, token) != 0) { } }
            catch (IOException) { }
        }

        /// <summary>Stalls the receiver, then verifies the canceled child closes its local connection.</summary>
        /// <param name="listener">The loopback listener.</param>
        /// <param name="entered">Signals receipt of headers.</param>
        /// <param name="release">Signals that the operation has returned.</param>
        /// <param name="token">The fixture deadline.</param>
        private static async Task ReceiveUntilChildCloses(
            TcpListener listener, TaskCompletionSource entered, TaskCompletionSource release, CancellationToken token)
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync(token);
            using NetworkStream stream = connection.GetStream();
            await ReadHeaders(stream, token);
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            byte[] buffer = new byte[16384];
            long bytes = 0;
            while (true)
            {
                int count;
                try { count = await stream.ReadAsync(buffer, token); }
                catch (IOException) { break; }
                if (count == 0) break;
                bytes += count;
                if (bytes > 16777216) throw new IOException("Canceled child did not stop sending.");
            }
        }

        /// <summary>Reads bounded ASCII HTTP headers without pre-consuming binary body bytes.</summary>
        /// <param name="stream">The fixture connection.</param>
        /// <param name="token">The fixture deadline.</param>
        /// <returns>The parsed request headers.</returns>
        private static async Task<WireRequest> ReadHeaders(NetworkStream stream, CancellationToken token)
        {
            string[] request = (await ReadLine(stream, token)).Split(' ', 3);
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int length = 0;
            while (true)
            {
                string line = await ReadLine(stream, token);
                if (line.Length == 0) break;
                length += line.Length;
                if (length > 16384) throw new IOException("Fixture header limit exceeded.");
                string[] parts = line.Split(':', 2);
                headers.Add(parts[0], parts[1].Trim());
            }
            return new WireRequest(request[0], request[1], headers, []);
        }

        /// <summary>Reads one bounded CRLF-terminated ASCII line.</summary>
        /// <param name="stream">The fixture connection.</param>
        /// <param name="token">The fixture deadline.</param>
        /// <returns>The line without CRLF.</returns>
        private static async Task<string> ReadLine(NetworkStream stream, CancellationToken token)
        {
            byte[] buffer = new byte[16384];
            int length = 0;
            while (length < buffer.Length)
            {
                int count = await stream.ReadAsync(buffer.AsMemory(length, 1), token);
                if (count == 0) throw new IOException("Fixture connection closed before a complete line.");
                length += count;
                if (length >= 2 && buffer[length - 2] == 13 && buffer[length - 1] == 10)
                    return Encoding.ASCII.GetString(buffer, 0, length - 2);
            }
            throw new IOException("Fixture line limit exceeded.");
        }

        /// <summary>Sends the exact local HTTP status and response body.</summary>
        /// <param name="stream">The fixture connection.</param>
        /// <param name="body">The actual response body.</param>
        /// <param name="status">The HTTP status.</param>
        /// <param name="token">The fixture deadline.</param>
        private static async Task SendResponse(NetworkStream stream, string body, int status, CancellationToken token)
        {
            byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 " + status.ToString(CultureInfo.InvariantCulture) +
                " Test response\r\nContent-Type: application/json\r\nContent-Length: " +
                Encoding.UTF8.GetByteCount(body).ToString(CultureInfo.InvariantCulture) + "\r\nConnection: close\r\n\r\n" + body);
            await stream.WriteAsync(response, token);
        }

        /// <summary>The local fixture's binary request capture.</summary>
        /// <param name="Method">The request method.</param>
        /// <param name="Target">The API path.</param>
        /// <param name="Headers">The parsed headers.</param>
        /// <param name="Body">The binary multipart bytes.</param>
        private sealed record WireRequest(string Method, string Target, Dictionary<string, string> Headers, byte[] Body);

        /// <summary>A deterministic injected HTTP handler.</summary>
        private sealed class CallbackHandler : HttpMessageHandler
        {
            /// <summary>The request callback.</summary>
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _callback;

            /// <summary>Initializes the handler.</summary>
            /// <param name="callback">The asynchronous request callback.</param>
            internal CallbackHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) => _callback = callback;

            /// <summary>Processes the injected request.</summary>
            /// <param name="request">The request.</param>
            /// <param name="cancellationToken">The request deadline.</param>
            /// <returns>The response.</returns>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                _callback(request, cancellationToken);
        }

        /// <summary>Tracks response-content disposal.</summary>
        private sealed class TrackingContent : StringContent
        {
            /// <summary>Initializes response content.</summary>
            /// <param name="body">The JSON or malformed response.</param>
            internal TrackingContent(string body) : base(body, Encoding.UTF8, "application/json") { }

            /// <summary>Gets whether response disposal reached its content.</summary>
            internal bool Disposed { get; private set; }

            /// <summary>Tracks actual disposal.</summary>
            /// <param name="disposing">Whether explicitly disposing.</param>
            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        /// <summary>A readable fixture stream with optional seekability or endless bounded-buffer reads.</summary>
        private class UploadStream : Stream
        {
            /// <summary>The small test payload.</summary>
            private readonly byte[] _payload;

            /// <summary>Whether the stream reports seekability.</summary>
            private readonly bool _seekable;

            /// <summary>Whether to keep supplying bytes until canceled.</summary>
            private readonly bool _endless;

            /// <summary>The current read position.</summary>
            private long _position;

            /// <summary>The number of bytes actually read, excluding seek offsets.</summary>
            private long _bytesRead;

            /// <summary>Initializes a caller-owned input fixture.</summary>
            /// <param name="payload">The test file bytes.</param>
            /// <param name="seekable">Whether length/position are available.</param>
            /// <param name="endless">Whether reads produce an unbounded upload without an unbounded allocation.</param>
            internal UploadStream(byte[] payload, bool seekable, bool endless = false)
            {
                _payload = payload;
                _seekable = seekable;
                _endless = endless;
            }

            /// <summary>Gets the number of actual source bytes consumed.</summary>
            internal long BytesRead => Interlocked.Read(ref _bytesRead);

            /// <summary>Gets whether anyone disposed the caller-owned input.</summary>
            internal bool Disposed { get; private set; }

            /// <summary>Gets whether reading is available.</summary>
            public override bool CanRead => !Disposed;

            /// <summary>Gets whether seeking is exposed.</summary>
            public override bool CanSeek => _seekable;

            /// <summary>Gets whether writing is available.</summary>
            public override bool CanWrite => false;

            /// <summary>Gets the length only for seekable fixtures.</summary>
            public override long Length => _seekable ? _payload.Length : throw new NotSupportedException();

            /// <summary>Gets or sets the position only for seekable fixtures.</summary>
            public override long Position
            {
                get => _seekable ? _position : throw new NotSupportedException();
                set => _position = _seekable ? value : throw new NotSupportedException();
            }

            /// <summary>Reads into a bounded destination buffer.</summary>
            /// <param name="buffer">The destination.</param>
            /// <param name="offset">The destination offset.</param>
            /// <param name="count">The maximum count.</param>
            /// <returns>The byte count.</returns>
            public override int Read(byte[] buffer, int offset, int count) => ReadCore(buffer.AsSpan(offset, count));

            /// <summary>Reads without allocating or buffering the upload.</summary>
            /// <param name="buffer">The destination.</param>
            /// <param name="cancellationToken">The upload deadline.</param>
            /// <returns>The byte count.</returns>
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(ReadCore(buffer.Span));
            }

            /// <summary>Reads deterministic binary data.</summary>
            /// <param name="buffer">The destination.</param>
            /// <returns>The byte count.</returns>
            private int ReadCore(Span<byte> buffer)
            {
                if (Disposed) throw new ObjectDisposedException(nameof(UploadStream));
                int count = _endless ? buffer.Length : (int)Math.Min(buffer.Length, _payload.Length - _position);
                if (_endless) buffer.Fill(0xA5);
                else _payload.AsSpan((int)_position, count).CopyTo(buffer);
                Interlocked.Add(ref _position, count);
                Interlocked.Add(ref _bytesRead, count);
                return count;
            }

            /// <summary>Seeks the fixture.</summary>
            /// <param name="offset">The seek offset.</param>
            /// <param name="origin">The seek origin.</param>
            /// <returns>The new position.</returns>
            public override long Seek(long offset, SeekOrigin origin)
            {
                if (!_seekable) throw new NotSupportedException();
                _position = origin switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => _position + offset,
                    SeekOrigin.End => _payload.Length + offset,
                    _ => throw new ArgumentOutOfRangeException(nameof(origin))
                };
                return _position;
            }

            /// <summary>No-op for read-only fixtures.</summary>
            public override void Flush() { }

            /// <summary>Rejects writes.</summary>
            /// <param name="buffer">The source.</param>
            /// <param name="offset">The source offset.</param>
            /// <param name="count">The source count.</param>
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            /// <summary>Rejects resizing.</summary>
            /// <param name="value">The requested length.</param>
            public override void SetLength(long value) => throw new NotSupportedException();

            /// <summary>Tracks disposal of the actual caller-owned input.</summary>
            /// <param name="disposing">Whether explicitly disposing.</param>
            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        /// <summary>Supplies initial bytes, then blocks cooperatively or raises an independently released source failure.</summary>
        private sealed class BlockingUploadStream : UploadStream
        {
            /// <summary>Whether the initial data has been returned.</summary>
            private bool _started;

            /// <summary>Initializes a nonseekable blocking caller-owned source.</summary>
            internal BlockingUploadStream() : base(new byte[81920], false) { }

            /// <summary>Gets the signal that a pending read is awaiting more data.</summary>
            internal TaskCompletionSource Waiting { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            /// <summary>Gets the test-only release for an independent source error.</summary>
            internal TaskCompletionSource Release { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            /// <summary>Gets the signal that the independent source failure has been raised.</summary>
            internal TaskCompletionSource Faulted { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            /// <summary>Gets whether curl exit cancellation reached the pending source read.</summary>
            internal bool CancellationObserved { get; private set; }

            /// <summary>Returns initial bytes, then waits while respecting the exact supplied cancellation token.</summary>
            /// <param name="buffer">The destination buffer.</param>
            /// <param name="cancellationToken">The serializer cancellation token.</param>
            /// <returns>The initial bytes, followed by cancellation or an independent source exception.</returns>
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (!_started)
                {
                    _started = true;
                    return await base.ReadAsync(buffer, cancellationToken);
                }
                Waiting.TrySetResult();
                try
                {
                    await Release.Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    CancellationObserved = true;
                    throw;
                }
                Faulted.TrySetResult();
                throw new IOException("Actual independent source failure");
            }
        }
    }
}
