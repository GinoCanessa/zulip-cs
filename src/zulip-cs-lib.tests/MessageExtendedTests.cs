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
    /// <summary>Tests for new Messages methods added in Phase 1 and Phase 4.</summary>
    public class MessageExtendedTests
    {
        /// <summary>A returned object cannot invent the message identity absent from the wire.</summary>
        [Fact]
        public async Task Messages_ReadsRequireNestedIdentities()
        {
            const string json = "{\"result\":\"success\",\"message\":{},\"messages\":[{}]}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json, json, json);
            (bool success, string? details, MessageObject? message, string? rawContent) single = await client.Messages.TryGetSingle(1);
            Assert.False(single.success);
            Assert.Null(single.message);
            Assert.Contains("'id'", single.details);
            Assert.False((await client.Messages.TryGet(Messages.GetAnchorMode.Newest)).success);
            await Assert.ThrowsAsync<Exception>(() => client.Messages.GetSingle(1));
            await Assert.ThrowsAsync<Exception>(() => client.Messages.Get(Messages.GetAnchorMode.Newest));
        }

        [Fact]
        public async Task Message_SendStream_ByName_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"id\":42}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TrySendStream("Hello stream!", "topic1", "general");
            Assert.True(actual.success, actual.details);
            Assert.Equal((ulong)42, actual.messageId);
        }

        [Fact]
        public async Task Message_SendStream_ById_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"id\":43}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TrySendStream("Hello stream!", "topic1", 1);
            Assert.True(actual.success, actual.details);
            Assert.Equal((ulong)43, actual.messageId);
        }

        [Fact]
        public async Task Message_Delete_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryDelete(100);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Message_Edit_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryEdit(100, content: "Updated content");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Message_AddEmoji_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryAddEmoji(100, "thumbs_up");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Message_RemoveEmoji_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryRemoveEmoji(100, "thumbs_up");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Message_Get_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"messages\":[{\"id\":1,\"sender_id\":10,\"content\":\"Hello\",\"subject\":\"topic1\",\"type\":\"stream\"}],\"found_newest\":true,\"found_oldest\":false}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryGet(
                Messages.GetAnchorMode.Newest,
                numBefore: 5,
                numAfter: 0);

            Assert.True(actual.success, actual.details);
            Assert.Single(actual.messages!);
            Assert.True(actual.foundNewest);
        }

        [Fact]
        public async Task Message_Get_WithNarrow_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"messages\":[],\"found_newest\":true,\"found_oldest\":true}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            Narrow[] narrow = new[]
            {
                new Narrow(Narrow.NarrowOperator.Channel, "general"),
                new Narrow(Narrow.NarrowOperator.Topic, "test")
            };

            var actual = await zulipClient.Messages.TryGet(
                Messages.GetAnchorMode.Newest,
                numBefore: 10,
                numAfter: 0,
                narrow: narrow);

            Assert.True(actual.success, actual.details);
            Assert.Empty(actual.messages!);
        }

        [Fact]
        public async Task Message_GetSingle_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"message\":{\"id\":42,\"sender_id\":10,\"content\":\"Single message\",\"subject\":\"test\",\"type\":\"stream\"},\"raw_content\":\"Single message\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryGetSingle(42);
            Assert.True(actual.success, actual.details);
            Assert.NotNull(actual.message);
            Assert.Equal((ulong)42, actual.message.Id);
            Assert.Equal("Single message", actual.rawContent);
        }

        [Fact]
        public async Task Message_Render_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"rendered\":\"<p><strong>bold</strong></p>\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryRender("**bold**");
            Assert.True(actual.success, actual.details);
            Assert.Contains("<strong>bold</strong>", actual.renderedHtml);
        }

        [Fact]
        public async Task Message_UpdateFlags_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryUpdateFlags(
                new ulong[] { 1, 2, 3 }, Messages.FlagOperation.Add, "starred");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Message_GetEditHistory_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"message_history\":[{\"user_id\":10,\"timestamp\":1700000000}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryGetEditHistory(42);
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.history!);
        }

        [Fact]
        public async Task Message_MarkAllAsRead_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryMarkAllAsRead();
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Message_MarkStreamAsRead_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryMarkStreamAsRead(1);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Message_MarkTopicAsRead_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryMarkTopicAsRead(1, "topic1");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Message_GetReadReceipts_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user_ids\":[10,20,30]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryGetReadReceipts(42);
            Assert.True(actual.success, actual.details);
            Assert.Equal(3, actual.userIds!.Count);
        }

        [Fact]
        public async Task Message_SendPrivate_Error()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"error\",\"msg\":\"Invalid email\",\"code\":\"BAD_REQUEST\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.BadRequest, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TrySendPrivate("message", "bad@example.org");
            Assert.False(actual.success);
            Assert.False(string.IsNullOrEmpty(actual.details));
        }

        [Fact]
        public async Task Message_Get_Error()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"error\",\"msg\":\"Invalid narrow\",\"code\":\"BAD_REQUEST\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.BadRequest, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Messages.TryGet(
                Messages.GetAnchorMode.Newest, numBefore: 5, numAfter: 0);
            Assert.False(actual.success);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("General,Team")]
        [InlineData("[1,2]")]
        [InlineData("{\"id\":42}")]
        [InlineData("null")]
        [InlineData("a \"quoted\" channel + \u96ea")]
        public async Task Message_SendStream_NamesRemainJsonStrings(string name)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\",\"id\":42}");
            (bool success, string? details, ulong messageId) actual =
                await client.Messages.TrySendStream("hello \"world\" \u00e9", "topic + test", name);
            Assert.True(actual.success, actual.details);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/api/v1/messages", request.RequestUri!.AbsoluteUri);
            Dictionary<string, string> fields = await Utils.ReadForm(request);
            using JsonDocument destination = JsonDocument.Parse(fields["to"]);
            Assert.Equal(JsonValueKind.String, destination.RootElement.ValueKind);
            Assert.Equal(name, destination.RootElement.GetString());
            Assert.Equal("channel", fields["type"]);
            Assert.Equal("topic + test", fields["topic"]);
            Assert.Equal("hello \"world\" \u00e9", fields["content"]);
        }

        [Fact]
        public async Task Message_SendStream_IdRemainsJsonNumber()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\",\"id\":42}");
            Assert.True((await client.Messages.TrySendStream("hello", "topic", 123)).success);
            Dictionary<string, string> fields = await Utils.ReadForm(Assert.Single(requests));
            using JsonDocument destination = JsonDocument.Parse(fields["to"]);
            Assert.Equal(JsonValueKind.Number, destination.RootElement.ValueKind);
            Assert.Equal(123, destination.RootElement.GetInt32());
        }

        [Fact]
        public async Task Message_SendStream_RejectsEmptyOrMultipleDestinationsBeforeSending()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            string[] noNames = [];
            int[] noIds = [];
            Assert.False((await client.Messages.TrySendStream("hello", "topic", noNames)).success);
            Assert.False((await client.Messages.TrySendStream("hello", "topic", noIds)).success);
            Assert.False((await client.Messages.TrySendStream("hello", "topic", "general", "random")).success);
            Assert.False((await client.Messages.TrySendStream("hello", "topic", 1, 2)).success);
            Assert.False((await client.Messages.TrySendStream("hello", "topic", " ")).success);
            await Assert.ThrowsAsync<Exception>(() => client.Messages.SendStream("hello", "topic", 1, 2));
            Assert.Empty(requests);
        }

        [Theory]
        [InlineData("{\"result\":\"success\"}", false)]
        [InlineData("{\"result\":\"success\"}", true)]
        [InlineData("{\"result\":\"success\",\"id\":\"42\"}", false)]
        public async Task Message_SendStream_RejectsMissingOrMalformedReturnedId(string json, bool useId)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json);
            (bool success, string? details, ulong messageId) actual = useId
                ? await client.Messages.TrySendStream("hello", "topic", 10)
                : await client.Messages.TrySendStream("hello", "topic", "general");
            Assert.False(actual.success);
            Assert.Contains("'id'", actual.details);
            Assert.Equal(0UL, actual.messageId);
            Assert.Single(requests);
        }

        [Fact]
        public async Task Message_MarkAllAsReadBatch_ContinuationIsCallerControlled()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"complete\":false,\"ignored_parameters_unsupported\":[\"future\"]}",
                "{\"result\":\"success\",\"complete\":true}");
            (bool success, string? details, BatchOperationResult? batch) first = await client.Messages.TryMarkAllAsReadBatch();
            Assert.True(first.success, first.details);
            Assert.False(first.batch!.Complete);
            Assert.Same(client.LastResponse, first.batch.Response);
            Assert.Equal(new string[] { "future" }, first.batch.Response.IgnoredParametersUnsupported);
            Assert.Single(requests);
            BatchOperationResult second = await client.Messages.MarkAllAsReadBatch();
            Assert.True(second.Complete);
            Assert.Equal(2, requests.Count);
            Assert.False(first.batch.Complete);
            Assert.Same(client.LastResponse, second.Response);
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/v1/mark_all_as_read", request.RequestUri!.AbsolutePath);
                Assert.Empty(await Utils.ReadForm(request));
            }
        }

        [Theory]
        [InlineData("{\"result\":\"success\"}", null)]
        [InlineData("{\"result\":\"partially_completed\"}", false)]
        public async Task Message_MarkAllAsReadBatch_LegacyCompletionStaysDistinguishable(string json, bool? complete)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);
            (bool success, string? details, BatchOperationResult? batch) batch = await client.Messages.TryMarkAllAsReadBatch();
            Assert.True(batch.success, batch.details);
            Assert.Equal(complete, batch.batch!.Complete);
            Assert.Single(requests);
            (bool success, string? details) convenience = await client.Messages.TryMarkAllAsRead();
            Assert.Equal(complete == null, convenience.success);
            Assert.Contains("TryMarkAllAsReadBatch", convenience.details);
            if (complete == null) Assert.Contains("completion", batch.details);
            Assert.Equal(2, requests.Count);
        }

        [Theory]
        [InlineData("{\"result\":\"error\",\"msg\":\"permission denied\",\"code\":\"BAD_REQUEST\"}", "permission denied")]
        [InlineData("{\"result\":\"success\",\"complete\":\"false\"}", "complete")]
        public async Task Message_MarkAllAsReadBatch_RejectsRequestErrorsAndMalformedCompletion(string json, string error)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json);
            (bool success, string? details, BatchOperationResult? batch) actual = await client.Messages.TryMarkAllAsReadBatch();
            Assert.False(actual.success);
            Assert.Null(actual.batch);
            Assert.Contains(error, actual.details);
            Assert.Single(requests);
        }

        [Fact]
        public async Task Message_MarkAllAsRead_RejectsKnownPartialWorkWithoutRetrying()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"complete\":false}", "{\"result\":\"success\",\"complete\":false}");
            (bool success, string? details) actual = await client.Messages.TryMarkAllAsRead();
            Assert.False(actual.success);
            Assert.Contains("TryMarkAllAsReadBatch", actual.details);
            Assert.Single(requests);
            Exception exception = await Assert.ThrowsAsync<Exception>(() => client.Messages.MarkAllAsRead());
            Assert.Contains("incomplete", exception.Message);
            Assert.Equal(2, requests.Count);
        }

        [Theory]
        [InlineData("messages")]
        [InlineData("message")]
        [InlineData("message_history")]
        [InlineData("user_ids")]
        [InlineData("rendered")]
        public async Task Message_ReadsRejectMissingRequiredPayloads(string field)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}");
            (bool success, string? details) actual;
            switch (field)
            {
                case "messages":
                    (bool success, string? details, List<MessageObject>? messages, bool? foundNewest, bool? foundOldest) messages =
                        await client.Messages.TryGet(Messages.GetAnchorMode.Newest);
                    actual = (messages.success, messages.details);
                    Assert.Null(messages.messages);
                    break;
                case "message":
                    (bool success, string? details, MessageObject? message, string? rawContent) message =
                        await client.Messages.TryGetSingle(42);
                    actual = (message.success, message.details);
                    Assert.Null(message.message);
                    break;
                case "message_history":
                    (bool success, string? details, List<MessageHistoryObject>? history) history = await client.Messages.TryGetEditHistory(42);
                    actual = (history.success, history.details);
                    Assert.Null(history.history);
                    break;
                case "user_ids":
                    (bool success, string? details, List<int>? userIds) receipts = await client.Messages.TryGetReadReceipts(42);
                    actual = (receipts.success, receipts.details);
                    Assert.Null(receipts.userIds);
                    break;
                default:
                    (bool success, string? details, string? renderedHtml) rendered = await client.Messages.TryRender("hello");
                    actual = (rendered.success, rendered.details);
                    Assert.Null(rendered.renderedHtml);
                    break;
            }
            Assert.False(actual.success);
            Assert.Contains(field, actual.details);
        }

        [Fact]
        public async Task Message_ReadsAcceptLegitimateEmptyResults()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"messages\":[]}", "{\"result\":\"success\",\"message_history\":[]}",
                "{\"result\":\"success\",\"user_ids\":[]}", "{\"result\":\"success\",\"rendered\":\"\"}");
            Assert.Empty(await client.Messages.Get(Messages.GetAnchorMode.Newest));
            Assert.Empty(await client.Messages.GetEditHistory(42));
            Assert.Empty(await client.Messages.GetReadReceipts(42));
            Assert.Equal(string.Empty, await client.Messages.Render("hello"));
        }
    }
}
