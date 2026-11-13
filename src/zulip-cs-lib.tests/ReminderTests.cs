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
    /// <summary>Tests for Reminders resource.</summary>
    public class ReminderTests
    {
        /// <summary>Requires both reminder identity and target instead of returning default zero.</summary>
        /// <param name="fields">The incomplete reminder fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData("\"reminder_id\":7")]
        [InlineData("\"reminder_target_message_id\":100")]
        public async Task Reminders_GetAll_RequiresNestedIdentityAndTarget(string fields)
        {
            string json = $"{{\"result\":\"success\",\"reminders\":[{{{fields}}}]}}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details, List<ReminderObject>? reminders) actual = await client.Reminders.TryGetAll();
            Assert.False(actual.success);
            Assert.Null(actual.reminders);
            Assert.Contains("reminder", actual.details);
            await Assert.ThrowsAsync<Exception>(() => client.Reminders.GetAll());
        }

        [Fact]
        public async Task Reminders_GetAll_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                """
                {"result":"success","msg":"","reminders":[{
                  "reminder_id":7,"reminder_target_message_id":100,"scheduled_delivery_timestamp":1700000000,
                  "type":"private","to":[10,20],"content":"**Check this**","rendered_content":"<p><strong>Check this</strong></p>",
                  "failed":true
                }]}
                """);

            bool success = Utils.TryGetMockedClient(
                HttpStatusCode.OK, content,
                out Mock<HttpMessageHandler> handler, out HttpClient client, out ZulipClient zulipClient);

            Assert.True(success);

            (bool success, string? details, List<ReminderObject>? reminders) actual = await zulipClient.Reminders.TryGetAll();
            Assert.True(actual.success, actual.details);
            ReminderObject reminder = Assert.Single(actual.reminders!);
            Assert.Equal(7, reminder.ReminderId);
            Assert.Equal(100UL, reminder.MessageId);
            Assert.Equal(1700000000L, reminder.ScheduledDeliveryTimestamp);
            Assert.Equal("private", reminder.Type);
            Assert.Equal(new int[] { 10, 20 }, reminder.To);
            Assert.Equal("**Check this**", reminder.Content);
            Assert.Equal("<p><strong>Check this</strong></p>", reminder.RenderedContent);
            Assert.True(reminder.Failed);
        }

        [Fact]
        public async Task Reminders_Create_Success()
        {
            HttpContent content = Utils.ContentForJsonString(
                "{\"result\":\"success\",\"msg\":\"\",\"reminder_id\":7}");

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

        [Fact]
        public async Task Reminders_CreateWithId_PreservesNoteAndDeletionIdentity()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"reminder_id\":81}", "{\"result\":\"success\",\"reminder_id\":82}",
                "{\"result\":\"success\"}");
            const string note = "Review \"quoted\" note + \u00e9";
            (bool success, string? details, int reminderId) actual =
                await client.Reminders.TryCreateWithId(100, 1700000000, note);
            Assert.True(actual.success, actual.details);
            Assert.Equal(81, actual.reminderId);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/api/v1/reminders", request.RequestUri!.AbsoluteUri);
            Assert.Equal(new Dictionary<string, string>
            {
                { "message_id", "100" }, { "scheduled_delivery_timestamp", "1700000000" }, { "note", note }
            }, await Utils.ReadForm(request));

            Assert.Equal(82, await client.Reminders.CreateWithId(101, 1700000010));
            Assert.False((await Utils.ReadForm(requests[1])).ContainsKey("note"));
            await client.Reminders.Delete(actual.reminderId);
            Assert.Equal(3, requests.Count);
            Assert.Equal(HttpMethod.Delete, requests[2].Method);
            Assert.EndsWith("/reminders/81", requests[2].RequestUri!.AbsoluteUri);
        }

        [Theory]
        [InlineData("{\"result\":\"success\"}")]
        [InlineData("{\"result\":\"success\",\"reminder_id\":null}")]
        [InlineData("{\"result\":\"success\",\"reminder_id\":\"7\"}")]
        public async Task Reminders_Create_RejectsMissingOrMalformedReturnedId(string json)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json, json, json);
            (bool success, string? details, int reminderId) actual =
                await client.Reminders.TryCreateWithId(100, 1700000000);
            Assert.False(actual.success);
            Assert.Contains("reminder_id", actual.details);
            Assert.Equal(0, actual.reminderId);
            Assert.False((await client.Reminders.TryCreate(100, 1700000000)).success);
            await Assert.ThrowsAsync<Exception>(() => client.Reminders.CreateWithId(100, 1700000000));
            await Assert.ThrowsAsync<Exception>(() => client.Reminders.Create(100, 1700000000));
        }

        [Theory]
        [InlineData("{\"result\":\"success\"}", false)]
        [InlineData("{\"result\":\"success\",\"reminders\":null}", false)]
        [InlineData("{\"result\":\"success\",\"reminders\":[null]}", false)]
        [InlineData("{\"result\":\"success\",\"reminders\":[]}", true)]
        public async Task Reminders_GetAll_DistinguishesMalformedAndEmptyCollections(string json, bool expectedSuccess)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json);
            (bool success, string? details, List<ReminderObject>? reminders) actual = await client.Reminders.TryGetAll();
            Assert.Equal(expectedSuccess, actual.success);
            if (expectedSuccess) Assert.Empty(actual.reminders!);
            else
            {
                Assert.Null(actual.reminders);
                Assert.Contains("reminders", actual.details);
            }
        }
    }
}
