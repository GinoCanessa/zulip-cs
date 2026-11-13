using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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

    }
}
