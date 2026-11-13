using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Models;
using zulip_cs_lib.Resources;

namespace zulip_set_lib.tests
{
    /// <summary>A send message tests.</summary>
    public class MessageTests
    {
        /// <summary>Resource results never share mutable message state with diagnostics or later reads.</summary>
        [Fact]
        public async Task Message_Get_PreservesIndependentDiagnosticsAndRepeatedReads()
        {
            const string json = "{\"result\":\"success\",\"messages\":[{\"id\":1,\"content\":\"original\"}]}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json)!;
            Messages resource = new Messages((method, url, data) => Task.FromResult(response));
            (bool success, string? details, List<MessageObject>? messages, bool? foundNewest, bool? foundOldest) first =
                await resource.TryGet(Messages.GetAnchorMode.Newest);
            Assert.True(first.success, first.details);
            MessageObject returned = Assert.Single(first.messages!);
            returned.Content = "changed";
            MessageObject diagnostic = Assert.Single(response.Messages!);
            Assert.Equal("original", diagnostic.Content);
            Assert.NotSame(returned, diagnostic);
            List<MessageObject> next = await resource.Get(Messages.GetAnchorMode.Newest);
            Assert.Equal("original", Assert.Single(next).Content);
            Assert.NotSame(first.messages, next);
        }

        /// <summary>Observed or explicitly reassigned typed messages cannot override the authoritative raw payload.</summary>
        [Fact]
        public async Task Message_Get_ObservedTypedMutationDoesNotOverrideRaw()
        {
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(
                "{\"result\":\"success\",\"messages\":[{\"id\":1,\"content\":\"original\"}]}")!;
            response.Messages![0].Content = "changed";
            Messages resource = new Messages((method, url, data) => Task.FromResult(response));
            Assert.Equal("original", Assert.Single(await resource.Get(Messages.GetAnchorMode.Newest)).Content);
            response.Messages = [new MessageObject { Id = 2, Content = "assigned" }];
            Assert.Equal((ulong)1, Assert.Single(await resource.Get(Messages.GetAnchorMode.Newest)).Id);
            Assert.Equal("assigned", Assert.Single(response.Messages).Content);
        }

        /// <summary>Replacing raw diagnostics invalidates any pre-parsed shortcut.</summary>
        [Fact]
        public async Task Message_Get_ReplacedRawPayloadIsAuthoritative()
        {
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(
                "{\"result\":\"success\",\"messages\":[{\"id\":1}]}")!;
            response.RawPayload = JsonSerializer.SerializeToElement(new { result = "success", messages = new[] { new { id = 2 } } });
            Messages resource = new Messages((method, url, data) => Task.FromResult(response));
            Assert.Equal((ulong)2, Assert.Single(await resource.Get(Messages.GetAnchorMode.Newest)).Id);
        }

        /// <summary>Custom envelope options do not change the existing default-options resource contract.</summary>
        [Fact]
        public async Task Message_Get_CustomOptionsRetainRawDeserializationSemantics()
        {
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(
                "{\"result\":\"success\",\"messages\":[{\"id\":1,\"CONTENT\":\"custom\"}]}", options)!;
            Messages resource = new Messages((method, url, data) => Task.FromResult(response));
            MessageObject result = Assert.Single(await resource.Get(Messages.GetAnchorMode.Newest));
            Assert.Null(result.Content);
            Assert.Equal("custom", result.AdditionalProperties["CONTENT"].GetString());
            Assert.Equal("custom", Assert.Single(response.Messages!).Content);
        }

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
