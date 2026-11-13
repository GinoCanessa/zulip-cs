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
    /// <summary>Tests for NavigationViews resource.</summary>
    public class NavigationViewTests
    {
        [Fact]
        public async Task NavigationViews_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                """
                {"result":"success","msg":"","navigation_views":[
                  {"fragment":"narrow/channel/42-general/topic/test","is_pinned":true,"name":"My View"},
                  {"fragment":"all_messages","is_pinned":false}
                ]}
                """);

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            (bool success, string? details, List<NavigationViewObject>? views) actual =
                await zulipClient.NavigationViews.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Collection(actual.views!,
                view =>
                {
                    Assert.Equal("narrow/channel/42-general/topic/test", view.Fragment);
                    Assert.True(view.IsPinned);
                    Assert.Equal("My View", view.Name);
                },
                view =>
                {
                    Assert.Equal("all_messages", view.Fragment);
                    Assert.False(view.IsPinned);
                    Assert.Null(view.Name);
                });
        }

        [Fact]
        public async Task NavigationViews_Create_Success()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient zulipClient = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}");
            const string fragment = "narrow/channel/42-general/topic/why?#%+\u00e9";
            (bool success, string? details) actual =
                await zulipClient.NavigationViews.TryCreate(fragment, true, "New \"View\" \u00e9");
            Assert.True(actual.success, actual.details);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://zulip.example.org/api/v1/navigation_views", request.RequestUri!.AbsoluteUri);
            Dictionary<string, string> form = await Utils.ReadForm(request);
            Assert.Equal(fragment, form["fragment"]);
            Assert.Equal("true", form["is_pinned"]);
            Assert.Equal("New \"View\" \u00e9", form["name"]);
        }

        [Fact]
        public async Task NavigationViews_Update_Success()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient zulipClient = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}");
            (bool success, string? details) actual = await zulipClient.NavigationViews.TryUpdate(
                "narrow/channel/42-general/topic/why?#%+\u00e9", isPinned: false);
            Assert.True(actual.success, actual.details);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Patch, request.Method);
            Assert.Equal("https://zulip.example.org/api/v1/navigation_views/narrow/channel/42-general/topic/why%3F%23%25%2B%C3%A9",
                request.RequestUri!.AbsoluteUri);
            Assert.Empty(request.RequestUri.Query);
            Assert.Empty(request.RequestUri.Fragment);
            Assert.Equal(new Dictionary<string, string> { { "is_pinned", "false" } }, await Utils.ReadForm(request));
        }

        [Fact]
        public async Task NavigationViews_Delete_Success()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient zulipClient = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}");
            (bool success, string? details) actual = await zulipClient.NavigationViews.TryDelete(
                "narrow/channel/%2E%2E/topic/a?b#c%+\u96ea");
            Assert.True(actual.success, actual.details);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("https://zulip.example.org/api/v1/navigation_views/narrow/channel/%252E%252E/topic/a%3Fb%23c%25%2B%E9%9B%AA",
                request.RequestUri!.AbsoluteUri);
            Assert.Null(request.Content);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"fragment\":\"all_messages\"}")]
        [InlineData("{\"is_pinned\":true}")]
        [InlineData("{\"fragment\":\"all_messages\",\"is_pinned\":\"true\"}")]
        public async Task NavigationViews_GetAll_RejectsMissingOrInvalidIdentity(string view)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"navigation_views\":[" + view + "]}");
            (bool success, string? details, List<NavigationViewObject>? views) actual = await client.NavigationViews.TryGetAll();
            Assert.False(actual.success);
            Assert.Null(actual.views);
            Assert.False(string.IsNullOrWhiteSpace(actual.details));
        }

        [Theory]
        [InlineData("{\"result\":\"success\"}", false)]
        [InlineData("{\"result\":\"success\",\"navigation_views\":null}", false)]
        [InlineData("{\"result\":\"success\",\"navigation_views\":[]}", true)]
        public async Task NavigationViews_GetAll_DistinguishesMissingAndEmptyCollections(string json, bool expectedSuccess)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json);
            (bool success, string? details, List<NavigationViewObject>? views) actual = await client.NavigationViews.TryGetAll();
            Assert.Equal(expectedSuccess, actual.success);
            if (expectedSuccess) Assert.Empty(actual.views!);
            else Assert.Contains("navigation_views", actual.details);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("/all_messages")]
        [InlineData("narrow/")]
        [InlineData("narrow//channel")]
        [InlineData(".")]
        [InlineData("narrow/../all_messages")]
        [InlineData("narrow/./channel")]
        public async Task NavigationViews_InvalidFragmentsNeverSend(string fragment)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            Assert.False((await client.NavigationViews.TryCreate(fragment, true)).success);
            Assert.False((await client.NavigationViews.TryUpdate(fragment, isPinned: true)).success);
            Assert.False((await client.NavigationViews.TryDelete(fragment)).success);
            Assert.Empty(requests);
        }

        [Fact]
        public async Task NavigationViews_LegacySignaturesAndEmptyUpdatesNeverSend()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
#pragma warning disable CS0618
            (bool success, string? details) created = await client.NavigationViews.TryCreate("name only");
            (bool success, string? details) updated = await client.NavigationViews.TryUpdate(42, "name");
            (bool success, string? details) deleted = await client.NavigationViews.TryDelete(42);
            Assert.False(created.success);
            Assert.Contains("fragment", created.details);
            Assert.False(updated.success);
            Assert.Contains("fragment", updated.details);
            Assert.False(deleted.success);
            Assert.Contains("fragment", deleted.details);
            await Assert.ThrowsAsync<Exception>(() => client.NavigationViews.Create("name only"));
            await Assert.ThrowsAsync<Exception>(() => client.NavigationViews.Update(42));
            await Assert.ThrowsAsync<Exception>(() => client.NavigationViews.Delete(42));
#pragma warning restore CS0618
            Assert.False((await client.NavigationViews.TryUpdate("all_messages")).success);
            Assert.Empty(requests);
        }

        [Fact]
        public async Task NavigationViews_ThrowingPairsUseFragmentIdentityAndOptionalName()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\"}", "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            await client.NavigationViews.Create("all_messages", false);
            await client.NavigationViews.Update("all_messages", "Everything");
            await client.NavigationViews.Delete("all_messages");
            Assert.Equal(3, requests.Count);
            Dictionary<string, string> creation = await Utils.ReadForm(requests[0]);
            Assert.Equal("all_messages", creation["fragment"]);
            Assert.Equal("false", creation["is_pinned"]);
            Assert.False(creation.ContainsKey("name"));
            Assert.Equal(new Dictionary<string, string> { { "name", "Everything" } }, await Utils.ReadForm(requests[1]));
            Assert.Equal(HttpMethod.Delete, requests[2].Method);
            Assert.EndsWith("/navigation_views/all_messages", requests[2].RequestUri!.AbsoluteUri);
        }
    }
}
