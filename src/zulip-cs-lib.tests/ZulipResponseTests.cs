using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Models;

namespace zulip_set_lib.tests
{
    /// <summary>Tests for ZulipResponse deserialization.</summary>
    public class ZulipResponseTests
    {
        /// <summary>Realm presence uses collision-safe wire backing, not exception-message recovery.</summary>
        [Fact]
        public void ZulipResponse_RealmPresencePreservesModernAndLegacyWireData()
        {
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>("""
                {"result":"success","presences":{
                  "10":{"active_timestamp":123,"idle_timestamp":124},
                  "11":{"active_timestamp":125,"website":{"status":"active","timestamp":125}}
                }}
                """)!;
            Assert.Null(response.CaughtException);
            Assert.False(response.Presences!.ContainsKey("10"));
            Assert.Equal("active", response.Presences["11"]["website"].Status);
            JsonElement serialized = JsonSerializer.SerializeToElement(response);
            Assert.Equal(123, serialized.GetProperty("presences").GetProperty("10").GetProperty("active_timestamp").GetInt32());
            Assert.Equal(125, serialized.GetProperty("presences").GetProperty("11").GetProperty("website").GetProperty("timestamp").GetInt32());

            ZulipResponse assigned = new ZulipResponse
            {
                Result = "success",
                Presences = new Dictionary<string, Dictionary<string, PresenceInfo>>
                {
                    { "10", new Dictionary<string, PresenceInfo> { { "web", new PresenceInfo { Status = "idle", Timestamp = 20 } } } }
                }
            };
            ZulipResponse roundTrip = JsonSerializer.Deserialize<ZulipResponse>(JsonSerializer.Serialize(assigned))!;
            Assert.Equal("idle", roundTrip.Presences!["10"]["web"].Status);
            Assert.Equal(20, roundTrip.Presences["10"]["web"].Timestamp);
        }

        /// <summary>A valid-looking envelope cannot override a known failed HTTP status.</summary>
        [Fact]
        public void ZulipResponse_RequiredFieldValidationHonorsHttpFailure()
        {
            ZulipResponse response = new ZulipResponse { Result = "success", Members = [], HttpResponseCode = 502 };
            Assert.False(ResponsePayload.TryRead<List<UserObject>>(response, "Users.GetAll", "members",
                JsonValueKind.Array, out _, out string? details));
            Assert.Contains("502", details);
        }

        [Fact]
        public void ZulipResponse_Deserialize_Success()
        {
            string json = "{\"result\":\"success\",\"msg\":\"\",\"id\":42}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json)!;

            Assert.Equal("success", response.Result);
            Assert.Equal(string.Empty, response.Message);
            Assert.Equal((ulong)42, response.Id);
        }

        [Fact]
        public void ZulipResponse_Deserialize_Error()
        {
            string json = "{\"result\":\"error\",\"msg\":\"Something went wrong\",\"code\":\"BAD_REQUEST\"}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json)!;

            Assert.Equal("error", response.Result);
            Assert.Equal("Something went wrong", response.Message);
            Assert.Equal("BAD_REQUEST", response.ErrorCode);
        }

        [Fact]
        public void ZulipResponse_Deserialize_WithMessages()
        {
            string json = "{\"result\":\"success\",\"msg\":\"\",\"messages\":[{\"id\":1,\"sender_id\":10,\"content\":\"Hello\",\"subject\":\"test\",\"type\":\"stream\"}]}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json)!;

            Assert.Equal("success", response.Result);
            Assert.NotNull(response.Messages);
            Assert.Single(response.Messages);
            Assert.Equal((ulong)1, response.Messages[0].Id);
        }

        [Fact]
        public void ZulipResponse_Deserialize_WithMembers()
        {
            string json = "{\"result\":\"success\",\"msg\":\"\",\"members\":[{\"user_id\":1,\"email\":\"a@example.org\",\"full_name\":\"Alice\"},{\"user_id\":2,\"email\":\"b@example.org\",\"full_name\":\"Bob\"}]}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json)!;

            Assert.Equal("success", response.Result);
            Assert.NotNull(response.Members);
            Assert.Equal(2, response.Members.Count);
        }

