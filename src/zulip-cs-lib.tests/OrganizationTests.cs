using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Resources;

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
            Assert.Single(actual.linkifiers!);
        }

        [Fact]
        public async Task Organization_AddLinkifier_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"id\":42}");

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
            Assert.Single(actual.emoji!);
            Assert.True(actual.emoji!.ContainsKey("smile"));
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
            Assert.Single(actual.fields!);
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

        /// <summary>Rejects missing, null and malformed organization collections.</summary>
        /// <param name="payload">The malformed collection fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"linkifiers\":null,\"emoji\":null,\"custom_fields\":null")]
        [InlineData(",\"linkifiers\":{},\"emoji\":[],\"custom_fields\":{}")]
        [InlineData(",\"linkifiers\":[null],\"emoji\":{\"bad\":null},\"custom_fields\":[null]")]
        [InlineData(",\"linkifiers\":[{}],\"emoji\":{\"bad\":null},\"custom_fields\":[{}]")]
        public async Task Organization_RequiredLists_InvalidSuccessFails(string payload)
        {
            Organization organization = new Organization((method, url, data) => Task.FromResult(
                JsonSerializer.Deserialize<ZulipResponse>($"{{\"result\":\"success\"{payload}}}")!));
            Assert.False((await organization.TryGetLinkifiers()).success);
            Assert.False((await organization.TryGetCustomEmoji()).success);
            Assert.False((await organization.TryGetProfileFields()).success);
            await Assert.ThrowsAsync<Exception>(() => organization.GetLinkifiers());
            await Assert.ThrowsAsync<Exception>(() => organization.GetCustomEmoji());
            await Assert.ThrowsAsync<Exception>(() => organization.GetProfileFields());
        }

        /// <summary>Accepts empty organization collections as valid data.</summary>
        [Fact]
        public async Task Organization_EmptyListsAreValid()
        {
            Organization organization = new Organization((method, url, data) => Task.FromResult(
                JsonSerializer.Deserialize<ZulipResponse>("{\"result\":\"success\",\"linkifiers\":[],\"emoji\":{},\"custom_fields\":[]}")!));
            Assert.Empty(await organization.GetLinkifiers());
            Assert.Empty(await organization.GetCustomEmoji());
            Assert.Empty(await organization.GetProfileFields());
        }

        /// <summary>Rejects missing or unrepresentable actual linkifier identities.</summary>
        /// <param name="payload">The malformed identity fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"filter_id\":42")]
        [InlineData(",\"id\":null")]
        [InlineData(",\"id\":\"42\"")]
        [InlineData(",\"id\":2147483648")]
        public async Task Organization_AddLinkifier_InvalidIdentityFails(string payload)
        {
            Organization organization = new Organization((method, url, data) => Task.FromResult(
                JsonSerializer.Deserialize<ZulipResponse>($"{{\"result\":\"success\"{payload}}}")!));
            (bool success, string? details, int filterId) = await organization.TryAddLinkifier("pattern", "template");
            Assert.False(success);
            Assert.Equal(0, filterId);
            Assert.Contains("Organization.AddLinkifier failed:", details);
            Assert.Contains("id", details);
            await Assert.ThrowsAsync<Exception>(() => organization.AddLinkifier("pattern", "template"));
        }
    }
}
