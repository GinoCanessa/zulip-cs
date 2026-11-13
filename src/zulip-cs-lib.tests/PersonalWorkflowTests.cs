using System;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>Personal preference, custom profile and avatar contract tests.</summary>
    public class PersonalWorkflowTests
    {
        /// <summary>Selected settings are real unquoted form values, with false retained and null omitted.</summary>
        [Fact]
        public async Task Users_Settings_ActualFormAndOmission()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"msg\":\"\",\"ignored_parameters_unsupported\":[\"future\"]}",
                "{\"result\":\"success\",\"msg\":\"\"}");
            UserSettingsOptions options = new UserSettingsOptions
            {
                Timezone = "America/Chicago",
                DefaultLanguage = "de",
                TwentyFourHourTime = false,
                EnterSends = true,
                EnableDesktopNotifications = false,
                EnableOfflineEmailNotifications = true,
                EnableOfflinePushNotifications = false,
                EnableOnlinePushNotifications = true,
                EnableStreamDesktopNotifications = true,
                EnableStreamEmailNotifications = false,
                EnableStreamPushNotifications = true,
                EnableDigestEmails = false
            };

            (bool success, string? details) result = await client.Users.TryUpdateSettings(options);
            Assert.True(result.success, result.details);
            Assert.Equal(new Dictionary<string, string>
            {
                { "timezone", "America/Chicago" }, { "default_language", "de" },
                { "twenty_four_hour_time", "false" }, { "enter_sends", "true" },
                { "enable_desktop_notifications", "false" }, { "enable_offline_email_notifications", "true" },
                { "enable_offline_push_notifications", "false" }, { "enable_online_push_notifications", "true" },
                { "enable_stream_desktop_notifications", "true" }, { "enable_stream_email_notifications", "false" },
                { "enable_stream_push_notifications", "true" }, { "enable_digest_emails", "false" }
            }, await Utils.ReadForm(requests[0]));
            Assert.Equal(new string[] { "future" }, client.LastResponse!.IgnoredParametersUnsupported);
            await client.Users.UpdateSettings(new UserSettingsOptions());
            Assert.Empty(await Utils.ReadForm(requests[1]));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Patch, request.Method);
                Assert.Equal("/api/v1/settings", request.RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Profile updates preserve actual string/null/user-list types rather than encoding everything as strings.</summary>
        [Fact]
        public async Task Users_ProfileData_ActualValueTypesAndEmptyUpdates()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            ProfileDataInput[] updates =
            [
                new ProfileDataInput(4, "0"),
                new ProfileDataInput(5, "1909-04-05"),
                new ProfileDataInput(6, (string?)null),
                new ProfileDataInput(7, new int[] { 12, 19 }),
                new ProfileDataInput(8, "A + \"雪\""),
                new ProfileDataInput(9, Array.Empty<int>())
            ];
            (bool success, string? details) result = await client.Users.TryUpdateProfileData(updates);
            Assert.True(result.success, result.details);
            Dictionary<string, string> form = await Utils.ReadForm(requests[0]);
            Assert.Single(form);
            using JsonDocument document = JsonDocument.Parse(form["data"]);
            JsonElement fields = document.RootElement;
            Assert.Equal(6, fields.GetArrayLength());
            Assert.Equal(4, fields[0].GetProperty("id").GetInt32());
            Assert.Equal("0", fields[0].GetProperty("value").GetString());
            Assert.Equal("1909-04-05", fields[1].GetProperty("value").GetString());
            Assert.Equal(JsonValueKind.Null, fields[2].GetProperty("value").ValueKind);
            Assert.Equal(new int[] { 12, 19 }, fields[3].GetProperty("value").Deserialize<int[]>());
            Assert.Equal("A + \"雪\"", fields[4].GetProperty("value").GetString());
            Assert.Equal(0, fields[5].GetProperty("value").GetArrayLength());
            await client.Users.UpdateProfileData([]);
            Assert.Equal("[]", (await Utils.ReadForm(requests[1]))["data"]);
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Patch, request.Method);
                Assert.Equal("/api/v1/users/me/profile_data", request.RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Removal sends an explicit array of field IDs, including a safe empty list.</summary>
        [Fact]
        public async Task Users_ProfileDataRemoval_ActualDeleteBody()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            Assert.True((await client.Users.TryRemoveProfileData([4, 5])).success);
            await client.Users.RemoveProfileData([]);
            Assert.Equal(new Dictionary<string, string> { { "data", "[4,5]" } }, await Utils.ReadForm(requests[0]));
            Assert.Equal(new Dictionary<string, string> { { "data", "[]" } }, await Utils.ReadForm(requests[1]));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Delete, request.Method);
                Assert.Equal("/api/v1/users/me/profile_data", request.RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Server restrictions are explicit failures for every preference/profile mutation and its throwing pair.</summary>
        /// <param name="operation">The mutation being exercised.</param>
        [Theory]
        [InlineData("settings")]
        [InlineData("update")]
        [InlineData("remove")]
        public async Task Users_SettingsAndProfile_ServerErrorsAndThrowingPairs(string operation)
        {
            const string json = "{\"result\":\"error\",\"code\":\"BAD_REQUEST\",\"msg\":\"You are not allowed to change this field.\"}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details) result = operation switch
            {
                "settings" => await client.Users.TryUpdateSettings(new UserSettingsOptions { Timezone = "invalid" }),
                "update" => await client.Users.TryUpdateProfileData([new ProfileDataInput(4, "value")]),
                _ => await client.Users.TryRemoveProfileData([4])
            };
            Assert.False(result.success);
            Assert.Contains("You are not allowed", result.details);
            Exception error = await Assert.ThrowsAsync<Exception>(() => operation switch
            {
                "settings" => client.Users.UpdateSettings(new UserSettingsOptions { Timezone = "invalid" }),
                "update" => client.Users.UpdateProfileData([new ProfileDataInput(4, "value")]),
                _ => client.Users.RemoveProfileData([4])
            });
            Assert.Contains("You are not allowed", error.Message);
        }

        /// <summary>Invalid selected fields fail before sending; throwing pairs retain the same input diagnostics.</summary>
        [Fact]
        public async Task Users_SettingsAndProfile_RejectInvalidInputsWithoutRequest()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            Assert.False((await client.Users.TryUpdateSettings(null!)).success);
            Assert.False((await client.Users.TryUpdateProfileData(null!)).success);
            Assert.False((await client.Users.TryUpdateProfileData([null!])).success);
            Assert.False((await client.Users.TryUpdateProfileData([new ProfileDataInput(0, "value")])).success);
            Assert.False((await client.Users.TryUpdateProfileData([new ProfileDataInput(4, (int[])null!)])).success);
            Assert.False((await client.Users.TryUpdateProfileData([new ProfileDataInput(4, new int[] { -1 })])).success);
            Assert.False((await client.Users.TryRemoveProfileData(null!)).success);
            Assert.False((await client.Users.TryRemoveProfileData([0])).success);
            await Assert.ThrowsAsync<Exception>(() => client.Users.UpdateSettings(null!));
            await Assert.ThrowsAsync<Exception>(() => client.Users.UpdateProfileData([null!]));
            await Assert.ThrowsAsync<Exception>(() => client.Users.RemoveProfileData([-1]));
            Assert.Empty(requests);
        }

        /// <summary>Injected settings delegates need not set transport Success, but an unsuccessful HTTP status still fails.</summary>
        [Fact]
        public async Task Users_Settings_InjectedEnvelopeValidation()
        {
            Queue<ZulipResponse> responses = new Queue<ZulipResponse>(
            [
                new ZulipResponse { Result = "success", Success = false },
                new ZulipResponse { Result = "success", HttpResponseCode = 503 }
            ]);
            Users users = new Users((method, endpoint, data) => Task.FromResult(responses.Dequeue()));
            Assert.True((await users.TryUpdateSettings(new UserSettingsOptions { EnterSends = false })).success);
            Assert.False((await users.TryUpdateSettings(new UserSettingsOptions { EnterSends = false })).success);
        }

        /// <summary>Client-wired upload consumes the binary file field and leaves the caller's stream open.</summary>
        [Fact]
        public async Task Users_AvatarUpload_ActualMultipartContractAndOwnership()
        {
            byte[] bytes = [0, 255, 13, 10, 42];
            using MemoryStream input = new MemoryStream(bytes);
            byte[]? received = null;
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, token) =>
                {
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Equal("/api/v1/users/me/avatar", request.RequestUri!.AbsolutePath);
                    MultipartFormDataContent multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
                    HttpContent file = Assert.Single(multipart);
                    Assert.Equal("file", file.Headers.ContentDisposition!.Name);
                    Assert.Equal("profile \"雪\".png", file.Headers.ContentDisposition.FileNameStar);
                    Assert.Equal("image/png", file.Headers.ContentType!.MediaType);
                    received = await file.ReadAsByteArrayAsync(token);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = Utils.ContentForJsonString("{\"result\":\"success\",\"avatar_url\":\"/user_avatars/2/new.png\"}")
                    };
                });
            using HttpClient http = new HttpClient(handler.Object);
            ZulipClient client = new ZulipClient("https://zulip.example.org", "user@example.org", "test-key", http);
            string url = await client.Users.UploadAvatar(input, "profile \"雪\".png", "image/png", TestContext.Current.CancellationToken);
            Assert.Equal("/user_avatars/2/new.png", url);
            Assert.Equal(bytes, received);
            Assert.True(input.CanRead);
            Assert.Equal(input.Length, input.Position);
        }

        /// <summary>The focused avatar delegate receives the caller stream, token and default MIME type without pre-reading.</summary>
        [Fact]
        public async Task Users_AvatarUpload_InjectedDelegateAndMissingDelegate()
        {
            using MemoryStream input = new MemoryStream([0, 255]);
            CancellationToken token = TestContext.Current.CancellationToken;
            bool uploaded = false;
            Users users = new Users((method, endpoint, data) => throw new InvalidOperationException("No ordinary request expected"),
                (endpoint, field, stream, filename, mime, actualToken) =>
                {
                    uploaded = true;
                    Assert.Equal("api/v1/users/me/avatar", endpoint);
                    Assert.Equal("file", field);
                    Assert.Same(input, stream);
                    Assert.Equal(0, input.Position);
                    Assert.Equal("avatar.png", filename);
                    Assert.Equal("application/octet-stream", mime);
                    Assert.Equal(token, actualToken);
                    return Task.FromResult(new ZulipResponse
                    {
                        Result = "success",
                        HttpResponseBody = "{\"result\":\"success\",\"avatar_url\":\"/user_avatars/new.png\"}"
                    });
                });
            (bool success, string? details, string? avatarUrl) result =
                await users.TryUploadAvatar(input, "avatar.png", cancellationToken: token);
            Assert.True(result.success, result.details);
            Assert.Equal("/user_avatars/new.png", result.avatarUrl);
            Assert.True(uploaded);
            Users unconfigured = new Users((method, endpoint, data) => throw new InvalidOperationException("No ordinary request expected"));
            (bool success, string? details, string? avatarUrl) unavailable =
                await unconfigured.TryUploadAvatar(input, "avatar.png", cancellationToken: token);
            Assert.False(unavailable.success);
            Assert.Contains("multipart upload delegate", unavailable.details);
            Assert.Null(unavailable.avatarUrl);
            await Assert.ThrowsAsync<Exception>(() => unconfigured.UploadAvatar(input, "avatar.png", cancellationToken: token));
            Assert.True(input.CanRead);
            Assert.Equal(0, input.Position);
        }

        /// <summary>Avatar deletion has no form fields and retains both server-relative and absolute default-picture URLs.</summary>
        [Fact]
        public async Task Users_AvatarDeletion_ActualUrlAndNoParameters()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"avatar_url\":\"/user_avatars/default.png\"}",
                "{\"result\":\"success\",\"avatar_url\":\"https://secure.gravatar.com/avatar/hash\"}");
            (bool success, string? details, string? avatarUrl) result = await client.Users.TryDeleteAvatar();
            Assert.True(result.success, result.details);
            Assert.Equal("/user_avatars/default.png", result.avatarUrl);
            Assert.Equal("https://secure.gravatar.com/avatar/hash", await client.Users.DeleteAvatar());
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Delete, request.Method);
                Assert.Equal("/api/v1/users/me/avatar", request.RequestUri!.AbsolutePath);
                Assert.Empty(await Utils.ReadForm(request));
            }
        }

        /// <summary>Missing/incorrect picture URLs and server image restrictions never produce a successful default value.</summary>
        /// <param name="json">The actual response envelope.</param>
        /// <param name="expected">The expected diagnostic.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}", "avatar_url")]
        [InlineData("{\"result\":\"success\",\"url\":\"/user_avatars/wrong.png\"}", "avatar_url")]
        [InlineData("{\"result\":\"success\",\"avatar_url\":null}", "avatar_url")]
        [InlineData("{\"result\":\"success\",\"avatar_url\":42}", "avatar_url")]
        [InlineData("{\"result\":\"success\",\"avatar_url\":\"\"}", "avatar_url")]
        [InlineData("{\"result\":\"error\",\"code\":\"BAD_IMAGE\",\"msg\":\"Invalid image format\"}", "Invalid image format")]
        [InlineData("{\"result\":\"error\",\"msg\":\"Avatar changes are disabled in this organization.\"}", "Avatar changes are disabled")]
        public async Task Users_Avatars_RequireRealUrlsAndThrow(string json, string expected)
        {
            using MemoryStream input = new MemoryStream([0, 255]);
            ZulipClient client = Utils.CreateCapturingClient([], json, json, json, json);
            (bool success, string? details, string? avatarUrl) uploaded =
                await client.Users.TryUploadAvatar(input, "avatar.png", cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(uploaded.success);
            Assert.Contains(expected, uploaded.details);
            Assert.Null(uploaded.avatarUrl);
            Exception uploadError = await Assert.ThrowsAsync<Exception>(() =>
                client.Users.UploadAvatar(input, "avatar.png", cancellationToken: TestContext.Current.CancellationToken));
            Assert.Contains(expected, uploadError.Message);
            (bool success, string? details, string? avatarUrl) deleted = await client.Users.TryDeleteAvatar();
            Assert.False(deleted.success);
            Assert.Contains(expected, deleted.details);
            Assert.Null(deleted.avatarUrl);
            Exception deleteError = await Assert.ThrowsAsync<Exception>(() => client.Users.DeleteAvatar());
            Assert.Contains(expected, deleteError.Message);
            Assert.True(input.CanRead);
        }

        /// <summary>Unreadable streams and invalid multipart headers are rejected without reading or sending.</summary>
        [Fact]
        public async Task Users_AvatarUpload_RejectsInvalidInputsWithoutRequest()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            using MemoryStream input = new MemoryStream([0, 255]);
            Assert.False((await client.Users.TryUploadAvatar(input, "", cancellationToken: TestContext.Current.CancellationToken)).success);
            Assert.False((await client.Users.TryUploadAvatar(input, "bad\r\nname.png", cancellationToken: TestContext.Current.CancellationToken)).success);
            Assert.False((await client.Users.TryUploadAvatar(input, "good.png", "image/png\r\nInjected: yes", TestContext.Current.CancellationToken)).success);
            Assert.False((await client.Users.TryUploadAvatar(null!, "good.png", cancellationToken: TestContext.Current.CancellationToken)).success);
            MemoryStream unreadable = new MemoryStream();
            unreadable.Dispose();
            Assert.False((await client.Users.TryUploadAvatar(unreadable, "good.png", cancellationToken: TestContext.Current.CancellationToken)).success);
            await Assert.ThrowsAsync<Exception>(() => client.Users.UploadAvatar(input, "good.png", "not a MIME type", TestContext.Current.CancellationToken));
            Assert.Empty(requests);
            Assert.Equal(0, input.Position);
            Assert.True(input.CanRead);
        }
    }
}
