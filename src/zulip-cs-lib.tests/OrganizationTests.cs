using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for Organization resource.</summary>
    public class OrganizationTests
    {
        [Fact]
        public async Task Organization_GetLinkifiers_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"linkifiers\":[{\"id\":1,\"pattern\":\"#(?P<id>[0-9]+)\",\"url_template\":\"https://example.org/{id}\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Organization.TryGetLinkifiers();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.linkifiers);
        }

        [Fact]
        public async Task Organization_AddLinkifier_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"filter_id\":42}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Organization.TryAddLinkifier("#(?P<id>[0-9]+)", "https://example.org/{id}");
            Assert.True(actual.success, actual.details);
            Assert.Equal(42, actual.filterId);
        }

        [Fact]
        public async Task Organization_UpdateLinkifier_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Organization.TryUpdateLinkifier(42, "#(?P<id>[0-9]+)", "https://new.example.org/{id}");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Organization_RemoveLinkifier_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Organization.TryRemoveLinkifier(42);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Organization_GetCustomEmoji_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"emoji\":{\"smile\":{\"id\":\"1\",\"name\":\"smile\",\"source_url\":\"https://example.org/smile.png\"}}}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Organization.TryGetCustomEmoji();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.emoji);
            Assert.True(actual.emoji.ContainsKey("smile"));
        }

        [Fact]
        public async Task Organization_DeactivateCustomEmoji_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Organization.TryDeactivateCustomEmoji("smile");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Organization_GetProfileFields_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"custom_fields\":[{\"id\":1,\"name\":\"Phone\",\"type\":1}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Organization.TryGetProfileFields();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.fields);
        }

        [Fact]
        public async Task Organization_CreateProfileField_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Organization.TryCreateProfileField(1, "Department", hint: "Your department");
            Assert.True(actual.success, actual.details);
        }
    }
}
