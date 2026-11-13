using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Resources;

namespace zulip_set_lib.tests
{
    /// <summary>Personal presence publication, realm reads and complementary personal workflows.</summary>
    public class UserPresenceTests
    {
        /// <summary>Incremental publication preserves modern presence, revision, clock and exact options.</summary>
        [Fact]
        public async Task Users_UpdateOwnPresence_ModernIncremental()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"presence_last_update_id\":1000,\"server_timestamp\":1656958539.5," +
                "\"presences\":{\"10\":{\"active_timestamp\":1656958520,\"idle_timestamp\":1656958530}}}");
            RealmPresenceResult presence = await client.Users.UpdateOwnPresence(
                Users.PresenceStatus.Active, -1, newUserInput: true, historyLimitDays: 365);
            Assert.Equal(1000, presence.PresenceLastUpdateId);
            Assert.Equal(1656958539.5, presence.ServerTimestamp);
            Assert.Equal(1656958520, presence.Presences!["10"].ActiveTimestamp);
            Assert.Same(client.LastResponse, presence.Response);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal("/api/v1/users/me/presence", request.RequestUri!.AbsolutePath);
            Dictionary<string, string> form = await Utils.ReadForm(request);
            Assert.Equal("active", form["status"]);
            Assert.Equal("-1", form["last_update_id"]);
            Assert.Equal("true", form["new_user_input"]);
            Assert.Equal("false", form["ping_only"]);
            Assert.Equal("365", form["history_limit_days"]);
            Assert.False(form.ContainsKey("slim_presence"));
        }

        /// <summary>Ping-only success requires a revision but no fetched data, while empty fetched data is valid.</summary>
        [Fact]
        public async Task Users_UpdateOwnPresence_PingAndEmpty()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"presence_last_update_id\":1000}",
                "{\"result\":\"success\",\"presence_last_update_id\":1000,\"server_timestamp\":100.5,\"presences\":{}}");
            RealmPresenceResult ping = await client.Users.UpdateOwnPresence(Users.PresenceStatus.Idle, 1000, true);
            Assert.Null(ping.Presences);
            Assert.Null(ping.ServerTimestamp);
            Assert.Equal(1000, ping.PresenceLastUpdateId);
            RealmPresenceResult fetched = await client.Users.UpdateOwnPresence(Users.PresenceStatus.Idle, 1000);
            Assert.Empty(fetched.Presences!);
            Assert.Equal("1000", (await Utils.ReadForm(requests[1]))["last_update_id"]);
        }

        /// <summary>Realm lookup retains the actual email-keyed legacy entries without requesting modern-only options.</summary>
        [Fact]
        public async Task Users_GetRealmPresence_LegacyPayload()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"server_timestamp\":100.5,\"presences\":{\"user@example.org\":{" +
                "\"website\":{\"status\":\"active\",\"timestamp\":100,\"client\":\"website\"}," +
                "\"aggregated\":{\"status\":\"active\",\"timestamp\":100}}}}");
            RealmPresenceResult result = await client.Users.GetRealmPresence();
            Assert.Equal("active", result.Presences!["user@example.org"].Aggregated!.Status);
            Assert.Null(result.PresenceLastUpdateId);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal("/api/v1/realm/presence", request.RequestUri!.AbsolutePath);
            Assert.Null(request.Content);
        }

        /// <summary>Malformed publication results and missing realm data fail rather than invent state.</summary>
        /// <param name="json">The invalid response.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}")]
        [InlineData("{\"result\":\"success\",\"presence_last_update_id\":\"5\",\"server_timestamp\":100,\"presences\":{}}")]
        [InlineData("{\"result\":\"success\",\"presence_last_update_id\":5,\"presences\":{}}")]
        [InlineData("{\"result\":\"success\",\"presence_last_update_id\":5,\"server_timestamp\":100,\"presences\":{\"10\":{}}}")]
        [InlineData("{\"result\":\"error\",\"msg\":\"Forbidden\"}")]
        public async Task Users_UpdateOwnPresence_RequiresPayload(string json)
        {
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            var result = await client.Users.TryUpdateOwnPresence(Users.PresenceStatus.Active);
            Assert.False(result.success);
            Assert.Null(result.presence);
            Assert.Contains("Users.UpdateOwnPresence failed:", result.details);
            await Assert.ThrowsAsync<Exception>(() => client.Users.UpdateOwnPresence(Users.PresenceStatus.Active));
        }

        /// <summary>Alert-word removal is a DELETE with a JSON array and returns the remaining words.</summary>
        [Fact]
        public async Task Users_RemoveAlertWords_ReturnsRemaining()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\",\"alert_words\":[\"bar\"]}",
                "{\"result\":\"success\",\"alert_words\":[]}");
            Assert.Equal(new[] { "bar" }, await client.Users.RemoveAlertWords(["a,b", "quoted \"word\""]));
            Assert.Empty(await client.Users.RemoveAlertWords(["bar"]));
            Assert.Equal(HttpMethod.Delete, requests[0].Method);
            using JsonDocument words = JsonDocument.Parse((await Utils.ReadForm(requests[0]))["alert_words"]);
            Assert.Equal("a,b", words.RootElement[0].GetString());
            Assert.Equal("quoted \"word\"", words.RootElement[1].GetString());
        }

        /// <summary>Removal cannot succeed with missing/malformed words and realm reads require a real clock and entries.</summary>
        /// <param name="json">The malformed success payload.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}")]
        [InlineData("{\"result\":\"success\",\"alert_words\":[null],\"server_timestamp\":100,\"presences\":{\"10\":{}}}")]
        public async Task Users_PersonalReads_RequirePayload(string json)
        {
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            Assert.False((await client.Users.TryRemoveAlertWords(["word"])).success);
            Assert.False((await client.Users.TryGetRealmPresence()).success);
        }

        /// <summary>Editing notifications use the message path and start/stop rather than ordinary typing parameters.</summary>
        [Fact]
        public async Task Users_SetMessageEditTyping_RequestAndError()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}",
                "{\"result\":\"success\"}", "{\"result\":\"error\",\"msg\":\"Not allowed\"}");
            await client.Users.SetMessageEditTyping(47, true);
            await client.Users.SetMessageEditTyping(47, false);
            Assert.Equal("/api/v1/messages/47/typing", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("start", (await Utils.ReadForm(requests[0]))["op"]);
            Assert.Equal("stop", (await Utils.ReadForm(requests[1]))["op"]);
            Assert.False((await client.Users.TrySetMessageEditTyping(47, true)).success);
        }
    }
}
