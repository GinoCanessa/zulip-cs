using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for SavedSnippets resource.</summary>
    public class SavedSnippetTests
    {
        [Fact]
        public async Task SavedSnippets_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"saved_snippets\":[{\"id\":1,\"title\":\"Greeting\",\"content\":\"Hello!\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.SavedSnippets.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.snippets);
        }

        [Fact]
        public async Task SavedSnippets_Create_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.SavedSnippets.TryCreate("Greeting", "Hello there!");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task SavedSnippets_Edit_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.SavedSnippets.TryEdit(1, title: "Updated Greeting");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task SavedSnippets_Delete_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.SavedSnippets.TryDelete(1);
            Assert.True(actual.success, actual.details);
        }
    }
}
