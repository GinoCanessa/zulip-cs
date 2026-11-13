using System;
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
    /// <summary>Tests for SavedSnippets resource.</summary>
    public class SavedSnippetTests
    {
        /// <summary>Does not replace missing snippet data with a successful empty result.</summary>
        /// <param name="fields">The response fields.</param>
        /// <param name="expectedSuccess">Whether the payload is valid.</param>
        [Theory]
        [InlineData("", false)]
        [InlineData(",\"saved_snippets\":null", false)]
        [InlineData(",\"saved_snippets\":{}", false)]
        [InlineData(",\"saved_snippets\":[{}]", false)]
        [InlineData(",\"saved_snippets\":[]", true)]
        public async Task SavedSnippets_GetAll_ValidatesPayload(string fields, bool expectedSuccess)
        {
            ZulipClient client = Utils.CreateCapturingClient([], $"{{\"result\":\"success\"{fields}}}");
            (bool success, string? details, List<SavedSnippetObject>? snippets) actual = await client.SavedSnippets.TryGetAll();
            Assert.Equal(expectedSuccess, actual.success);
            if (expectedSuccess) Assert.Empty(actual.snippets!);
            else
            {
                Assert.Null(actual.snippets);
                Assert.Contains("SavedSnippets.GetAll failed:", actual.details);
            }
        }

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
            Assert.Single(actual.snippets!);
        }

        [Fact]
        public async Task SavedSnippets_Create_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"saved_snippet_id\":42}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.SavedSnippets.TryCreate("Greeting", "Hello there!");
            Assert.True(actual.success, actual.details);
        }

        /// <summary>Creation exposes the actual snippet identity and preserves form content.</summary>
        [Fact]
        public async Task SavedSnippets_CreateWithId_ReturnsIdentity()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\",\"saved_snippet_id\":42}");
            Assert.Equal(42, await client.SavedSnippets.CreateWithId("Greeting", "\"hello\" & welcome"));
            Dictionary<string, string> form = await Utils.ReadForm(Assert.Single(requests));
            Assert.Equal("Greeting", form["title"]);
            Assert.Equal("\"hello\" & welcome", form["content"]);
        }

        /// <summary>Missing or malformed identities cannot masquerade as completed creation.</summary>
        /// <param name="fields">Returned identity fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"id\":42")]
        [InlineData(",\"saved_snippet_id\":null")]
        [InlineData(",\"saved_snippet_id\":\"42\"")]
        public async Task SavedSnippets_CreateWithId_RequiresIdentity(string fields)
        {
            string json = $"{{\"result\":\"success\"{fields}}}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            var actual = await client.SavedSnippets.TryCreateWithId("title", "content");
            Assert.False(actual.success);
            Assert.Contains("saved_snippet_id", actual.details);
            await Assert.ThrowsAsync<Exception>(() => client.SavedSnippets.Create("title", "content"));
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
