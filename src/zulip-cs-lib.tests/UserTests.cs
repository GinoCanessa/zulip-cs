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
    /// <summary>Tests for Users resource.</summary>
    public class UserTests
    {
        /// <summary>Missing group identity does not become a successful zero.</summary>
        [Fact]
        public async Task Users_GetGroups_RequiresNestedIdentity()
        {
            const string json = "{\"result\":\"success\",\"user_groups\":[{}]}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            Assert.False((await client.Users.TryGetGroups()).success);
            await Assert.ThrowsAsync<System.Exception>(() => client.Users.GetGroups());
        }

        [Fact]
        public async Task Users_GetOwnUser_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user_id\":10,\"email\":\"me@example.org\",\"full_name\":\"Test User\",\"avatar_url\":\"https://example.org/avatar\",\"role\":400,\"is_bot\":false,\"timezone\":\"UTC\",\"max_message_id\":101}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetOwnUser();
            Assert.True(actual.success, actual.details);
            Assert.NotNull(actual.user);
            Assert.Equal(10, actual.user.UserId);
            Assert.Equal("me@example.org", actual.user.Email);
            Assert.Equal("Test User", actual.user.FullName);
            Assert.Equal("UTC", actual.user.Timezone);
            Assert.Equal(10, (await zulipClient.Users.GetOwnUser()).UserId);
        }

        [Fact]
        public async Task Users_GetUser_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user\":{\"user_id\":42,\"email\":\"other@example.org\",\"full_name\":\"Other User\"}}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetUser(42);
            Assert.True(actual.success, actual.details);
            Assert.Equal(42, actual.user!.UserId);
        }

        [Fact]
        public async Task Users_GetUserByEmail_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user\":{\"user_id\":5,\"email\":\"user@example.org\",\"full_name\":\"Email User\"}}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetUserByEmail("user@example.org");
            Assert.True(actual.success, actual.details);
            Assert.Equal("user@example.org", actual.user!.Email);
        }

        [Fact]
        public async Task Users_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"members\":[{\"user_id\":1,\"email\":\"a@example.org\",\"full_name\":\"User A\"},{\"user_id\":2,\"email\":\"b@example.org\",\"full_name\":\"User B\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Equal(2, actual.members!.Count);
        }

        [Fact]
        public async Task Users_SetTypingStatus_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TrySetTypingStatus("start", new int[] { 1, 2 });
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_Create_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryCreate("new@example.org", "password123", "New User");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_Update_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryUpdate(10, fullName: "Updated Name");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_Deactivate_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryDeactivate(10);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_Reactivate_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryReactivate(10);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_MuteUser_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryMuteUser(10);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_UnmuteUser_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryUnmuteUser(10);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_GetAlertWords_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"alert_words\":[\"foo\",\"bar\"]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetAlertWords();
            Assert.True(actual.success, actual.details);
            Assert.Equal(2, actual.alertWords!.Count);
            Assert.Contains("foo", actual.alertWords!);
        }

        [Fact]
        public async Task Users_AddAlertWords_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryAddAlertWords(new string[] { "alert1", "alert2" });
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_GetGroups_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user_groups\":[{\"id\":1,\"name\":\"admins\",\"description\":\"Admin group\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetGroups();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.groups!);
        }

        [Fact]
        public async Task Users_CreateGroup_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"group_id\":42}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryCreateGroup("test-group", "A test group", new int[] { 1, 2 });
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_GetOwnUser_Error()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"error\",\"msg\":\"Not logged in\",\"code\":\"UNAUTHORIZED\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.Unauthorized, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetOwnUser();
            Assert.False(actual.success);
            Assert.False(string.IsNullOrEmpty(actual.details));
        }

        /// <summary>Rejects malformed successful own-profile identities through injected delegates.</summary>
        /// <param name="json">The malformed profile response.</param>
        /// <param name="field">The missing identity field.</param>
        [Theory]
        [InlineData("{\"result\":\"success\",\"email\":\"me@example.org\",\"full_name\":\"Me\"}", "user_id")]
        [InlineData("{\"result\":\"success\",\"user_id\":10,\"full_name\":\"Me\"}", "email")]
        [InlineData("{\"result\":\"success\",\"user_id\":10,\"email\":\"me@example.org\"}", "full_name")]
        [InlineData("{\"result\":\"success\",\"user\":{\"user_id\":10,\"email\":\"me@example.org\",\"full_name\":\"Me\"}}", "user_id")]
        public async Task Users_GetOwnUser_MissingIdentityFails(string json, string field)
        {
            Users users = new Users((method, url, data) =>
                Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>(json)!));

            (bool success, string? details, UserObject? user) = await users.TryGetOwnUser();
            Assert.False(success);
            Assert.Null(user);
            Assert.Contains("Users.GetOwnUser failed:", details);
            Assert.Contains(field, details);
            await Assert.ThrowsAsync<Exception>(() => users.GetOwnUser());
        }

        /// <summary>Reads populated and empty integer group members from the actual endpoint.</summary>
        /// <param name="membersJson">The actual numeric members array.</param>
        /// <param name="count">The expected member count.</param>
        [Theory]
        [InlineData("[10,12]", 2)]
        [InlineData("[]", 0)]
        public async Task Users_GroupMembers_ActualNumericPayload(string membersJson, int count)
        {
            HttpContent content = Utils.ContentForJsonString($"{{\"result\":\"success\",\"msg\":\"\",\"members\":{membersJson}}}");
            Assert.True(Utils.TryGetMockedClient(HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient));

            (bool success, string? details, List<int>? members) = await zulipClient.Users.TryGetGroupMembers(7);
            Assert.True(success, details);
            Assert.Equal(count, members!.Count);
            if (count != 0) Assert.Equal(new int[] { 10, 12 }, members);
            Assert.Equal(members, await zulipClient.Users.GetGroupMembers(7));
            handler.Protected().Verify("SendAsync", Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(request => request.Method == HttpMethod.Get &&
                    request.RequestUri!.AbsolutePath == "/api/v1/user_groups/7/members" &&
                    request.RequestUri.Query == string.Empty),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>Preserves the requested direct-member filter on the request delegate.</summary>
        /// <param name="directOnly">The requested filtering mode.</param>
        /// <param name="expected">The expected wire value.</param>
        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public async Task Users_GroupMembers_DirectMemberFiltering(bool directOnly, string expected)
        {
            string? captured = null;
            Users users = new Users((method, url, data) =>
            {
                captured = data!["direct_member_only"];
                Assert.Equal(HttpMethod.Get, method);
                Assert.Equal("api/v1/user_groups/7/members", url);
                return Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>("{\"result\":\"success\",\"members\":[10]}")!);
            });

            Assert.Equal(new int[] { 10 }, await users.GetGroupMembers(7, directOnly));
            Assert.Equal(expected, captured);
        }

        /// <summary>Rejects missing, malformed and wrong-envelope group member lists.</summary>
        /// <param name="payload">The malformed members fields.</param>
        /// <param name="expected">The expected diagnostic text.</param>
        [Theory]
        [InlineData("", "members")]
        [InlineData(",\"members\":null", "members")]
        [InlineData(",\"members\":{}", "members")]
        [InlineData(",\"members\":[{\"user_id\":10}]", "Invalid response payload")]
        [InlineData(",\"user_ids\":[10,12]", "members")]
        public async Task Users_GroupMembers_InvalidPayloadFails(string payload, string expected)
        {
            Users users = new Users((method, url, data) => Task.FromResult(
                JsonSerializer.Deserialize<ZulipResponse>($"{{\"result\":\"success\"{payload}}}")!));

            (bool success, string? details, List<int>? members) = await users.TryGetGroupMembers(7);
            Assert.False(success);
            Assert.Null(members);
            Assert.Contains("Users.GetGroupMembers failed:", details);
            Assert.Contains(expected, details);
            await Assert.ThrowsAsync<Exception>(() => users.GetGroupMembers(7));
        }

        /// <summary>Preserves modern presence data and limits the legacy adapter to actual legacy entries.</summary>
        /// <param name="presenceJson">The real-shaped presence data.</param>
        /// <param name="hasLegacy">Whether the response includes legacy entries.</param>
        [Theory]
        [InlineData("{\"active_timestamp\":1656958520,\"idle_timestamp\":1656958530,\"website\":{\"status\":\"active\",\"timestamp\":1656958520},\"aggregated\":{\"status\":\"offline\",\"timestamp\":1656958520}}", true)]
        [InlineData("{\"active_timestamp\":1656958520,\"idle_timestamp\":1656958530}", false)]
        public async Task Users_Presence_ModernDetailsAndLegacyAdapter(string presenceJson, bool hasLegacy)
        {
            HttpContent content = Utils.ContentForJsonString(
                $"{{\"result\":\"success\",\"msg\":\"\",\"presence\":{presenceJson},\"server_timestamp\":1656958540.123,\"ignored_parameters_unsupported\":[\"future_option\"],\"future_payload\":{{\"enabled\":true}}}}");
            Assert.True(Utils.TryGetMockedClient(HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient));

            PresenceResult detailed = await zulipClient.Users.GetPresenceDetails("10");
            Assert.Equal(1656958520L, detailed.Presence.ActiveTimestamp);
            Assert.Equal(1656958530L, detailed.Presence.IdleTimestamp);
            Assert.Equal(1656958540.123, detailed.ServerTimestamp);
            Assert.Equal(new string[] { "future_option" }, detailed.IgnoredParametersUnsupported);
            Assert.True(detailed.Response.ExtensionData!["future_payload"].GetProperty("enabled").GetBoolean());
            Assert.Same(detailed.Response, zulipClient.LastResponse);
            Assert.True(detailed.Response.Success);
            (bool success, string? details, Dictionary<string, PresenceInfo>? legacy) = await zulipClient.Users.TryGetPresence("10");
            Assert.Equal(hasLegacy, success);
            if (hasLegacy)
            {
                Assert.Equal("offline", legacy!["aggregated"].Status);
                Assert.Equal("active", legacy["website"].Status);
                Assert.DoesNotContain("active_timestamp", legacy.Keys);
            }
            else
            {
                Assert.Null(legacy);
                Assert.Null(detailed.Presence.Website);
                Assert.Null(detailed.Presence.Aggregated);
                Assert.Contains("GetPresenceDetails", details);
                await Assert.ThrowsAsync<Exception>(() => zulipClient.Users.GetPresence("10"));
            }

            Assert.Equal(new string[] { "future_option" }, detailed.IgnoredParametersUnsupported);
            handler.Protected().Verify("SendAsync", Times.AtLeast(2),
                ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.AbsolutePath == "/api/v1/users/10/presence"),
                ItExpr.IsAny<CancellationToken>());
        }

        /// <summary>Retains old client-specific presence without requiring a modern server clock.</summary>
        [Fact]
        public async Task Users_Presence_LegacyShapeAndOptionalServerClock()
        {
            const string json = "{\"result\":\"success\",\"presence\":{\"website\":{\"status\":\"idle\",\"timestamp\":123},\"aggregated\":{\"status\":\"offline\",\"timestamp\":123},\"desktop\":{\"status\":\"idle\",\"timestamp\":123,\"client\":\"desktop\"}}}";
            Users users = new Users((method, url, data) =>
                Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>(json)!));

            PresenceResult details = await users.GetPresenceDetails("me@example.org");
            Assert.Null(details.ServerTimestamp);
            Assert.Null(details.Presence.ActiveTimestamp);
            Assert.Null(details.Presence.IdleTimestamp);
            Assert.Equal("offline", details.Presence.Aggregated!.Status);
            Assert.Equal("desktop", (await users.GetPresence("me@example.org"))["desktop"].Client);
        }

        /// <summary>Turns missing or malformed required presence into standard Try failures.</summary>
        /// <param name="payload">The malformed presence fields.</param>
        /// <param name="expected">The expected diagnostic text.</param>
        [Theory]
        [InlineData("", "presence")]
        [InlineData(",\"presence\":null", "presence")]
        [InlineData(",\"presence\":[]", "presence")]
        [InlineData(",\"presence\":{\"active_timestamp\":\"123\"}", "active_timestamp")]
        [InlineData(",\"presence\":{\"website\":{\"timestamp\":123}}", "website")]
        [InlineData(",\"presence\":{\"idle_timestamp\":123},\"server_timestamp\":\"bad\"", "server_timestamp")]
        [InlineData(",\"presence\":{\"idle_timestamp\":123},\"server_timestamp\":null", "server_timestamp")]
        public async Task Users_Presence_InvalidPayloadFails(string payload, string expected)
        {
            Users users = new Users((method, url, data) =>
                Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>($"{{\"result\":\"success\"{payload}}}")!));
            (bool success, string? details, PresenceResult? presence) = await users.TryGetPresenceDetails("10");
            Assert.False(success);
            Assert.Null(presence);
            Assert.Contains("Users.GetPresenceDetails failed:", details);
            Assert.Contains(expected, details);
            await Assert.ThrowsAsync<Exception>(() => users.GetPresenceDetails("10"));
        }

        /// <summary>Validates every data-returning user getter independently of the transport.</summary>
        /// <param name="payload">The malformed required fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"user\":null,\"members\":null,\"status\":null,\"user_groups\":null,\"alert_words\":null")]
        [InlineData(",\"user\":[],\"members\":{},\"status\":[],\"user_groups\":{},\"alert_words\":{}")]
        public async Task Users_RequiredData_InvalidSuccessFailsForInjectedDelegates(string payload)
        {
            Users users = new Users((method, url, data) =>
                Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>($"{{\"result\":\"success\"{payload}}}")!));

            Assert.False((await users.TryGetUser(10)).success);
            Assert.False((await users.TryGetUserByEmail("me@example.org")).success);
            Assert.False((await users.TryGetAll()).success);
            Assert.False((await users.TryGetStatus(10)).success);
            Assert.False((await users.TryGetGroups()).success);
            Assert.False((await users.TryGetAlertWords()).success);
            await Assert.ThrowsAsync<Exception>(() => users.GetUser(10));
            await Assert.ThrowsAsync<Exception>(() => users.GetUserByEmail("me@example.org"));
            await Assert.ThrowsAsync<Exception>(() => users.GetAll());
            await Assert.ThrowsAsync<Exception>(() => users.GetStatus(10));
            await Assert.ThrowsAsync<Exception>(() => users.GetGroups());
            await Assert.ThrowsAsync<Exception>(() => users.GetAlertWords());
        }

        /// <summary>Accepts legitimate empty user collections and unset status.</summary>
        [Fact]
        public async Task Users_RequiredData_EmptyCollectionsAndStatusAreValid()
        {
            Users users = new Users((method, url, data) => Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>(
                "{\"result\":\"success\",\"members\":[],\"user_groups\":[],\"alert_words\":[],\"status\":{}}")!));
            Assert.Empty(await users.GetAll());
            Assert.Empty(await users.GetGroups());
            Assert.Empty(await users.GetAlertWords());
            Assert.NotNull(await users.GetStatus(10));
        }

        /// <summary>Owns the body-only delegate payload and supports its legacy projection.</summary>
        [Fact]
        public async Task Users_Presence_BodyOnlyInjectedDelegateRetainsOwnedData()
        {
            Users users = new Users((method, url, data) => Task.FromResult(new ZulipResponse
            {
                Result = "success",
                HttpResponseBody = "{\"result\":\"success\",\"presence\":{\"website\":{\"status\":\"idle\",\"timestamp\":123}},\"future\":true}"
            }));
            PresenceResult presence = await users.GetPresenceDetails("10");
            Assert.Equal(123, presence.Response.RawPayload.GetProperty("presence").GetProperty("website").GetProperty("timestamp").GetInt32());
            Assert.Equal("idle", (await users.GetPresence("10"))["website"].Status);
        }

        /// <summary>Retains unknown modern presence fields without interpreting them as legacy status.</summary>
        [Fact]
        public async Task Users_Presence_UnknownFieldsDoNotInvalidateModernData()
        {
            Users users = new Users((method, url, data) => Task.FromResult(JsonSerializer.Deserialize<ZulipResponse>(
                "{\"result\":\"success\",\"presence\":{\"active_timestamp\":123,\"future\":{\"status\":\"new-shape\"},\"future_scalar\":true}}")!));
            PresenceResult result = await users.GetPresenceDetails("10");
            Assert.Equal("new-shape", result.Presence.ExtensionData!["future"].GetProperty("status").GetString());
            Assert.True(result.Presence.ExtensionData["future_scalar"].GetBoolean());
        }
    }
}
