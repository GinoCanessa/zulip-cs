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

namespace zulip_set_lib.tests
{
    /// <summary>Tests for Channels resource.</summary>
    public class ChannelTests
    {
        /// <summary>Creates a channel with real form settings and the live endpoint's id response.</summary>
        [Fact]
        public async Task Channels_CreateWithId_ActualContractAndDefaults()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"id\":50}", "{\"result\":\"success\",\"id\":51}");
            ChannelCreationOptions options = new ChannelCreationOptions
            {
                Description = "Music + \"雪\"",
                Announce = false,
                InviteOnly = true,
                IsWebPublic = false,
                IsDefaultStream = true,
                HistoryPublicToSubscribers = false,
                TopicsPolicy = "allow_empty_topic",
                MessageRetentionDays = "realm_default",
                CanAdministerChannelGroup = new GroupSettingValue(11),
                CanCreateTopicGroup = new GroupSettingValue([12], [15]),
                DefaultPushNotifications = false
            };

            (bool success, string? details, int streamId) actual = await client.Channels.TryCreateWithId("Music + 雪", [12, 17], options);
            Assert.True(actual.success, actual.details);
            Assert.Equal(50, actual.streamId);
            Assert.Single(requests);
            Assert.Equal(HttpMethod.Post, requests[0].Method);
            Assert.Equal("/api/v1/channels/create", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal(new Dictionary<string, string>
            {
                { "name", "Music + 雪" }, { "subscribers", "[12,17]" },
                { "description", "Music + \"雪\"" }, { "announce", "false" },
                { "invite_only", "true" }, { "is_web_public", "false" }, { "is_default_stream", "true" },
                { "history_public_to_subscribers", "false" }, { "topics_policy", "allow_empty_topic" },
                { "message_retention_days", "\"realm_default\"" }, { "can_administer_channel_group", "11" },
                { "can_create_topic_group", "{\"direct_members\":[12],\"direct_subgroups\":[15]}" },
                { "default_push_notifications", "false" }
            }, await Utils.ReadForm(requests[0]));

            Assert.Equal(51, await client.Channels.CreateWithId("Empty channel", []));
            Assert.Equal(new Dictionary<string, string> { { "name", "Empty channel" }, { "subscribers", "[]" } },
                await Utils.ReadForm(requests[1]));
        }

