using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for Drafts resource.</summary>
    public class DraftTests
    {
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
            Assert.Single(actual.drafts);
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
