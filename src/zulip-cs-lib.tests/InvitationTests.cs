using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using zulip_cs_lib;

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
            Assert.Single(actual.invites);
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
                "{\"result\":\"success\",\"msg\":\"\",\"invite_link_url\":\"https://zulip.example.org/join/abc123\"}");

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
    }
}
