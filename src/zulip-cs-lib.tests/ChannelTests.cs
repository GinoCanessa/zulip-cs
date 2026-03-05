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
    /// <summary>Tests for Channels resource.</summary>
    public class ChannelTests
    {
        [Fact]
        public async Task Channels_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"streams\":[{\"stream_id\":1,\"name\":\"general\"},{\"stream_id\":2,\"name\":\"random\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Equal(2, actual.streams.Count);
        }

        [Fact]
        public async Task Channels_GetById_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"stream\":{\"stream_id\":1,\"name\":\"general\"}}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryGetById(1);
            Assert.True(actual.success, actual.details);
            Assert.NotNull(actual.stream);
            Assert.Equal(1, actual.stream.StreamId);
        }

        [Fact]
        public async Task Channels_GetIdByName_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"stream_id\":42}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryGetIdByName("general");
            Assert.True(actual.success, actual.details);
            Assert.Equal(42, actual.streamId);
        }

        [Fact]
        public async Task Channels_GetSubscriptions_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"subscriptions\":[{\"stream_id\":1,\"name\":\"general\",\"color\":\"#c2c2c2\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryGetSubscriptions();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.subscriptions);
        }

        [Fact]
        public async Task Channels_Subscribe_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TrySubscribe("[{\"name\":\"new-stream\"}]");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Channels_Unsubscribe_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryUnsubscribe(new string[] { "general" });
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Channels_GetTopics_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"topics\":[{\"max_id\":100,\"name\":\"topic1\"},{\"max_id\":200,\"name\":\"topic2\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryGetTopics(1);
            Assert.True(actual.success, actual.details);
            Assert.Equal(2, actual.topics.Count);
        }

        [Fact]
        public async Task Channels_GetSubscribers_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"subscribers\":[1,2,3]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryGetSubscribers(1);
            Assert.True(actual.success, actual.details);
            Assert.Equal(3, actual.subscribers.Count);
        }

        [Fact]
        public async Task Channels_Update_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryUpdate(1, description: "New description");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Channels_Archive_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryArchive(1);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Channels_GetEmailAddress_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"email_address\":\"stream@zulip.example.org\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryGetEmailAddress(1);
            Assert.True(actual.success, actual.details);
            Assert.Equal("stream@zulip.example.org", actual.email);
        }

        [Fact]
        public async Task Channels_DeleteTopic_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryDeleteTopic(1, "old-topic");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Channels_AddDefaultChannel_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryAddDefaultChannel(1);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Channels_UpdateTopicMuting_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryUpdateTopicMuting("general", "topic1", "add");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Channels_GetAll_Error()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"error\",\"msg\":\"Not authorized\",\"code\":\"UNAUTHORIZED\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.Unauthorized, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Channels.TryGetAll();
            Assert.False(actual.success);
            Assert.False(string.IsNullOrEmpty(actual.details));
        }
    }
}