        [Fact]
        public void ZulipResponse_Deserialize_ServerSettings()
        {
            string json = "{\"result\":\"success\",\"msg\":\"\",\"zulip_version\":\"9.0\",\"zulip_feature_level\":310}";
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(json)!;

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

        /// <summary>Retains original payload and extension data beyond the source document's lifetime.</summary>
        [Fact]
        public void ZulipResponse_RawPayloadAndUnknownFieldsOwnTheirStorage()
        {
            ZulipResponse response;
            using (JsonDocument document = JsonDocument.Parse(
                "{\"result\":\"success\",\"members\":[10,12],\"last_event_id\":-1,\"future\":{\"nested\":[1,true]},\"ignored_parameters_unsupported\":[\"future_option\"]}"))
            {
                response = document.RootElement.Deserialize<ZulipResponse>()!;
            }

            Assert.Equal(12, response.RawPayload.GetProperty("members")[1].GetInt32());
            Assert.Equal(-1, response.RawPayload.GetProperty("last_event_id").GetInt32());
            Assert.True(response.ExtensionData!["future"].GetProperty("nested")[1].GetBoolean());
            Assert.Equal(new string[] { "future_option" }, response.IgnoredParametersUnsupported);
            Assert.Null(response.Members);
            response.Result = "changed";
            Assert.Equal("success", response.RawPayload.GetProperty("result").GetString());
        }

        /// <summary>Preserves caller-created users and legacy presence serialization.</summary>
        [Fact]
        public void ZulipResponse_CallerCreatedMembersAndPresenceRoundTrip()
        {
            ZulipResponse response = new ZulipResponse
            {
                Result = "success",
                Members = [new UserObject { UserId = 10, Email = "a@example.org", FullName = "Alice" }],
                Presence = new Dictionary<string, PresenceInfo>
                {
                    { "website", new PresenceInfo { Status = "idle", Timestamp = 123, Client = "web" } },
                    { "aggregated", new PresenceInfo { Status = "offline", Timestamp = 123 } }
                }
            };

            string json = JsonSerializer.Serialize(response);
            using JsonDocument document = JsonDocument.Parse(json);
            Assert.Equal(10, document.RootElement.GetProperty("members")[0].GetProperty("user_id").GetInt32());
            Assert.Equal("offline", document.RootElement.GetProperty("presence").GetProperty("aggregated").GetProperty("status").GetString());
            Assert.False(document.RootElement.TryGetProperty("MembersPayload", out _));
            Assert.False(document.RootElement.TryGetProperty("RawPayload", out _));
            ZulipResponse roundTrip = JsonSerializer.Deserialize<ZulipResponse>(json)!;
            Assert.Equal("Alice", Assert.Single(roundTrip.Members!).FullName);
            Assert.Equal("web", roundTrip.Presence!["website"].Client);
            Assert.Equal(123L, roundTrip.Presence["aggregated"].Timestamp);
        }

        /// <summary>Reading the legacy accessor does not discard modern wire presence on serialization.</summary>
        [Fact]
        public void ZulipResponse_ModernPresenceSurvivesLegacyAccessorAndSerialization()
        {
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(
                "{\"result\":\"success\",\"presence\":{\"active_timestamp\":123,\"idle_timestamp\":124,\"website\":{\"status\":\"active\",\"timestamp\":123},\"aggregated\":{\"status\":\"offline\",\"timestamp\":123}},\"ignored_parameters_unsupported\":[\"option\"],\"future_field\":{\"flag\":true}}")!;

            Assert.Equal("offline", response.Presence!["aggregated"].Status);
            JsonElement serialized = JsonSerializer.SerializeToElement(response);
            Assert.Equal(123, serialized.GetProperty("presence").GetProperty("active_timestamp").GetInt32());
            Assert.Equal("option", serialized.GetProperty("ignored_parameters_unsupported")[0].GetString());
            Assert.True(serialized.GetProperty("future_field").GetProperty("flag").GetBoolean());
        }

        /// <summary>Validates raw field presence rather than applying a generic numeric threshold.</summary>
        [Fact]
        public void ZulipResponse_RequiredFieldValidationPreservesValidNumericValues()
        {
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>("{\"result\":\"success\",\"last_event_id\":-1,\"id\":0}")!;
            Assert.True(ResponsePayload.TryRead<int>(response, "Events.RegisterQueue", "last_event_id",
                JsonValueKind.Number, out int eventId, out string? eventDetails), eventDetails);
            Assert.Equal(-1, eventId);
            Assert.True(ResponsePayload.TryRead<int>(response, "Organization.AddLinkifier", "id",
                JsonValueKind.Number, out int id, out string? details), details);
            Assert.Equal(0, id);
            ZulipResponse missing = JsonSerializer.Deserialize<ZulipResponse>("{\"result\":\"success\"}")!;
            Assert.False(ResponsePayload.TryRead<int>(missing, "Events.RegisterQueue", "last_event_id",
                JsonValueKind.Number, out _, out string? failure));
            Assert.Contains("last_event_id", failure);
        }

        /// <summary>Distinguishes explicit zero and negative IDs from unassigned defaults in injected responses.</summary>
        /// <param name="eventId">The explicitly assigned event ID.</param>
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void ZulipResponse_InjectedIntegersValidateAssignmentRatherThanDefault(int eventId)
        {
            ZulipResponse explicitResponse = new ZulipResponse { Result = "success", LastEventId = eventId };
            Assert.True(ResponsePayload.TryRead<int>(explicitResponse, "Events.RegisterQueue", "last_event_id",
                JsonValueKind.Number, out int actualId, out string? details), details);
            Assert.Equal(eventId, actualId);
            ZulipResponse unassignedResponse = new ZulipResponse { Result = "success" };
            Assert.False(ResponsePayload.TryRead<int>(unassignedResponse, "Events.RegisterQueue", "last_event_id",
                JsonValueKind.Number, out _, out string? failure));
            Assert.Contains("last_event_id", failure);
        }

        /// <summary>Resource validation uses the API envelope and caught errors, not a transport-only flag.</summary>
        [Fact]
        public void ZulipResponse_RequiredFieldValidationRejectsCaughtExceptionButNotTransportFlag()
        {
            ZulipResponse response = new ZulipResponse { Result = "success", Members = [] };
            Assert.False(response.Success);
            Assert.True(ResponsePayload.TryRead<List<UserObject>>(response, "Users.GetAll", "members",
                JsonValueKind.Array, out List<UserObject>? members, out string? details), details);
            Assert.Empty(members!);
            response.CaughtException = "response could not be parsed";
            Assert.False(ResponsePayload.TryRead<List<UserObject>>(response, "Users.GetAll", "members",
                JsonValueKind.Array, out _, out string? failure));
            Assert.Equal("Users.GetAll failed: response could not be parsed", failure);
        }

        /// <summary>Malformed typed fields do not overwrite the actual API error envelope.</summary>
        [Fact]
        public void ZulipResponse_InvalidTypedFieldRetainsActualApiError()
        {
            ZulipResponse response = JsonSerializer.Deserialize<ZulipResponse>(
                "{\"result\":\"error\",\"code\":\"BAD_REQUEST\",\"msg\":\"Actual server error\",\"user\":\"invalid\"}")!;
            Assert.Equal("error", response.Result);
            Assert.Equal("BAD_REQUEST", response.ErrorCode);
            Assert.Equal("Actual server error", response.Message);
            Assert.NotNull(response.CaughtException);
            Assert.Equal("invalid", response.RawPayload.GetProperty("user").GetString());
            Assert.Contains("BAD_REQUEST", response.GetFailureMessage());
            Assert.Contains("Actual server error", response.GetFailureMessage());
        }

        /// <summary>Retains meaningful empty collections while respecting default-value serialization.</summary>
        [Fact]
        public void ZulipResponse_SerializationRespectsNullAndDefaultOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            ZulipResponse response = new ZulipResponse { Result = "success", Members = [], Presence = [] };
            JsonElement serialized = JsonSerializer.SerializeToElement(response, options);
            Assert.Empty(serialized.GetProperty("members").EnumerateArray());
            Assert.Empty(serialized.GetProperty("presence").EnumerateObject());
            Assert.False(serialized.TryGetProperty("id", out _));
            Assert.False(serialized.TryGetProperty("last_event_id", out _));
        }
    }
}
