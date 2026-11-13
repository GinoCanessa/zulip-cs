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
    /// <summary>Tests for Server resource.</summary>
    public class ServerTests
    {
        /// <summary>Requires the version and feature level used for feature gating.</summary>
        /// <param name="fields">The response fields.</param>
        /// <param name="expectedSuccess">Whether both fields are valid.</param>
        [Theory]
        [InlineData("", false)]
        [InlineData(",\"zulip_version\":\"12.0\"", false)]
        [InlineData(",\"zulip_feature_level\":513", false)]
        [InlineData(",\"zulip_version\":null,\"zulip_feature_level\":513", false)]
        [InlineData(",\"zulip_version\":\"12.0\",\"zulip_feature_level\":\"513\"", false)]
        [InlineData(",\"zulip_version\":\"12.0\",\"zulip_feature_level\":0", true)]
        public async Task Server_GetSettings_ValidatesFeatureGate(string fields, bool expectedSuccess)
        {
            ZulipClient client = Utils.CreateCapturingClient([], $"{{\"result\":\"success\"{fields}}}");
            (bool success, string? details, string? zulipVersion, int? featureLevel) actual =
                await client.Server.TryGetSettings();
            Assert.Equal(expectedSuccess, actual.success);
            if (expectedSuccess)
            {
                Assert.Equal("12.0", actual.zulipVersion);
                Assert.Equal(0, actual.featureLevel);
            }
            else
            {
                Assert.Null(actual.zulipVersion);
                Assert.Null(actual.featureLevel);
                Assert.Contains("Server.GetSettings failed:", actual.details);
            }
        }

        [Fact]
        public async Task Server_GetSettings_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"zulip_version\":\"9.0\",\"zulip_feature_level\":310}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK,
                content,
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            var actual = await zulipClient.Server.TryGetSettings();

            Assert.True(actual.success, $"TryGetSettings failed: {actual.details}");
            Assert.Equal("9.0", actual.zulipVersion);
            Assert.Equal(310, actual.featureLevel);
        }

        [Fact]
        public async Task Server_GetSettings_Error()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"error\",\"msg\":\"Unauthorized\",\"code\":\"UNAUTHORIZED\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.Unauthorized,
                content,
                out Mock<HttpMessageHandler> mockMessageHandler,
                out HttpClient mockHttpClient,
                out ZulipClient zulipClient);

            Assert.True(success, "Failed to get mocked ZulipClient");

            var actual = await zulipClient.Server.TryGetSettings();

            Assert.False(actual.success);
            Assert.False(string.IsNullOrEmpty(actual.details));
        }
    }
}
