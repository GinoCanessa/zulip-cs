using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>Explicit API-key lifecycle and unauthenticated JWT bootstrap contracts.</summary>
    public class CredentialTests
    {
        /// <summary>Key regeneration returns the new key but leaves subsequent client headers explicitly unchanged.</summary>
        [Fact]
        public async Task Credentials_OwnRotationDoesNotImplicitlyReplaceAuthentication()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"api_key\":\"rotated-key\"}",
                "{\"result\":\"error\",\"msg\":\"Old key rejected\"}");
            string key = await client.Users.RegenerateOwnApiKey();
            Assert.Equal("rotated-key", key);
            Assert.Equal("/api/v1/users/me/api_key/regenerate", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal(HttpMethod.Post, requests[0].Method);
            Assert.False((await client.Users.TryGetOwnUser()).success);
            Assert.Equal(requests[0].Headers.Authorization, requests[1].Headers.Authorization);
            Assert.DoesNotContain("rotated-key", requests[1].Headers.Authorization!.ToString());
        }

        /// <summary>Bot key read/rotation use their distinct actual contracts and preserve permission errors.</summary>
        [Fact]
        public async Task Credentials_BotReadAndRotationReturnActualKeys()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"api_key\":\"bot-old\"}",
                "{\"result\":\"success\",\"api_key\":\"bot-new\"}",
                "{\"result\":\"error\",\"msg\":\"Must be the bot owner\"}");
            Assert.Equal("bot-old", await client.Users.GetBotApiKey(17));
            Assert.Equal("bot-new", await client.Users.RegenerateBotApiKey(17));
            var denied = await client.Users.TryGetBotApiKey(18);
            Assert.False(denied.success);
            Assert.Contains("Must be the bot owner", denied.details);
            Assert.Equal(HttpMethod.Get, requests[0].Method);
            Assert.Equal("/api/v1/bots/17/api_key", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal(HttpMethod.Post, requests[1].Method);
            Assert.Equal("/api/v1/bots/17/api_key/regenerate", requests[1].RequestUri!.AbsolutePath);
        }

        /// <summary>Missing/empty/wrong credential data never becomes successful null or a silently unusable client.</summary>
        /// <param name="json">The failed or malformed response.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}")]
        [InlineData("{\"result\":\"success\",\"api_key\":\"\"}")]
        [InlineData("{\"result\":\"success\",\"api_key\":false}")]
        [InlineData("{\"result\":\"error\",\"msg\":\"Permission denied\"}")]
        public async Task Credentials_RequiredKeysAndErrorsAreExplicit(string json)
        {
            ZulipClient client = Utils.CreateCapturingClient([], json, json, json);
            var own = await client.Users.TryRegenerateOwnApiKey();
            Assert.False(own.success);
            Assert.Null(own.apiKey);
            Assert.Contains("may already have been rotated", own.details);
            Assert.False((await client.Users.TryGetBotApiKey(17)).success);
            await Assert.ThrowsAsync<Exception>(() => client.Users.RegenerateBotApiKey(17));
        }

        /// <summary>JWT bootstrap submits only form credentials, validates optional requested profile and preserves caller HTTP ownership.</summary>
        /// <param name="includeProfile">Whether the optional profile parameter is omitted, false or true.</param>
        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Credentials_JwtUsesFormAndRequestedProfile(bool? includeProfile)
        {
            const string token = "test.jwt + &token";
            Dictionary<string, string>? fields = null;
            int calls = 0;
            using HttpClient http = new HttpClient(new Handler(async (request, cancellation) =>
            {
                calls++;
                if (calls == 1)
                {
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Equal("/api/v1/jwt/fetch_api_key", request.RequestUri!.AbsolutePath);
                    Assert.Empty(request.RequestUri.Query);
                    Assert.Null(request.Headers.Authorization);
                    fields = Utils.DecodeForm(await request.Content!.ReadAsStringAsync(cancellation));
                }
                return Json("{\"result\":\"success\",\"api_key\":\"jwt-key\",\"email\":\"jwt@example.org\"," +
                    "\"user\":{\"user_id\":17,\"email\":\"jwt@example.org\",\"full_name\":\"JWT User\"}," +
                    "\"ignored_parameters_unsupported\":[\"future\"]}");
            }));
            ApiKeyResult credentials = await ZulipClient.FetchApiKeyJwt("https://example.org", token, includeProfile, http,
                TestContext.Current.CancellationToken);
            Assert.Equal("jwt-key", credentials.ApiKey);
            Assert.Equal("jwt@example.org", credentials.Email);
            Assert.Equal(token, fields!["token"]);
            if (includeProfile.HasValue)
                Assert.Equal(includeProfile.Value ? "true" : "false", fields["include_profile"]);
            else
                Assert.False(fields.ContainsKey("include_profile"));
            if (includeProfile == true) Assert.Equal(17, credentials.User!.UserId);
            else Assert.Null(credentials.User);
            Assert.Equal(200, credentials.Response.HttpResponseCode);
            Assert.Contains("future", credentials.Response.IgnoredParametersUnsupported!);
            Assert.DoesNotContain("jwt-key", credentials.ToString());
            using HttpResponseMessage second = await http.GetAsync("https://example.org/", TestContext.Current.CancellationToken);
            Assert.Equal(2, calls);
        }

        /// <summary>JWT cannot accept missing identity/profile or success-shaped HTTP errors, and never echoes the submitted token.</summary>
        /// <param name="json">The returned JSON.</param>
        /// <param name="status">The HTTP status.</param>
        /// <param name="profile">Whether a profile is required.</param>
        [Theory]
        [InlineData("{\"result\":\"success\",\"email\":\"a@example.org\"}", 200, false)]
        [InlineData("{\"result\":\"success\",\"api_key\":\"new-key\"}", 200, false)]
        [InlineData("{\"result\":\"success\",\"api_key\":\"new-key\",\"email\":\"a@example.org\"}", 200, true)]
        [InlineData("{\"result\":\"success\",\"api_key\":\"new-key\",\"email\":\"a@example.org\"}", 401, false)]
        [InlineData("{\"result\":\"error\",\"msg\":\"JWT disabled: submitted-test-token\"}", 400, false)]
        [InlineData("{\"result\":\"error\",\"api_key\":\"new-key\",\"msg\":\"Rejected new-key submitted-test-token\"}", 400, false)]
        [InlineData("not JSON submitted-test-token", 200, false)]
        public async Task Credentials_JwtFailuresValidateAndRedact(string json, int status, bool profile)
        {
            using HttpClient http = new HttpClient(new Handler((_, _) => Task.FromResult(Json(json, status))));
            var result = await ZulipClient.TryFetchApiKeyJwt("https://example.org", "submitted-test-token", profile, http,
                TestContext.Current.CancellationToken);
            Assert.False(result.success);
            Assert.Null(result.credentials);
            Assert.DoesNotContain("submitted-test-token", result.details);
            Assert.DoesNotContain("new-key", result.details);
            await Assert.ThrowsAsync<Exception>(() => ZulipClient.FetchApiKeyJwt(
                "https://example.org", "submitted-test-token", profile, http, TestContext.Current.CancellationToken));
        }

        /// <summary>Injected authentication is rejected without mutating headers or sending the JWT under unrelated credentials.</summary>
        [Fact]
        public async Task Credentials_JwtRejectsDefaultAuthentication()
        {
            int calls = 0;
            using HttpClient http = new HttpClient(new Handler((_, _) =>
            {
                calls++;
                return Task.FromResult(Json("{}"));
            }));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "test");
            var result = await ZulipClient.TryFetchApiKeyJwt("https://example.org", "test-token",
                httpClient: http, cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(result.success);
            Assert.Equal(0, calls);
            Assert.Equal("Basic test", http.DefaultRequestHeaders.Authorization.ToString());
        }

        /// <summary>JWT cancellation remains an explicit Try failure and caller HTTP ownership is unchanged.</summary>
        [Fact]
        public async Task Credentials_JwtCancellationIsExplicit()
        {
            using HttpClient http = new HttpClient(new Handler(async (_, cancellation) =>
            {
                await Task.Delay(Timeout.Infinite, cancellation);
                return Json("{}");
            }));
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var result = await ZulipClient.TryFetchApiKeyJwt("https://example.org", "test-token",
                httpClient: http, cancellationToken: cancellation.Token);
            Assert.False(result.success);
            Assert.Contains("canceled", result.details);
        }

        /// <summary>Creates a fresh test-only HTTP response.</summary>
        /// <param name="json">The JSON body.</param>
        /// <param name="status">The HTTP status.</param>
        /// <returns>The disposable response.</returns>
        private static HttpResponseMessage Json(string json, int status = 200) =>
            new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        /// <summary>A deterministic local credential request handler.</summary>
        private sealed class Handler : HttpMessageHandler
        {
            /// <summary>The asynchronous callback.</summary>
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _callback;

            /// <summary>Initializes the handler.</summary>
            /// <param name="callback">The asynchronous request callback.</param>
            internal Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) => _callback = callback;

            /// <summary>Invokes the deterministic local callback.</summary>
            /// <param name="request">The HTTP request.</param>
            /// <param name="cancellationToken">Caller cancellation.</param>
            /// <returns>The local response.</returns>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                _callback(request, cancellationToken);
        }
    }
}
