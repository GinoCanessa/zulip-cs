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
using zulip_cs_lib.Resources;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for new Messages methods added in Phase 1 and Phase 4.</summary>
    public class MessageExtendedTests
    {
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
            Assert.Single(actual.messages);
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
            Assert.Empty(actual.messages);
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
            Assert.Single(actual.history);
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
            Assert.Equal(3, actual.userIds.Count);
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
    }
}
