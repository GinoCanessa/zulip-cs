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
using zulip_cs_lib.Models;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for Channels resource.</summary>
    public class ChannelTests
    {
        /// <summary>Missing channel identities fail on details and collections, including throwing projections.</summary>
        [Fact]
        public async Task Channels_ReadsRequireNestedIdentities()
        {
            const string json = "{\"result\":\"success\",\"stream\":{},\"streams\":[{}],\"subscriptions\":[{}]}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json, json, json, json, json);
            (bool success, string? details, StreamObject? stream) single = await client.Channels.TryGetById(1);
            Assert.False(single.success);
            Assert.Null(single.stream);
            Assert.Contains("stream_id", single.details);
            Assert.False((await client.Channels.TryGetAll()).success);
            Assert.False((await client.Channels.TryGetSubscriptions()).success);
            await Assert.ThrowsAsync<Exception>(() => client.Channels.GetById(1));
            await Assert.ThrowsAsync<Exception>(() => client.Channels.GetAll());
            await Assert.ThrowsAsync<Exception>(() => client.Channels.GetSubscriptions());
        }

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
            Assert.Equal(2, actual.streams!.Count);
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
            Assert.Single(actual.subscriptions!);
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
            Assert.Equal(2, actual.topics!.Count);
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
            Assert.Equal(3, actual.subscribers!.Count);
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

        [Fact]
        public async Task Channels_DeleteTopicBatch_ContinuationIsCallerControlled()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"complete\":false}", "{\"result\":\"success\",\"complete\":true}");
            const string topic = "old \"topic\" + \u96ea";
            (bool success, string? details, BatchOperationResult? batch) first = await client.Channels.TryDeleteTopicBatch(42, topic);
            Assert.True(first.success, first.details);
            Assert.False(first.batch!.Complete);
            Assert.Same(client.LastResponse, first.batch.Response);
            Assert.Single(requests);

            BatchOperationResult second = await client.Channels.DeleteTopicBatch(42, topic);
            Assert.True(second.Complete);
            Assert.Equal(2, requests.Count);
            Assert.False(first.batch.Complete);
            Assert.Same(client.LastResponse, second.Response);
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/v1/streams/42/delete_topic", request.RequestUri!.AbsolutePath);
                Assert.Equal(new Dictionary<string, string> { { "topic_name", topic } }, await Utils.ReadForm(request));
            }
        }

        [Theory]
        [InlineData("{\"result\":\"success\"}", null)]
        [InlineData("{\"result\":\"partially_completed\"}", false)]
        public async Task Channels_DeleteTopicBatch_LegacyCompletionStaysDistinguishable(string json, bool? complete)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);
            (bool success, string? details, BatchOperationResult? batch) actual = await client.Channels.TryDeleteTopicBatch(42, "old-topic");
            Assert.True(actual.success, actual.details);
            Assert.Equal(complete, actual.batch!.Complete);
            Assert.Single(requests);
            (bool success, string? details) convenience = await client.Channels.TryDeleteTopic(42, "old-topic");
            Assert.Equal(complete == null, convenience.success);
            Assert.Contains("TryDeleteTopicBatch", convenience.details);
            if (complete == null) Assert.Contains("completion", actual.details);
            Assert.Equal(2, requests.Count);
        }

        [Theory]
        [InlineData("{\"result\":\"error\",\"msg\":\"permission denied\",\"code\":\"BAD_REQUEST\"}", "permission denied")]
        [InlineData("{\"result\":\"success\",\"complete\":null}", "complete")]
        public async Task Channels_DeleteTopicBatch_RejectsRequestErrorsAndMalformedCompletion(string json, string error)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json);
            (bool success, string? details, BatchOperationResult? batch) actual = await client.Channels.TryDeleteTopicBatch(42, "old-topic");
            Assert.False(actual.success);
            Assert.Null(actual.batch);
            Assert.Contains(error, actual.details);
            Assert.Single(requests);
        }

        [Fact]
        public async Task Channels_DeleteTopic_RejectsKnownPartialWorkWithoutRetrying()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"complete\":false}", "{\"result\":\"success\",\"complete\":false}");
            (bool success, string? details) actual = await client.Channels.TryDeleteTopic(42, "old-topic");
            Assert.False(actual.success);
            Assert.Contains("TryDeleteTopicBatch", actual.details);
            Assert.Single(requests);
            Exception exception = await Assert.ThrowsAsync<Exception>(() => client.Channels.DeleteTopic(42, "old-topic"));
            Assert.Contains("incomplete", exception.Message);
            Assert.Equal(2, requests.Count);
        }

        [Theory]
        [InlineData("streams")]
        [InlineData("stream")]
        [InlineData("stream_id")]
        [InlineData("subscriptions")]
        [InlineData("topics")]
        [InlineData("subscribers")]
        [InlineData("email_address")]
        public async Task Channels_ReadsRejectMissingRequiredPayloads(string field)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}");
            (bool success, string? details) actual;
            switch (field)
            {
                case "streams":
                    (bool success, string? details, List<StreamObject>? streams) streams = await client.Channels.TryGetAll();
                    actual = (streams.success, streams.details);
                    break;
                case "stream":
                    (bool success, string? details, StreamObject? stream) stream = await client.Channels.TryGetById(42);
                    actual = (stream.success, stream.details);
                    break;
                case "stream_id":
                    (bool success, string? details, int streamId) id = await client.Channels.TryGetIdByName("general");
                    actual = (id.success, id.details);
                    break;
                case "subscriptions":
                    (bool success, string? details, List<SubscriptionObject>? subscriptions) subscriptions = await client.Channels.TryGetSubscriptions();
                    actual = (subscriptions.success, subscriptions.details);
                    break;
                case "topics":
                    (bool success, string? details, List<TopicObject>? topics) topics = await client.Channels.TryGetTopics(42);
                    actual = (topics.success, topics.details);
                    break;
                case "subscribers":
                    (bool success, string? details, List<int>? subscribers) subscribers = await client.Channels.TryGetSubscribers(42);
                    actual = (subscribers.success, subscribers.details);
                    break;
                default:
                    (bool success, string? details, string? email) email = await client.Channels.TryGetEmailAddress(42);
                    actual = (email.success, email.details);
                    break;
            }
            Assert.False(actual.success);
            Assert.Contains(field, actual.details);
        }

        [Fact]
        public async Task Channels_ReadsAcceptLegitimateEmptyCollections()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"streams\":[]}", "{\"result\":\"success\",\"subscriptions\":[]}",
                "{\"result\":\"success\",\"topics\":[]}", "{\"result\":\"success\",\"subscribers\":[]}");
            Assert.Empty(await client.Channels.GetAll());
            Assert.Empty(await client.Channels.GetSubscriptions());
            Assert.Empty(await client.Channels.GetTopics(42));
            Assert.Empty(await client.Channels.GetSubscribers(42));
        }
    }
}
