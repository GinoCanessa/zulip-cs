using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for ScheduledMessages resource.</summary>
    public class ScheduledMessageTests
    {
        [Fact]
        public async Task ScheduledMessages_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"scheduled_messages\":[{\"scheduled_message_id\":1,\"type\":\"direct\",\"to\":[10],\"content\":\"Hello\",\"scheduled_delivery_timestamp\":1700000000}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.ScheduledMessages.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.scheduledMessages);
            Assert.Equal(1, actual.scheduledMessages[0].ScheduledMessageId);
        }

        [Fact]
        public async Task ScheduledMessages_Create_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"scheduled_message_id\":42}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.ScheduledMessages.TryCreate("direct", "[10]", "Hello later", 1700000000);
            Assert.True(actual.success, actual.details);
            Assert.Equal(42, actual.scheduledMessageId);
        }

        [Fact]
        public async Task ScheduledMessages_Edit_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.ScheduledMessages.TryEdit(42, content: "Updated content");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task ScheduledMessages_Delete_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.ScheduledMessages.TryDelete(42);
            Assert.True(actual.success, actual.details);
        }
    }
}