        /// <summary>Creation requires the real integer id field and propagates server failures.</summary>
        /// <param name="json">The response payload.</param>
        /// <param name="error">The expected diagnostic.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}", "id")]
        [InlineData("{\"result\":\"success\",\"stream_id\":50}", "id")]
        [InlineData("{\"result\":\"success\",\"id\":null}", "id")]
        [InlineData("{\"result\":\"success\",\"id\":\"50\"}", "id")]
        [InlineData("{\"result\":\"success\",\"id\":1.5}", "id")]
        [InlineData("{\"result\":\"error\",\"msg\":\"Channel already exists\",\"code\":\"CHANNEL_ALREADY_EXISTS\"}", "Channel already exists")]
        public async Task Channels_CreateWithId_RequiresIdentityAndThrows(string json, string error)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);
            (bool success, string? details, int streamId) actual = await client.Channels.TryCreateWithId("Music", []);
            Assert.False(actual.success);
            Assert.Contains(error, actual.details);
            Assert.Equal(0, actual.streamId);
            Exception exception = await Assert.ThrowsAsync<Exception>(() => client.Channels.CreateWithId("Music", []));
            Assert.Contains(error, exception.Message);
            Assert.Equal(2, requests.Count);
        }

        /// <summary>All personal topic policies use the modern endpoint, including a legitimate empty topic.</summary>
        /// <param name="policy">The selected personal policy.</param>
        /// <param name="expected">Its integer wire value.</param>
        [Theory]
        [InlineData(TopicVisibilityPolicy.None, "0")]
        [InlineData(TopicVisibilityPolicy.Muted, "1")]
        [InlineData(TopicVisibilityPolicy.Unmuted, "2")]
        [InlineData(TopicVisibilityPolicy.Followed, "3")]
        public async Task Channels_UpdateTopicVisibility_ActualPolicies(TopicVisibilityPolicy policy, string expected)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            const string topic = "topic + \"雪\"";
            Assert.True((await client.Channels.TryUpdateTopicVisibility(42, topic, policy)).success);
            await client.Channels.UpdateTopicVisibility(42, "", policy);
            Assert.Equal(2, requests.Count);
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/v1/user_topics", request.RequestUri!.AbsolutePath);
            }
            Assert.Equal(new Dictionary<string, string>
                { { "stream_id", "42" }, { "topic", topic }, { "visibility_policy", expected } },
                await Utils.ReadForm(requests[0]));
            Assert.Equal("", (await Utils.ReadForm(requests[1]))["topic"]);
        }

        /// <summary>The actual subscription-status boolean is required, and false remains successful.</summary>
        /// <param name="isSubscribed">The actual server status.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Channels_SubscriptionStatus_ActualBoolean(bool isSubscribed)
        {
            List<HttpRequestMessage> requests = [];
            string json = "{\"result\":\"success\",\"is_subscribed\":" + (isSubscribed ? "true" : "false") + "}";
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);
            (bool success, string? details, bool isSubscribed) actual = await client.Channels.TryGetSubscriptionStatus(12, 42);
            Assert.True(actual.success, actual.details);
            Assert.Equal(isSubscribed, actual.isSubscribed);
            Assert.Equal(isSubscribed, await client.Channels.GetSubscriptionStatus(12, 42));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v1/users/12/subscriptions/42", request.RequestUri!.AbsolutePath);
                Assert.Equal("", request.RequestUri.Query);
            }
        }

        /// <summary>Wrong or missing subscription-status fields cannot become a successful false.</summary>
        /// <param name="fields">The malformed status fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"is_subscribed\":null")]
        [InlineData(",\"is_subscribed\":\"false\"")]
        [InlineData(",\"is_subscribed\":0")]
        [InlineData(",\"subscribed\":false")]
        public async Task Channels_SubscriptionStatus_RejectsMalformedPayload(string fields)
        {
            string json = "{\"result\":\"success\"" + fields + "}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details, bool isSubscribed) actual = await client.Channels.TryGetSubscriptionStatus(12, 42);
            Assert.False(actual.success);
            Assert.Contains("is_subscribed", actual.details);
            await Assert.ThrowsAsync<Exception>(() => client.Channels.GetSubscriptionStatus(12, 42));
        }

        /// <summary>Bulk updates serialize real booleans and color strings; single updates use PATCH and unquoted form colors.</summary>
        [Fact]
        public async Task Channels_SubscriptionSettings_BulkAndSingleActualForms()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\"}", "{\"result\":\"success\"}", "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            SubscriptionPropertyChange[] changes =
            [
                new SubscriptionPropertyChange(42, SubscriptionProperty.IsMuted, false),
                new SubscriptionPropertyChange(43, SubscriptionProperty.PinToTop, true),
                new SubscriptionPropertyChange(44, "#f00f00"),
                new SubscriptionPropertyChange(45, SubscriptionProperty.WildcardMentionsNotify, false)
            ];
            Assert.True((await client.Channels.TryUpdateSubscriptionSettings(changes)).success);
            await client.Channels.UpdateSubscriptionSettings([]);
            await client.Channels.UpdateSubscriptionProperty(changes[2]);
            Assert.True((await client.Channels.TryUpdateSubscriptionProperty(changes[0])).success);
            Assert.Equal(4, requests.Count);
            Assert.Equal(HttpMethod.Post, requests[0].Method);
            Assert.Equal("/api/v1/users/me/subscriptions/properties", requests[0].RequestUri!.AbsolutePath);
            Dictionary<string, string> form = await Utils.ReadForm(requests[0]);
            Assert.Single(form);
            using JsonDocument document = JsonDocument.Parse(form["subscription_data"]);
            Assert.Equal(4, document.RootElement.GetArrayLength());
            Assert.Equal(42, document.RootElement[0].GetProperty("stream_id").GetInt32());
            Assert.Equal("is_muted", document.RootElement[0].GetProperty("property").GetString());
            Assert.False(document.RootElement[0].GetProperty("value").GetBoolean());
            Assert.True(document.RootElement[1].GetProperty("value").GetBoolean());
            Assert.Equal("#f00f00", document.RootElement[2].GetProperty("value").GetString());
            Assert.Equal("wildcard_mentions_notify", document.RootElement[3].GetProperty("property").GetString());
            Assert.False(document.RootElement[3].GetProperty("value").GetBoolean());
            Assert.Equal("[]", (await Utils.ReadForm(requests[1]))["subscription_data"]);
            Assert.Equal(HttpMethod.Patch, requests[2].Method);
            Assert.Equal("/api/v1/users/me/subscriptions/44", requests[2].RequestUri!.AbsolutePath);
            Assert.Equal(new Dictionary<string, string> { { "property", "color" }, { "value", "#f00f00" } },
                await Utils.ReadForm(requests[2]));
            Assert.Equal(new Dictionary<string, string> { { "property", "is_muted" }, { "value", "false" } },
                await Utils.ReadForm(requests[3]));
        }

        /// <summary>Invalid IDs, required inputs, options and typed property/value mismatches do not request.</summary>
        [Fact]
        public async Task Channels_NewInputs_RejectInvalidWithoutRequests()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            Assert.False((await client.Channels.TryCreateWithId("", [])).success);
            Assert.False((await client.Channels.TryCreateWithId("Music", null!)).success);
            Assert.False((await client.Channels.TryCreateWithId("Music", [-1])).success);
            Assert.False((await client.Channels.TryCreateWithId("Music", [], new ChannelCreationOptions { TopicsPolicy = "invalid" })).success);
            Assert.False((await client.Channels.TryCreateWithId("Music", [], new ChannelCreationOptions { MessageRetentionDays = "-1" })).success);
            Assert.False((await client.Channels.TryCreateWithId("Music", [], new ChannelCreationOptions { CanCreateTopicGroup = new GroupSettingValue(0) })).success);
            Assert.False((await client.Channels.TryUpdateTopicVisibility(42, "", (TopicVisibilityPolicy)4)).success);
            Assert.False((await client.Channels.TryUpdateTopicVisibility(0, "", TopicVisibilityPolicy.None)).success);
            Assert.False((await client.Channels.TryUpdateTopicVisibility(42, null!, TopicVisibilityPolicy.None)).success);
            Assert.False((await client.Channels.TryGetSubscriptionStatus(0, 42)).success);
            Assert.False((await client.Channels.TryGetSubscriptionStatus(12, 0)).success);
            Assert.False((await client.Channels.TryUpdateSubscriptionSettings(null!)).success);
            Assert.False((await client.Channels.TryUpdateSubscriptionSettings([null!])).success);
            Assert.False((await client.Channels.TryUpdateSubscriptionSettings(
                [new SubscriptionPropertyChange(42, SubscriptionProperty.IsMuted, true),
                 new SubscriptionPropertyChange(0, SubscriptionProperty.IsMuted, false)])).success);
            Assert.False((await client.Channels.TryUpdateSubscriptionProperty(new SubscriptionPropertyChange(42, (SubscriptionProperty)999, false))).success);
            Assert.False((await client.Channels.TryUpdateSubscriptionProperty(new SubscriptionPropertyChange(42, SubscriptionProperty.Color, true))).success);
            Assert.False((await client.Channels.TryUpdateSubscriptionProperty(new SubscriptionPropertyChange(42, ""))).success);
            await Assert.ThrowsAsync<Exception>(() => client.Channels.CreateWithId("Music", [-1]));
            await Assert.ThrowsAsync<Exception>(() => client.Channels.UpdateTopicVisibility(42, "", (TopicVisibilityPolicy)(-1)));
            await Assert.ThrowsAsync<Exception>(() => client.Channels.UpdateSubscriptionProperty(null!));
            Assert.Empty(requests);
        }

        /// <summary>New channel methods retain server errors and never retry automatically.</summary>
        [Fact]
        public async Task Channels_NewMethods_ServerErrorsPropagate()
        {
            const string json = "{\"result\":\"error\",\"msg\":\"permission denied\",\"code\":\"BAD_REQUEST\"}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json, json, json, json, json, json, json);
            Assert.Contains("permission denied", (await client.Channels.TryUpdateTopicVisibility(42, "", TopicVisibilityPolicy.None)).details);
            Assert.Contains("permission denied", (await client.Channels.TryGetSubscriptionStatus(12, 42)).details);
            Assert.Contains("permission denied", (await client.Channels.TryUpdateSubscriptionSettings([])).details);
            SubscriptionPropertyChange change = new SubscriptionPropertyChange(42, SubscriptionProperty.IsMuted, false);
            Assert.Contains("permission denied", (await client.Channels.TryUpdateSubscriptionProperty(change)).details);
            await Assert.ThrowsAsync<Exception>(() => client.Channels.UpdateTopicVisibility(42, "", TopicVisibilityPolicy.None));
            await Assert.ThrowsAsync<Exception>(() => client.Channels.GetSubscriptionStatus(12, 42));
            await Assert.ThrowsAsync<Exception>(() => client.Channels.UpdateSubscriptionSettings([]));
            await Assert.ThrowsAsync<Exception>(() => client.Channels.UpdateSubscriptionProperty(change));
        }

        /// <summary>Email lookup requires the documented email key and a nonempty string.</summary>
        /// <param name="fields">The invalid email fields.</param>
        [Theory]
        [InlineData("\"email_address\":\"stream@example.org\"")]
        [InlineData("\"email\":null")]
        [InlineData("\"email\":false")]
        [InlineData("\"email\":\"\"")]
        public async Task Channels_Email_RejectsWrongKeyAndMalformedPayload(string fields)
        {
            string json = "{\"result\":\"success\"," + fields + "}";
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);
            (bool success, string? details, string? email) actual = await client.Channels.TryGetEmailAddress(42);
            Assert.False(actual.success);
            Assert.Null(actual.email);
            Assert.Contains("email", actual.details);
            await Assert.ThrowsAsync<Exception>(() => client.Channels.GetEmailAddress(42));
            Assert.Equal(HttpMethod.Get, requests[0].Method);
            Assert.Equal("/api/v1/streams/42/email_address", requests[0].RequestUri!.AbsolutePath);
        }

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
                "{\"result\":\"success\",\"msg\":\"\",\"email\":\"stream@zulip.example.org\"}");

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
        [InlineData("email")]
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
