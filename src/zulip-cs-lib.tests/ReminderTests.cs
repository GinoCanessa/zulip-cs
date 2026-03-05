using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for Reminders resource.</summary>
    public class ReminderTests
    {
        [Fact]
        public async Task Reminders_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"reminders\":[{\"id\":1,\"message_id\":100,\"scheduled_delivery_timestamp\":1700000000}]}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Reminders.TryGetAll();
            Assert.True(actual.success, actual.details);
            Assert.Single(actual.reminders);
        }

        [Fact]
        public async Task Reminders_Create_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Reminders.TryCreate(100, 1700000000);
            Assert.True(actual.success, actual.details);
        }

        [Fact]
        public async Task Reminders_Delete_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\"}");

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            var actual = await zulipClient.Reminders.TryDelete(1);
            Assert.True(actual.success, actual.details);
        }
    }
}
