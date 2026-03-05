using System.Text.Json;
using Xunit;
using zulip_cs_lib;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for ZulipResponse deserialization.</summary>
    public class ZulipResponseTests
    {
        [Fact]
        public void ZulipResponse_Deserialize_Success()
        {
            string json = "{\"result\":\"success\",\"msg\":\"\",\"id\":42}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json);

            Assert.Equal("success", response.Result);
            Assert.Equal(string.Empty, response.Message);
            Assert.Equal((ulong)42, response.Id);
        }

        [Fact]
        public void ZulipResponse_Deserialize_Error()
        {
            string json = "{\"result\":\"error\",\"msg\":\"Something went wrong\",\"code\":\"BAD_REQUEST\"}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json);

            Assert.Equal("error", response.Result);
            Assert.Equal("Something went wrong", response.Message);
            Assert.Equal("BAD_REQUEST", response.ErrorCode);
        }

        [Fact]
        public void ZulipResponse_Deserialize_WithMessages()
        {
            string json = "{\"result\":\"success\",\"msg\":\"\",\"messages\":[{\"id\":1,\"sender_id\":10,\"content\":\"Hello\",\"subject\":\"test\",\"type\":\"stream\"}]}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json);

            Assert.Equal("success", response.Result);
            Assert.NotNull(response.Messages);
            Assert.Single(response.Messages);
            Assert.Equal((ulong)1, response.Messages[0].Id);
        }

        [Fact]
        public void ZulipResponse_Deserialize_WithMembers()
        {
            string json = "{\"result\":\"success\",\"msg\":\"\",\"members\":[{\"user_id\":1,\"email\":\"a@example.org\",\"full_name\":\"Alice\"},{\"user_id\":2,\"email\":\"b@example.org\",\"full_name\":\"Bob\"}]}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json);

            Assert.Equal("success", response.Result);
            Assert.NotNull(response.Members);
            Assert.Equal(2, response.Members.Count);
        }

        [Fact]
        public void ZulipResponse_Deserialize_ServerSettings()
        {
            string json = "{\"result\":\"success\",\"msg\":\"\",\"zulip_version\":\"9.0\",\"zulip_feature_level\":310}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json);

            Assert.Equal("9.0", response.ZulipVersion);
            Assert.Equal(310, response.ZulipFeatureLevel);
        }

        [Fact]
        public void ZulipResponse_GetFailureMessage_WithCaughtException()
        {
            ZulipResponse response = new ZulipResponse
            {
                CaughtException = "Connection refused"
            };

            string message = response.GetFailureMessage();
            Assert.Equal("Connection refused", message);
        }

        [Fact]
        public void ZulipResponse_GetFailureMessage_WithEmptyResult()
        {
            ZulipResponse response = new ZulipResponse
            {
                HttpResponseCode = 500
            };

            string message = response.GetFailureMessage();
            Assert.Contains("500", message);
        }

        [Fact]
        public void ZulipResponse_GetFailureMessage_WithErrorResult()
        {
            ZulipResponse response = new ZulipResponse
            {
                Result = "error",
                ErrorCode = "BAD_REQUEST",
                Message = "Invalid parameters"
            };

            string message = response.GetFailureMessage();
            Assert.Contains("error", message);
            Assert.Contains("BAD_REQUEST", message);
            Assert.Contains("Invalid parameters", message);
        }
    }
}
