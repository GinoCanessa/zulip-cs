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
    /// <summary>A send message tests.</summary>
    public class MessageTests
    {
        [Fact]
        public async Task Message_SendPrivate_EmailSingle()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            Dictionary<string, dynamic> mockResponse = new Dictionary<string, dynamic>()
            {
                { "result", "success" },
                { "msg", string.Empty },
                { "id", 1 },
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            (bool success, string? details, ulong messageId) actual = await zulipClient.Messages.TrySendPrivate(
                "message", 
                "user1@example.org");

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.True(actual.success, $"TrySendPrivate failed: {actual.details}");
            Assert.Equal((ulong)mockResponse["id"], actual.messageId);
        }

        [Fact]
        public async Task Message_SendPrivate_EmailMultiple()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            Dictionary<string, dynamic> mockResponse = new Dictionary<string, dynamic>()
            {
                { "result", "success" },
                { "msg", string.Empty },
                { "id", 1 },
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            (bool success, string? details, ulong messageId) actual = await zulipClient.Messages.TrySendPrivate(
                "message",
                "user1@example.org",
                "user2@example.org");

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.True(actual.success, $"TrySendPrivate failed: {actual.details}");
            Assert.Equal((ulong)mockResponse["id"], actual.messageId);
        }

        [Fact]
        public async Task Message_SendPrivate_IdSingle()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            Dictionary<string, dynamic> mockResponse = new Dictionary<string, dynamic>()
            {
                { "result", "success" },
                { "msg", string.Empty },
                { "id", 1 },
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            (bool success, string? details, ulong messageId) actual = await zulipClient.Messages.TrySendPrivate(
                "message",
                1);

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.True(actual.success, $"TrySendPrivate failed: {actual.details}");
            Assert.Equal((ulong)mockResponse["id"], actual.messageId);
        }

        [Fact]
        public async Task Message_SendPrivate_IdMultiple()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            Dictionary<string, dynamic> mockResponse = new Dictionary<string, dynamic>()
            {
                { "result", "success" },
                { "msg", string.Empty },
                { "id", 1 },
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            (bool success, string? details, ulong messageId) actual = await zulipClient.Messages.TrySendPrivate(
                "message",
                1,
                2);

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.True(actual.success, $"TrySendPrivate failed: {actual.details}");
            Assert.Equal((ulong)mockResponse["id"], actual.messageId);
        }

        [Fact]
        public async Task Message_SendPrivate_InvalidEmail()
        {
            HttpStatusCode code = HttpStatusCode.OK;
            Dictionary<string, dynamic> mockResponse = new Dictionary<string, dynamic>()
            {
                { "result", "error" },
                { "msg", "Invalid email 'invalid@example.org'" },
                { "code", "BAD_REQUEST" },
            };

            bool success = Utils.TryGetMockedClient(
                code,
                Utils.ContentForResponse(mockResponse),
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            (bool success, string? details, ulong messageId) actual = await zulipClient.Messages.TrySendPrivate(
                "message",
                "invalid@example.org");

            mockMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());

            Assert.False(actual.success, $"TrySendPrivate succeeded on invalid email!");
            Assert.False(string.IsNullOrEmpty(actual.details), $"TrySendPrivate must contain details on errors!");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Message_SendPrivate_EmailFormsRemainCommaSeparated(bool separateArguments)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\",\"id\":31}");
            string[] recipients = separateArguments
                ? ["user1@example.org", "user2@example.org"]
                : ["user1@example.org, user2@example.org"];
            (bool success, string? details, ulong messageId) actual =
                await client.Messages.TrySendPrivate("hello", recipients);
            Assert.True(actual.success, actual.details);
            Assert.Equal(31UL, actual.messageId);
            HttpRequestMessage request = Assert.Single(requests);
            Dictionary<string, string> fields = await Utils.ReadForm(request);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/api/v1/messages", request.RequestUri!.AbsoluteUri);
            Assert.Equal("direct", fields["type"]);
            Assert.Equal("user1@example.org, user2@example.org", fields["to"]);
            Assert.Equal("hello", fields["content"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Message_SendPrivate_RequiresReturnedMessageId(bool useIds)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}");
            (bool success, string? details, ulong messageId) actual = useIds
                ? await client.Messages.TrySendPrivate("hello", 10)
                : await client.Messages.TrySendPrivate("hello", "user@example.org");
            Assert.False(actual.success);
            Assert.Contains("'id'", actual.details);
            Assert.Equal(0UL, actual.messageId);
            Assert.Single(requests);
        }
    }
}
