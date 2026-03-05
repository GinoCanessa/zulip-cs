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
    /// <summary>Tests for Users resource.</summary>
    public class UserTests
    {
        [Fact]
        public async Task Users_GetOwnUser_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user\":{\"user_id\":10,\"email\":\"me@example.org\",\"full_name\":\"Test User\"}}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetOwnUser();
            Assert.True(actual.success, actual.details);
            Assert.NotNull(actual.user);
            Assert.Equal(10, actual.user.UserId);
        }

        [Fact]
        public async Task Users_GetUser_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user\":{\"user_id\":42,\"email\":\"other@example.org\",\"full_name\":\"Other User\"}}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetUser(42);
            Assert.True(actual.success, actual.details);
            Assert.Equal(42, actual.user.UserId);
        }

        [Fact]
        public async Task Users_GetUserByEmail_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user\":{\"user_id\":5,\"email\":\"user@example.org\",\"full_name\":\"Email User\"}}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetUserByEmail("user@example.org");
            Assert.True(actual.success, actual.details);
            Assert.Equal("user@example.org", actual.user.Email);
        }

        [Fact]
        public async Task Users_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"members\":[{\"user_id\":1,\"email\":\"a@example.org\",\"full_name\":\"User A\"},{\"user_id\":2,\"email\":\"b@example.org\",\"full_name\":\"User B\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Equal(2, actual.members.Count);
        }

        [Fact]
        public async Task Users_SetTypingStatus_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TrySetTypingStatus("start", new int[] { 1, 2 });
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_Create_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryCreate("new@example.org", "password123", "New User");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_Update_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryUpdate(10, fullName: "Updated Name");
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_Deactivate_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryDeactivate(10);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_Reactivate_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryReactivate(10);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_MuteUser_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryMuteUser(10);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_UnmuteUser_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryUnmuteUser(10);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_GetAlertWords_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"alert_words\":[\"foo\",\"bar\"]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetAlertWords();
            Assert.True(actual.success, actual.details);
            Assert.Equal(2, actual.alertWords.Count);
            Assert.Contains("foo", actual.alertWords);
        }

        [Fact]
        public async Task Users_AddAlertWords_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryAddAlertWords(new string[] { "alert1", "alert2" });
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_GetGroups_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"user_groups\":[{\"id\":1,\"name\":\"admins\",\"description\":\"Admin group\"}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetGroups();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.groups);
        }

        [Fact]
        public async Task Users_CreateGroup_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryCreateGroup("test-group", "A test group", new int[] { 1, 2 });
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Users_GetOwnUser_Error()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"error\",\"msg\":\"Not logged in\",\"code\":\"UNAUTHORIZED\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.Unauthorized, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Users.TryGetOwnUser();
            Assert.False(actual.success);
            Assert.False(string.IsNullOrEmpty(actual.details));
        }
    }
}
