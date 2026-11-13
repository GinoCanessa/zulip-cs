using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Models;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for Drafts resource.</summary>
    public class DraftTests
    {
        /// <summary>Distinguishes valid empty drafts from missing or malformed required data.</summary>
        /// <param name="fields">The response fields.</param>
        /// <param name="expectedSuccess">Whether the actual payload is valid.</param>
        [Theory]
        [InlineData("", false)]
        [InlineData(",\"drafts\":null", false)]
        [InlineData(",\"drafts\":{}", false)]
        [InlineData(",\"drafts\":[{}]", false)]
        [InlineData(",\"drafts\":[]", true)]
        public async Task Drafts_GetAll_ValidatesPayload(string fields, bool expectedSuccess)
        {
            ZulipClient client = Utils.CreateCapturingClient([], $"{{\"result\":\"success\"{fields}}}");
            (bool success, string? details, List<DraftObject>? drafts) actual = await client.Drafts.TryGetAll();
            Assert.Equal(expectedSuccess, actual.success);
            if (expectedSuccess) Assert.Empty(actual.drafts!);
            else
            {
                Assert.Null(actual.drafts);
                Assert.Contains("Drafts.GetAll failed:", actual.details);
            }
        }

        /// <summary>Endpoint-only mutations also reject parse failures and failed HTTP statuses.</summary>
        /// <param name="json">The response body.</param>
        /// <param name="status">The HTTP status.</param>
        [Theory]
        [InlineData("{\"result\":\"success\",\"drafts\":\"invalid\"}", HttpStatusCode.OK)]
        [InlineData("{\"result\":\"success\"}", HttpStatusCode.BadGateway)]
        public async Task Drafts_Create_RejectsResponseFailure(string json, HttpStatusCode status)
        {
            Assert.True(Utils.TryGetMockedClient(status, Utils.ContentForJsonString(json),
                out _, out _, out ZulipClient client));
            (bool success, string? details) actual = await client.Drafts.TryCreate("[]");
            Assert.False(actual.success);
            Assert.Contains("Drafts.Create failed:", actual.details);
        }

        [Fact]
        public async Task Drafts_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"drafts\":[{\"id\":1,\"type\":\"channel\",\"to\":[10],\"topic\":\"test\",\"content\":\"Draft message\",\"timestamp\":1700000000}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Drafts.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.drafts!);
        }

        [Fact]
        public async Task Drafts_Create_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Drafts.TryCreate("[{\"type\":\"channel\",\"to\":[10],\"topic\":\"test\",\"content\":\"Draft\"}]");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Drafts_Edit_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Drafts.TryEdit(1, "{\"type\":\"channel\",\"to\":[10],\"topic\":\"updated\",\"content\":\"Updated draft\"}");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Drafts_Delete_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Drafts.TryDelete(1);
            Assert.True(actual.success, actual.details);
        }
    }
}
