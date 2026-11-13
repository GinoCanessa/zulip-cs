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
    /// <summary>Tests for Invitations resource.</summary>
    public class InvitationTests
    {
        [Fact]
        public async Task Invitations_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"invites\":[{\"id\":1,\"invited_by_user_id\":10,\"email\":\"new@example.org\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Invitations.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.invites!);
        }

        [Fact]
        public async Task Invitations_Send_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Invitations.TrySend("new@example.org", "[1]");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Invitations_CreateLink_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"invite_link\":\"https://zulip.example.org/join/abc123\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Invitations.TryCreateLink("[1]");
            Assert.True(actual.success, actual.details);
            Assert.Contains("abc123", actual.linkUrl);
        }

        [Fact]
        public async Task Invitations_Resend_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Invitations.TryResend(1);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Invitations_Revoke_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Invitations.TryRevoke(1);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Invitations_RevokeLink_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Invitations.TryRevokeLink(1);
            Assert.True(actual.success, actual.details);
        }

        /// <summary>Rejects missing or malformed required invitation lists.</summary>
        /// <param name="payload">The malformed invitation fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"invites\":null")]
        [InlineData(",\"invites\":{}")]
        [InlineData(",\"invites\":[null]")]
        public async Task Invitations_MissingOrInvalidListFails(string payload)
        {
            Invitations invitations = new Invitations((method, url, data) => Task.FromResult(
                JsonSerializer.Deserialize<ZulipResponse>($"{{\"result\":\"success\"{payload}}}")!));
            (bool success, string? details, List<zulip_cs_lib.Models.InviteObject>? invites) = await invitations.TryGetAll();
            Assert.False(success);
            Assert.Null(invites);
            Assert.Contains("Invitations.GetAll failed:", details);
            Assert.Contains("invites", details);
            await Assert.ThrowsAsync<Exception>(() => invitations.GetAll());
        }

        /// <summary>Accepts an empty invitation list.</summary>
        [Fact]
        public async Task Invitations_EmptyListIsValid()
        {
            Invitations invitations = new Invitations((method, url, data) => Task.FromResult(
                JsonSerializer.Deserialize<ZulipResponse>("{\"result\":\"success\",\"invites\":[]}")!));
            Assert.Empty(await invitations.GetAll());
        }

        /// <summary>Rejects missing, null or incorrect returned invitation URLs.</summary>
        /// <param name="payload">The malformed invitation URL fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"invite_link_url\":\"https://example.org/join/a\"")]
        [InlineData(",\"invite_link\":null")]
        [InlineData(",\"invite_link\":42")]
        [InlineData(",\"invite_link\":\"\"")]
        public async Task Invitations_InvalidLinkPayloadFails(string payload)
        {
            Invitations invitations = new Invitations((method, url, data) => Task.FromResult(
                JsonSerializer.Deserialize<ZulipResponse>($"{{\"result\":\"success\"{payload}}}")!));
            (bool success, string? details, string? linkUrl) = await invitations.TryCreateLink("[1]");
            Assert.False(success);
            Assert.Null(linkUrl);
            Assert.Contains("Invitations.CreateLink failed:", details);
            Assert.Contains("invite_link", details);
            await Assert.ThrowsAsync<Exception>(() => invitations.CreateLink("[1]"));
        }
    }
}
