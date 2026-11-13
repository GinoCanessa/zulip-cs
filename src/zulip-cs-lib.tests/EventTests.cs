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
    /// <summary>Tests for Events resource.</summary>
    public class EventTests
    {
        /// <summary>A known message event must include its usable message payload.</summary>
        [Fact]
        public async Task Events_MessageEventRequiresPayload()
        {
            Events resource = CreateEvents("{\"result\":\"success\",\"events\":[{\"id\":1,\"type\":\"message\"}]}");
            (bool success, string? details, List<EventObject>? events) actual = await resource.TryGetEvents("queue", -1);
            Assert.False(actual.success);
            Assert.Null(actual.events);
            Assert.Contains("'message' object", actual.details);
            await Assert.ThrowsAsync<Exception>(() => resource.GetEvents("queue", -1));
        }

        /// <summary>Unknown kinds can reuse known field names without inheriting unrelated schemas.</summary>
        [Fact]
        public async Task Events_UnknownKindOwnsCollidingPayloadFields()
        {
            Events resource = CreateEvents("""
                {"result":"success","events":[{"id":4,"type":"future_kind",
                  "messages":{"items":[]},"flags":{"future":true},"op":{"kind":"future"},
                  "message":[null,1],"message_id":"future"}]}
                """);
            EventObject item = Assert.Single(await resource.GetEvents("queue", -1));
            Assert.Null(item.Messages);
            Assert.Null(item.Flags);
            Assert.Null(item.Message);
            Assert.Null(item.MessageId);
            Assert.Empty(item.ExtensionData!["messages"].GetProperty("items").EnumerateArray());
            Assert.True(item.ExtensionData["flags"].GetProperty("future").GetBoolean());
            Assert.Equal("future", item.ExtensionData["op"].GetProperty("kind").GetString());
            Assert.Equal(1, item.ExtensionData["message"][1].GetInt32());
            JsonElement roundTrip = JsonSerializer.SerializeToElement(item);
            Assert.Equal("future", roundTrip.GetProperty("message_id").GetString());
            Assert.True(roundTrip.GetProperty("flags").GetProperty("future").GetBoolean());
            Assert.Equal("future_kind", roundTrip.GetProperty("type").GetString());
        }

        /// <summary>Even supported kinds preserve fields outside their own typed contract.</summary>
        [Fact]
        public async Task Events_KnownKindRetainsUnrelatedFieldCollision()
        {
            Events resource = CreateEvents("""
                {"result":"success","events":[{"id":4,"type":"reaction","op":"add",
                  "message_id":31,"user_id":10,"messages":{"future":true}}]}
                """);
            EventObject item = Assert.Single(await resource.GetEvents("queue", -1));
            Assert.Equal(31, item.MessageId);
            Assert.Equal(10, item.UserId);
            Assert.Null(item.Messages);
            Assert.True(item.ExtensionData!["messages"].GetProperty("future").GetBoolean());
            Assert.True(JsonSerializer.SerializeToElement(item).GetProperty("messages").GetProperty("future").GetBoolean());
        }

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
                "{\"result\":\"success\",\"msg\":\"\",\"events\":[{\"id\":0,\"type\":\"heartbeat\"},{\"id\":1,\"type\":\"message\",\"message\":{\"id\":31}}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Events.TryGetEvents("abc123:0", -1, dontBlock: true);
            Assert.True(actual.success, actual.details);
            Assert.Equal(2, actual.events!.Count);
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

        /// <summary>Retains common message, edit, delete and flag data and owned future payloads.</summary>
        [Fact]
        public async Task Events_GetEvents_PopulatedCommonAndFutureEvents()
        {
            const string json = """
                {
                  "result":"success","events":[
                    {"id":0,"type":"message","flags":["read","mentioned"],"local_message_id":"local-你好",
                     "message":{"id":31,"sender_id":10,"content":"<p>Hello</p>","recipient_id":23,
                                "timestamp":1594825416,"client":"test","subject":"topic","type":"stream",
                                "stream_id":1,"sender_email":"a@example.org","sender_full_name":"A",
                                "display_recipient":"General","content_type":"text/html","avatar_url":null,
                                "future_message":{"nested":[1,"two"]}}},
                    {"id":2,"type":"update_message","user_id":10,"rendering_only":false,
                     "message_id":31,"message_ids":[31,32],"flags":["mentioned"],"edit_timestamp":1594825500,
                     "stream_id":1,"stream_name":"General","new_stream_id":2,"propagate_mode":"change_all",
                     "orig_subject":"topic","subject":"","orig_content":"Hello","content":"Changed",
                     "orig_rendered_content":"<p>Hello</p>","rendered_content":"<p>Changed</p>","is_me_message":false,
                     "topic_links":[{"text":"issue","url":"https://example.org/1"}]},
                    {"id":3,"type":"delete_message","message_ids":[31,32],"message_type":"stream","stream_id":2,"topic":""},
                    {"id":4,"type":"delete_message","message_id":33,"message_type":"private"},
                    {"id":6,"type":"update_message_flags","op":"add","operation":"add","flag":"read","messages":[],"all":true},
                    {"id":7,"type":"subscription","op":"peer_add","stream_ids":[1,2],"user_ids":[10,11]},
                    {"id":8,"type":"future_523_plus","user":{"variant":"object"},"value":[false,{"payload":42}],
                     "future_field":{"nested":[null,"future"]}},
                    {"id":9,"type":"reaction","op":"add","user_id":10,"message_id":31}
                  ]
                }
                """;
            Events resource = CreateEvents(json);
            List<EventObject> events = await resource.GetEvents("queue", -1, true);
            Assert.Equal(8, events.Count);

            EventObject message = events[0];
            Assert.Equal(0, message.Id);
            Assert.Equal("local-你好", message.LocalMessageId);
            Assert.Equal(new string[] { "read", "mentioned" }, message.Flags);
            Assert.Equal(31UL, message.Message!.Id);
            Assert.Equal(10, message.Message.SenderId);
            Assert.Equal("<p>Hello</p>", message.Message.Content);
            Assert.Equal("topic", message.Message.Subject);
            Assert.Equal(1, message.Message.StreamId);
            Assert.Equal(1594825416, message.Message.Timestamp);
            Assert.Equal("General", ((JsonElement)message.Message.DisplayRecipient!).GetString());
            Assert.Equal("two", message.RawMessage!.Value.GetProperty("future_message").GetProperty("nested")[1].GetString());

            EventObject update = events[1];
            Assert.Equal(31, update.MessageId);
            Assert.Equal(new int[] { 31, 32 }, update.MessageIds);
            Assert.Equal(10, update.UserId);
            Assert.False(update.RenderingOnly);
            Assert.Equal(1594825500, update.EditTimestamp);
            Assert.Equal(1, update.StreamId);
            Assert.Equal(2, update.NewStreamId);
            Assert.Equal("General", update.StreamName);
            Assert.Equal("change_all", update.PropagateMode);
            Assert.Equal("topic", update.OriginalSubject);
            Assert.Equal("", update.Subject);
            Assert.Equal("Hello", update.OriginalContent);
            Assert.Equal("Changed", update.Content);
            Assert.Equal("<p>Hello</p>", update.OriginalRenderedContent);
            Assert.Equal("<p>Changed</p>", update.RenderedContent);
            Assert.False(update.IsMeMessage);
            Assert.Equal(new string[] { "mentioned" }, update.Flags);
            Assert.Equal("issue", update.ExtensionData!["topic_links"][0].GetProperty("text").GetString());
            Assert.Equal(new int[] { 31, 32 }, events[2].MessageIds);
            Assert.Equal("stream", events[2].MessageType);
            Assert.Equal(2, events[2].StreamId);
            Assert.Equal("", events[2].Topic);
            Assert.Equal(33, events[3].MessageId);
            Assert.Equal("private", events[3].MessageType);
            Assert.Equal("add", events[4].Op);
            Assert.Equal("add", events[4].Operation);
            Assert.Equal("read", events[4].Flag);
            Assert.True(events[4].All);
            Assert.Empty(events[4].Messages!);
            Assert.Equal(new int[] { 1, 2 }, events[5].StreamIds);
            Assert.Equal(new int[] { 10, 11 }, events[5].UserIds);
            Assert.Equal("object", events[6].ExtensionData!["user"].GetProperty("variant").GetString());
            Assert.Equal(42, events[6].ExtensionData!["value"][1].GetProperty("payload").GetInt32());
            Assert.Equal(10, events[7].UserId);

            JsonElement roundTrip = JsonSerializer.SerializeToElement(events);
            Assert.Equal("future", roundTrip[6].GetProperty("future_field").GetProperty("nested")[1].GetString());
            Assert.Equal("two", roundTrip[0].GetProperty("message").GetProperty("future_message").GetProperty("nested")[1].GetString());
        }

        /// <summary>Accepts null update users and long event IDs without confusing them with message IDs.</summary>
        [Fact]
        public async Task Events_GetEvents_RenderingOnlyAndLongEventId()
        {
            Events resource = CreateEvents("""
                {"result":"success","events":[
                  {"id":2147483648,"type":"update_message","user_id":null,"rendering_only":true,
                   "message_id":31,"message_ids":[31],"flags":[],"edit_timestamp":1594825500,
                   "content":"original","rendered_content":"<p>Preview</p>"}
                ]}
                """);
            EventObject item = Assert.Single(await resource.GetEvents("queue", 0));
            Assert.Equal(2147483648L, item.Id);
            Assert.Null(item.UserId);
            Assert.True(item.RenderingOnly);
            Assert.Equal("<p>Preview</p>", item.RenderedContent);
        }

        /// <summary>Returned cursors can be passed directly into the next poll without narrowing.</summary>
        /// <param name="cursor">The received event ID.</param>
        [Theory]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData(2147483648L)]
        [InlineData(long.MaxValue)]
        public async Task Events_ReceivedIdRoundTripsIntoNextPoll(long cursor)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                $"{{\"result\":\"success\",\"events\":[{{\"id\":{cursor},\"type\":\"future\"}}]}}",
                "{\"result\":\"success\",\"events\":[]}");
            EventObject item = Assert.Single(await client.Events.GetEvents("queue", -1, true));
            Assert.Empty(await client.Events.GetEvents("queue", item.Id, true));
            Assert.Equal(cursor.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Utils.DecodeForm(requests[1].RequestUri!.Query)["last_event_id"]);
            Assert.Equal("true", Utils.DecodeForm(requests[1].RequestUri!.Query)["dont_block"]);
        }

        /// <summary>Long registration preserves diagnostics and rejects legacy projections without losing queue identity.</summary>
        [Fact]
        public async Task Events_LongRegistrationPreservesCursorAndLegacyCleanup()
        {
            const long cursor = 2147483648L;
            Events resource = CreateEvents($"{{\"result\":\"success\",\"queue_id\":\"queue:long\",\"last_event_id\":{cursor}}}");
            RegistrationResult registration = await resource.Register();
            Assert.Equal(cursor, registration.LastEventId64);
            Assert.Equal(cursor, registration.Response.LastEventId64);
            Assert.Equal(cursor, registration.RawInitialState.GetProperty("last_event_id").GetInt64());
            Assert.Throws<OverflowException>(() => registration.LastEventId);
            Assert.Throws<OverflowException>(() => registration.Response.LastEventId);
            JsonElement json = JsonSerializer.SerializeToElement(registration);
            Assert.Equal(cursor, json.GetProperty("LastEventId").GetInt64());
            Assert.Equal(cursor, json.GetProperty("Response").GetProperty("last_event_id").GetInt64());
            Assert.False(json.GetProperty("Response").TryGetProperty("LastEventId64", out _));
            (bool success, string? details, string? queueId, int lastEventId) legacy = await resource.TryRegisterQueue();
            Assert.False(legacy.success);
            Assert.Equal("queue:long", legacy.queueId);
            Assert.Contains("LastEventId64", legacy.details);
            Assert.Contains("queue:long", legacy.details);
            Assert.Contains("delete", legacy.details);
            await Assert.ThrowsAsync<Exception>(() => resource.RegisterQueue());
            Events assigned = new Events((method, url, data) => Task.FromResult(new ZulipResponse
            {
                Result = "success", QueueId = "assigned", LastEventId64 = cursor
            }));
            Assert.Equal(cursor, (await assigned.Register()).LastEventId64);
        }

        /// <summary>Fetches the actual initial registration fields, capabilities and request-local warnings.</summary>
        [Fact]
        public async Task Events_Register_DetailedInitialStateAndWarnings()
        {
            const string json = """
                {"result":"success","msg":"","queue_id":"queue:0","last_event_id":-1,
                 "zulip_feature_level":513,"zulip_version":"13.0-dev","zulip_merge_base":"13.0-dev",
                 "idle_queue_timeout_secs":3600,"event_queue_longpoll_timeout_seconds":110,
                 "server_web_public_streams_enabled":true,"server_can_summarize_topics":false,
                 "realm_users":[{"user_id":10,"email":"a@example.org","full_name":"A","future_user":true}],
                 "realm_non_active_users":[{"user_id":11,"email":"b@example.org","full_name":"B"}],
                 "subscriptions":[{"stream_id":1,"name":"General","color":"#aabbcc","subscribers":[10]}],
                 "unsubscribed":[{"stream_id":2,"name":"Past"}],
                 "never_subscribed":[{"stream_id":3,"name":"New"}],
                 "streams":[{"stream_id":1,"name":"General","description":"Announcements","future_channel":[true]}],
                 "max_message_id":32,"starred_messages":[31],
                 "unread_msgs":{"count":1,"pms":[],"streams":[{"stream_id":1,"topic":"T","unread_message_ids":[32]}],
                                "huddles":[],"mentions":[],"old_unreads_missing":false},
                 "recent_private_conversations":[{"max_message_id":30,"user_ids":[11]}],
                 "presences":{"10":{"active_timestamp":1594825500}},"server_timestamp":1594825510.25,
                 "ignored_parameters_unsupported":["future_option"],
                 "future_initial":{"data":["owned",null]}}
                """;
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json)!;
            Events resource = new Events((method, url, data) => Task.FromResult(response));
            RegistrationResult registration = await resource.Register();

            Assert.Equal("queue:0", registration.QueueId);
            Assert.Equal(-1, registration.LastEventId);
            Assert.Equal(513, registration.ZulipFeatureLevel);
            Assert.Equal("13.0-dev", registration.ZulipVersion);
            Assert.Equal("13.0-dev", registration.ZulipMergeBase);
            Assert.Equal(3600, registration.IdleQueueTimeoutSeconds);
            Assert.Equal(110, registration.EventQueueLongpollTimeoutSeconds);
            Assert.True(registration.ServerWebPublicStreamsEnabled);
            Assert.False(registration.ServerCanSummarizeTopics);
            Assert.Equal(10, Assert.Single(registration.RealmUsers!).UserId);
            Assert.Equal("B", Assert.Single(registration.RealmNonActiveUsers!).FullName);
            Assert.Null(registration.RealmUsers![0].IsActive);
            Assert.Null(registration.RealmNonActiveUsers![0].IsActive);
            Assert.Equal(1, Assert.Single(registration.Subscriptions!).StreamId);
            Assert.Equal("#aabbcc", registration.Subscriptions![0].Color);
            Assert.Equal(2, Assert.Single(registration.Unsubscribed!).StreamId);
            Assert.Equal(3, Assert.Single(registration.NeverSubscribed!).StreamId);
            Assert.Equal("Announcements", Assert.Single(registration.Streams!).Description);
            Assert.Equal(32, registration.MaxMessageId);
            Assert.Equal(new int[] { 31 }, registration.StarredMessages);
            Assert.Equal(1, registration.UnreadMessages!.Value.GetProperty("count").GetInt32());
            Assert.Equal(30, registration.RecentPrivateConversations!.Value[0].GetProperty("max_message_id").GetInt32());
            Assert.Equal(1594825500, registration.Presences!["10"].ActiveTimestamp);
            Assert.Equal(1594825510.25, registration.ServerTimestamp);
            Assert.Same(response, registration.Response);
            Assert.Null(registration.Response.CaughtException);
            Assert.Equal(new string[] { "future_option" }, registration.IgnoredParametersUnsupported);
            Assert.Equal("future_option", Assert.Single(registration.Response.IgnoredParametersUnsupported!));
            Assert.Equal("future_option", registration.RawInitialState.GetProperty("ignored_parameters_unsupported")[0].GetString());
            Assert.Equal("owned", registration.RawInitialState.GetProperty("future_initial").GetProperty("data")[0].GetString());
            Assert.True(registration.RawInitialState.GetProperty("realm_users")[0].GetProperty("future_user").GetBoolean());
            Assert.True(registration.RawInitialState.GetProperty("streams")[0].GetProperty("future_channel")[0].GetBoolean());
            Assert.Equal(1594825500, registration.RawInitialState.GetProperty("presences").GetProperty("10").GetProperty("active_timestamp").GetInt32());

            response.RawPayload = JsonSerializer.SerializeToElement(new { changed = true });
            response.QueueId = "changed";
            registration.Subscriptions![0].Name = "changed";
            Assert.Equal("queue:0", registration.QueueId);
            Assert.Equal("General", registration.RawInitialState.GetProperty("subscriptions")[0].GetProperty("name").GetString());
            Assert.Equal("owned", registration.RawInitialState.GetProperty("future_initial").GetProperty("data")[0].GetString());
        }

        /// <summary>Captures JSON form fields on the actual HTTP path, including the timeout union.</summary>
        /// <param name="timeoutJson">The numeric or mobile JSON timeout.</param>
        [Theory]
        [InlineData("3600")]
        [InlineData("\"mobile\"")]
        public async Task Events_Register_OptionsUseCanonicalFormFields(string timeoutJson)
        {
            Dictionary<string, string>? fields = null;
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage request, CancellationToken token) =>
                {
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.EndsWith("/api/v1/register", request.RequestUri!.AbsolutePath);
                    fields = [];
                    string form = await request.Content!.ReadAsStringAsync(token);
                    foreach (string pair in form.Split('&'))
                    {
                        string[] parts = pair.Split('=', 2);
                        fields.Add(Uri.UnescapeDataString(parts[0].Replace("+", " ")),
                            Uri.UnescapeDataString(parts[1].Replace("+", " ")));
                    }

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = Utils.ContentForJsonString(
                            "{\"result\":\"success\",\"queue_id\":\"queue\",\"last_event_id\":0,\"presences\":{\"10\":{\"active_timestamp\":1594825500}},\"ignored_parameters_unsupported\":[\"client_capabilities\"]}")
                    };
                });
            using HttpClient client = new HttpClient(handler.Object);
            ZulipClient zulipClient = new ZulipClient("https://example.org", "a@example.org", "key", client);
            RegistrationOptions options = new RegistrationOptions
            {
                EventTypes = new string[] { "message", "future_event" },
                FetchEventTypes = new string[] { "realm_user", "subscription", "stream", "realm" },
                Narrow = "[[\"stream\",\"General 你好\"]]",
                AllPublicStreams = true,
                ClientCapabilities = new Dictionary<string, bool>
                {
                    { "bulk_message_deletion", true },
                    { "empty_topic_name", true },
                    { "future_capability", false }
                },
                ClientGravatar = false,
                IncludeSubscribers = "partial",
                SlimPresence = true,
                ApplyMarkdown = true,
                IdleQueueTimeout = JsonSerializer.Deserialize<JsonElement>(timeoutJson)
            };
            RegistrationResult registration = await zulipClient.Events.Register(options);
            Assert.NotNull(fields);
            Assert.Equal(10, fields.Count);
            Assert.Equal(options.Narrow, fields["narrow"]);
            Assert.Equal("true", fields["all_public_streams"]);
            Assert.Equal("false", fields["client_gravatar"]);
            Assert.Equal("partial", fields["include_subscribers"]);
            Assert.Equal("true", fields["slim_presence"]);
            Assert.Equal("true", fields["apply_markdown"]);
            Assert.Equal(timeoutJson, fields["idle_queue_timeout"]);
            JsonElement eventTypes = JsonSerializer.Deserialize<JsonElement>(fields["event_types"]);
            Assert.Equal("message", eventTypes[0].GetString());
            Assert.Equal("future_event", eventTypes[1].GetString());
            JsonElement fetchTypes = JsonSerializer.Deserialize<JsonElement>(fields["fetch_event_types"]);
            Assert.Equal(4, fetchTypes.GetArrayLength());
            Assert.Equal("realm_user", fetchTypes[0].GetString());
            JsonElement capabilities = JsonSerializer.Deserialize<JsonElement>(fields["client_capabilities"]);
            Assert.True(capabilities.GetProperty("bulk_message_deletion").GetBoolean());
            Assert.True(capabilities.GetProperty("empty_topic_name").GetBoolean());
            Assert.False(capabilities.GetProperty("future_capability").GetBoolean());
            Assert.Equal("client_capabilities", Assert.Single(registration.Response.IgnoredParametersUnsupported!));
            Assert.Equal(0, registration.LastEventId);
            Assert.True(registration.Response.Success);
            Assert.Null(registration.Response.CaughtException);
            Assert.Equal(1594825500, registration.Presences!["10"].ActiveTimestamp);
        }

        /// <summary>Rejects missing, null and malformed required registration fields for detailed and legacy calls.</summary>
        /// <param name="fields">The incomplete response fields.</param>
        /// <param name="expected">The diagnostic field.</param>
        [Theory]
        [InlineData("", "queue_id")]
        [InlineData(",\"queue_id\":null,\"last_event_id\":-1", "queue_id")]
        [InlineData(",\"queue_id\":\"\",\"last_event_id\":-1", "queue_id")]
        [InlineData(",\"queue_id\":\"   \",\"last_event_id\":-1", "queue_id")]
        [InlineData(",\"queue_id\":1,\"last_event_id\":-1", "queue_id")]
        [InlineData(",\"queue_id\":\"queue\"", "last_event_id")]
        [InlineData(",\"queue_id\":\"queue\",\"last_event_id\":null", "last_event_id")]
        [InlineData(",\"queue_id\":\"queue\",\"last_event_id\":\"0\"", "last_event_id")]
        [InlineData(",\"queue_id\":\"queue\",\"last_event_id\":1.5", "last_event_id")]
        [InlineData(",\"queue_id\":\"queue\",\"last_event_id\":9223372036854775808", "last_event_id")]
        public async Task Events_Register_RequiredDataFails(string fields, string expected)
        {
            Events resource = CreateEvents($"{{\"result\":\"success\"{fields}}}");
            (bool success, string? details, RegistrationResult? registration) result = await resource.TryRegister();
            Assert.False(result.success);
            Assert.Null(result.registration);
            Assert.Contains("Events.Register failed:", result.details);
            Assert.Contains(expected, result.details);
            (bool success, string? details, string? queueId, int lastEventId) legacy = await resource.TryRegisterQueue();
            Assert.False(legacy.success);
            Assert.Null(legacy.queueId);
            Assert.Equal(-1, legacy.lastEventId);
            Assert.Contains("Events.RegisterQueue failed:", legacy.details);
            Assert.Contains(expected, legacy.details);
            await Assert.ThrowsAsync<Exception>(() => resource.Register());
            await Assert.ThrowsAsync<Exception>(() => resource.RegisterQueue());
        }

        /// <summary>Rejects malformed typed initial state rather than manufacturing default identities.</summary>
        /// <param name="fields">The malformed supplied initial field.</param>
        /// <param name="expected">The diagnostic field.</param>
        [Theory]
        [InlineData(",\"realm_users\":null", "realm_users")]
        [InlineData(",\"realm_users\":[{}]", "user_id")]
        [InlineData(",\"realm_users\":[{\"user_id\":2147483648,\"email\":\"a\",\"full_name\":\"A\"}]", "user_id")]
        [InlineData(",\"subscriptions\":[{\"name\":\"General\"}]", "stream_id")]
        [InlineData(",\"streams\":[{\"stream_id\":2147483648,\"name\":\"General\"}]", "stream_id")]
        [InlineData(",\"streams\":{}", "streams")]
        [InlineData(",\"zulip_feature_level\":null", "zulip_feature_level")]
        [InlineData(",\"idle_queue_timeout_secs\":\"3600\"", "idle_queue_timeout_secs")]
        [InlineData(",\"event_queue_longpoll_timeout_seconds\":1.5", "event_queue_longpoll_timeout_seconds")]
        [InlineData(",\"server_can_summarize_topics\":\"false\"", "server_can_summarize_topics")]
        [InlineData(",\"max_message_id\":2147483648", "max_message_id")]
        [InlineData(",\"starred_messages\":[2147483648]", "starred_messages")]
        [InlineData(",\"unread_msgs\":[]", "unread_msgs")]
        [InlineData(",\"presences\":{\"10\":{\"active_timestamp\":\"bad\"}}", "presences")]
        [InlineData(",\"presences\":{\"10\":null}", "presences")]
        public async Task Events_Register_InvalidInitialDataFails(string fields, string expected)
        {
            Events resource = CreateEvents($"{{\"result\":\"success\",\"queue_id\":\"queue\",\"last_event_id\":-1{fields}}}");
            (bool success, string? details, RegistrationResult? registration) result = await resource.TryRegister();
            Assert.False(result.success);
            Assert.Null(result.registration);
            Assert.Contains(expected, result.details);
        }

        /// <summary>Allows genuine empty initial collections and keeps the legacy queue method a validated projection.</summary>
        [Fact]
        public async Task Events_Register_EmptyInitialStateAndLegacyProjection()
        {
            Dictionary<string, string>? captured = null;
            Events resource = new Events((method, url, data) =>
            {
                Assert.Equal(HttpMethod.Post, method);
                Assert.Equal("api/v1/register", url);
                captured = data;
                return Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>("""
                    {"result":"success","queue_id":"queue","last_event_id":-1,
                     "realm_users":[],"realm_non_active_users":[],"subscriptions":[],"unsubscribed":[],
                     "never_subscribed":[],"streams":[],"starred_messages":[],
                     "ignored_parameters_unsupported":["future_option"]}
                    """)!);
            });
            RegistrationResult registration = await resource.Register();
            Assert.Empty(captured!);
            Assert.Empty(registration.RealmUsers!);
            Assert.Empty(registration.RealmNonActiveUsers!);
            Assert.Empty(registration.Subscriptions!);
            Assert.Empty(registration.Unsubscribed!);
            Assert.Empty(registration.NeverSubscribed!);
            Assert.Empty(registration.Streams!);
            Assert.Empty(registration.StarredMessages!);
            Assert.Null(registration.ZulipFeatureLevel);
            Assert.Null(registration.IdleQueueTimeoutSeconds);
            (bool success, string? details, string? queueId, int lastEventId) legacy =
                await resource.TryRegisterQueue("[\"message\"]", "[]", false);
            Assert.True(legacy.success, legacy.details);
            Assert.Equal(registration.QueueId, legacy.queueId);
            Assert.Equal(registration.LastEventId, legacy.lastEventId);
            Assert.Equal("[\"message\"]", captured!["event_types"]);
            Assert.Equal("[]", captured["narrow"]);
            Assert.Equal("false", captured["all_public_streams"]);
            Assert.Equal(("queue", -1), await resource.RegisterQueue());
        }

        /// <summary>Validates body-only and explicitly assigned delegates without relying on transport defaults.</summary>
        [Fact]
        public async Task Events_Register_InjectedDelegatesAndOwnedBody()
        {
            ZulipResponse bodyResponse = new ZulipResponse
            {
                Result = "success",
                HttpResponseBody = "{\"result\":\"success\",\"queue_id\":\"body\",\"last_event_id\":0,\"ignored_parameters_unsupported\":[\"future\"],\"future\":{\"owned\":true}}"
            };
            Events bodyResource = new Events((method, url, data) => Task.FromResult(bodyResponse));
            RegistrationResult registration = await bodyResource.Register();
            bodyResponse.HttpResponseBody = "";
            Assert.Equal("body", registration.QueueId);
            Assert.Equal(0, registration.LastEventId);
            Assert.True(registration.RawInitialState.GetProperty("future").GetProperty("owned").GetBoolean());
            Assert.Equal("future", Assert.Single(registration.Response.IgnoredParametersUnsupported!));
            Assert.True(registration.Response.RawPayload.GetProperty("future").GetProperty("owned").GetBoolean());

            Events assigned = new Events((method, url, data) => Task.FromResult(new ZulipResponse
            {
                Result = "success", QueueId = "assigned", LastEventId = 0
            }));
            Assert.Equal(("assigned", 0), await assigned.RegisterQueue());
            Events missingCursor = new Events((method, url, data) => Task.FromResult(new ZulipResponse
            {
                Result = "success", QueueId = "assigned"
            }));
            Assert.False((await missingCursor.TryRegister()).success);
            Assert.False((await missingCursor.TryRegisterQueue()).success);
        }

        /// <summary>Accepts actual legacy initial presences without changing transport-failure semantics.</summary>
        [Fact]
        public async Task Events_Register_LegacyPresenceAndUnrelatedErrors()
        {
            const string json = """
                {"result":"success","queue_id":"queue","last_event_id":-1,
                 "presences":{"a@example.org":{"website":{"status":"active","timestamp":123},
                                               "aggregated":{"status":"active","timestamp":123}}}}
                """;
            RegistrationResult registration = await CreateEvents(json).Register();
            Assert.Equal("active", registration.Presences!["a@example.org"].Website!.Status);
            Assert.Equal(123, registration.Presences["a@example.org"].Aggregated!.Timestamp);
            Assert.Null(registration.Presences["a@example.org"].ActiveTimestamp);
            Assert.Null(registration.Response.CaughtException);

            ZulipResponse failedResponse = JsonSerializer.Deserialize<ZulipResponse>(json)!;
            failedResponse.CaughtException = "Transport failure";
            Events failed = new Events((method, url, data) => Task.FromResult(failedResponse));
            (bool success, string? details, RegistrationResult? registration) result = await failed.TryRegister();
            Assert.False(result.success);
            Assert.Null(result.registration);
            Assert.Contains("Transport failure", result.details);
            Assert.Equal("Transport failure", failedResponse.CaughtException);
        }

        /// <summary>Requires an actual events array and valid event identities, even for injected delegates.</summary>
        /// <param name="fields">The malformed events field.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"events\":null")]
        [InlineData(",\"events\":{}")]
        [InlineData(",\"events\":[null]")]
        [InlineData(",\"events\":[{}]")]
        [InlineData(",\"events\":[{\"type\":\"heartbeat\"}]")]
        [InlineData(",\"events\":[{\"id\":null,\"type\":\"heartbeat\"}]")]
        [InlineData(",\"events\":[{\"id\":\"0\",\"type\":\"heartbeat\"}]")]
        [InlineData(",\"events\":[{\"id\":0.5,\"type\":\"heartbeat\"}]")]
        [InlineData(",\"events\":[{\"id\":0}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":null}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":\"\"}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":1}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":\"message\",\"message\":null}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":\"message\",\"message\":{\"content\":\"missing id\"}}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":\"message\",\"message\":{\"id\":2147483648}}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":\"message\",\"message\":{\"id\":31,\"sender_id\":\"bad\"}}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":\"delete_message\",\"message_id\":2147483648}]")]
        [InlineData(",\"events\":[{\"id\":0,\"type\":\"update_message_flags\",\"messages\":[1.5]}]")]
        public async Task Events_GetEvents_InvalidRequiredDataFails(string fields)
        {
            Events resource = CreateEvents($"{{\"result\":\"success\"{fields}}}");
            (bool success, string? details, List<EventObject>? events) result = await resource.TryGetEvents("queue", -1);
            Assert.False(result.success);
            Assert.Null(result.events);
            Assert.Contains("Events.GetEvents failed:", result.details);
            await Assert.ThrowsAsync<Exception>(() => resource.GetEvents("queue", -1));
        }

        /// <summary>Distinguishes genuine empty events from missing data on caller-created and body-only delegates.</summary>
        [Fact]
        public async Task Events_GetEvents_EmptyArrayIsValidForInjectedDelegates()
        {
            Events resource = CreateEvents("{\"result\":\"success\",\"events\":[]}");
            Assert.Empty(await resource.GetEvents("queue", -1, true));
            Events assigned = new Events((method, url, data) => Task.FromResult(new ZulipResponse
            {
                Result = "success", Events = []
            }));
            Assert.Empty(await assigned.GetEvents("queue", -1));
            Events missing = new Events((method, url, data) => Task.FromResult(new ZulipResponse { Result = "success" }));
            Assert.False((await missing.TryGetEvents("queue", -1)).success);
            Events bodyOnly = new Events((method, url, data) => Task.FromResult(new ZulipResponse
            {
                Result = "success", HttpResponseBody = "{\"result\":\"success\",\"events\":[]}"
            }));
            Assert.Empty(await bodyOnly.GetEvents("queue", -1));
        }

        /// <summary>Rejects unsupported subscriber values and malformed timeout unions before sending.</summary>
        /// <param name="timeoutJson">The invalid timeout JSON, or null to test subscriber inclusion.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("null")]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("1.5")]
        [InlineData("2147483648")]
        [InlineData("\"3600\"")]
        [InlineData("\"desktop\"")]
        [InlineData("true")]
        public async Task Events_Register_InvalidOptionsMakeNoRequest(string? timeoutJson)
        {
            Events resource = new Events((method, url, data) => throw new InvalidOperationException("Must not send."));
            RegistrationOptions options = new RegistrationOptions
            {
                IncludeSubscribers = timeoutJson == null ? "yes" : null,
                IdleQueueTimeout = timeoutJson == null ? null : JsonSerializer.Deserialize<JsonElement>(timeoutJson)
            };
            (bool success, string? details, RegistrationResult? registration) result = await resource.TryRegister(options);
            Assert.False(result.success);
            Assert.Null(result.registration);
            Assert.Contains(timeoutJson == null ? "include_subscribers" : "idle_queue_timeout", result.details);
            await Assert.ThrowsAsync<Exception>(() => resource.Register(options));
        }

        /// <summary>Creates a direct request delegate from an owned deserialized response.</summary>
        /// <param name="json">The response envelope.</param>
        /// <returns>The resource under test.</returns>
        private static Events CreateEvents(string json)
        {
            return new Events((method, url, data) => Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>(json)!));
        }
    }
}
