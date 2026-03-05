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
    /// <summary>Tests for Events resource.</summary>
    public class EventTests
    {
        [Fact]
        public async Task Events_RegisterQueue_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"queue_id\":\"abc123:0\",\"last_event_id\":-1}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Events.TryRegisterQueue();
            Assert.True(actual.success, actual.details);
            Assert.Equal("abc123:0", actual.queueId);
            Assert.Equal(-1, actual.lastEventId);
        }

        [Fact]
        public async Task Events_RegisterQueue_WithEventTypes()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"queue_id\":\"def456:0\",\"last_event_id\":-1}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Events.TryRegisterQueue(eventTypes: "[\"message\"]");
            Assert.True(actual.success, actual.details);
            Assert.Equal("def456:0", actual.queueId);
        }

        [Fact]
        public async Task Events_GetEvents_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"events\":[{\"id\":0,\"type\":\"heartbeat\"},{\"id\":1,\"type\":\"message\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Events.TryGetEvents("abc123:0", -1, dontBlock: true);
            Assert.True(actual.success, actual.details);
            Assert.Equal(2, actual.events.Count);
        }

        [Fact]
        public async Task Events_DeleteQueue_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Events.TryDeleteQueue("abc123:0");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Events_RegisterQueue_Error()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"error\",\"msg\":\"Queue limit exceeded\",\"code\":\"BAD_REQUEST\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.BadRequest, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Events.TryRegisterQueue();
            Assert.False(actual.success);
            Assert.False(string.IsNullOrEmpty(actual.details));
        }

        [Fact]
        public async Task Events_GetEvents_Error()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"error\",\"msg\":\"Bad event queue id\",\"code\":\"BAD_EVENT_QUEUE_ID\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.BadRequest, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Events.TryGetEvents("invalid:0", -1);
            Assert.False(actual.success);
        }
    }
}
