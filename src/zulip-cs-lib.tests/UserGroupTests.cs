using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Models;

namespace zulip_set_lib.tests
{
    /// <summary>Live-contract tests for user-group administration.</summary>
    public class UserGroupTests
    {
        /// <summary>Creation preserves group_id, optional subgroups and actual named/anonymous governance settings.</summary>
        [Fact]
        public async Task Groups_CreateWithId_ActualFormsAndLegacyProjections()
        {
            List<HttpRequestMessage> requests = [];
            const string json = "{\"result\":\"success\",\"group_id\":123}";
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json, json, json);
            GroupOptions options = new GroupOptions
            {
                Subgroups = [11],
                CanAddMembersGroup = new GroupSettingValue(11),
                CanJoinGroup = new GroupSettingValue([10], [11]),
                CanLeaveGroup = new GroupSettingValue([], []),
                CanManageGroup = new GroupSettingValue(12),
                CanMentionGroup = new GroupSettingValue(13),
                CanRemoveMembersGroup = new GroupSettingValue(14)
            };
            (bool success, string? details, int groupId) actual = await client.Users.TryCreateGroupWithId(
                "Marketing + 雪", "Team \"description\"", [10, 12], options);
            Assert.True(actual.success, actual.details);
            Assert.Equal(123, actual.groupId);
            Assert.Single(requests);
            Assert.Equal(HttpMethod.Post, requests[0].Method);
            Assert.Equal("/api/v1/user_groups/create", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal(new Dictionary<string, string>
            {
                { "name", "Marketing + 雪" }, { "description", "Team \"description\"" }, { "members", "[10,12]" },
                { "subgroups", "[11]" }, { "can_add_members_group", "11" },
                { "can_join_group", "{\"direct_members\":[10],\"direct_subgroups\":[11]}" },
                { "can_leave_group", "{\"direct_members\":[],\"direct_subgroups\":[]}" },
                { "can_manage_group", "12" }, { "can_mention_group", "13" }, { "can_remove_members_group", "14" }
            }, await Utils.ReadForm(requests[0]));

            Assert.Equal(123, await client.Users.CreateGroupWithId("Empty", "", []));
            Assert.True((await client.Users.TryCreateGroup("Empty", "", [])).success);
            await client.Users.CreateGroup("Empty", "", []);
            Assert.Equal(4, requests.Count);
            for (int index = 1; index < requests.Count; index++)
            {
                Assert.Equal(new Dictionary<string, string>
                    { { "name", "Empty" }, { "description", "" }, { "members", "[]" } },
                    await Utils.ReadForm(requests[index]));
            }
        }

        /// <summary>All creation variants require the actual integer group_id field and preserve server errors.</summary>
        /// <param name="json">The malformed or failed response.</param>
        /// <param name="expected">The expected failure detail.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}", "group_id")]
        [InlineData("{\"result\":\"success\",\"id\":123}", "group_id")]
        [InlineData("{\"result\":\"success\",\"group_id\":null}", "group_id")]
        [InlineData("{\"result\":\"success\",\"group_id\":\"123\"}", "group_id")]
        [InlineData("{\"result\":\"success\",\"group_id\":1.5}", "group_id")]
        [InlineData("{\"result\":\"error\",\"msg\":\"Invalid user ID: 500\",\"code\":\"BAD_REQUEST\"}", "Invalid user ID: 500")]
        public async Task Groups_Create_RequiresIdEvenForLegacyProjection(string json, string expected)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json, json, json);
            (bool success, string? details, int groupId) actual = await client.Users.TryCreateGroupWithId("Team", "", []);
            Assert.False(actual.success);
            Assert.Contains(expected, actual.details);
            Assert.Equal(0, actual.groupId);
            Assert.False((await client.Users.TryCreateGroup("Team", "", [])).success);
            await Assert.ThrowsAsync<Exception>(() => client.Users.CreateGroupWithId("Team", "", []));
            await Assert.ThrowsAsync<Exception>(() => client.Users.CreateGroup("Team", "", []));
            Assert.Equal(4, requests.Count);
        }

        /// <summary>Update uses group-setting new/old values, omits absent expectations and supports explicit reactivation.</summary>
        [Fact]
        public async Task Groups_Update_ActualNewOldContract()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            GroupUpdateOptions options = new GroupUpdateOptions
            {
                CanAddMembersGroup = new GroupSettingUpdate(new GroupSettingValue([10], [11]), new GroupSettingValue(11)),
                CanJoinGroup = new GroupSettingUpdate(new GroupSettingValue(12)),
                CanLeaveGroup = new GroupSettingUpdate(new GroupSettingValue([], []), new GroupSettingValue([10], [])),
                CanManageGroup = new GroupSettingUpdate(new GroupSettingValue(13)),
                CanMentionGroup = new GroupSettingUpdate(new GroupSettingValue(14)),
                CanRemoveMembersGroup = new GroupSettingUpdate(new GroupSettingValue(15))
            };
            Assert.True((await client.Users.TryUpdateGroup(38, "Team + 雪", "", options)).success);
            Assert.Single(requests);
            Assert.Equal(HttpMethod.Patch, requests[0].Method);
            Assert.Equal("/api/v1/user_groups/38", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal(new Dictionary<string, string>
            {
                { "name", "Team + 雪" }, { "description", "" },
                { "can_add_members_group", "{\"new\":{\"direct_members\":[10],\"direct_subgroups\":[11]},\"old\":11}" },
                { "can_join_group", "{\"new\":12}" },
                { "can_leave_group", "{\"new\":{\"direct_members\":[],\"direct_subgroups\":[]},\"old\":{\"direct_members\":[10],\"direct_subgroups\":[]}}" },
                { "can_manage_group", "{\"new\":13}" }, { "can_mention_group", "{\"new\":14}" },
                { "can_remove_members_group", "{\"new\":15}" }
            }, await Utils.ReadForm(requests[0]));
            await client.Users.UpdateGroup(38, options: new GroupUpdateOptions { Reactivate = true });
            Assert.Equal(new Dictionary<string, string> { { "deactivated", "false" } }, await Utils.ReadForm(requests[1]));
        }

        /// <summary>Membership and subgroup mutations send the appropriate add/delete parameter names, including explicit empty lists.</summary>
        [Fact]
        public async Task Groups_UpdateMembersAndSubgroups_ActualForms()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\"}", "{\"result\":\"success\"}", "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            Assert.True((await client.Users.TryUpdateGroupMembers(38, add: [12, 13], delete: [10],
                addSubgroups: [11], deleteSubgroups: [])).success);
            await client.Users.UpdateGroupMembers(38, delete: []);
            Assert.True((await client.Users.TryUpdateGroupSubgroups(38, add: [11], delete: [9])).success);
            await client.Users.UpdateGroupSubgroups(38, add: []);
            Assert.Equal(4, requests.Count);
            Assert.Equal(new Dictionary<string, string>
            {
                { "add", "[12,13]" }, { "delete", "[10]" }, { "add_subgroups", "[11]" }, { "delete_subgroups", "[]" }
            }, await Utils.ReadForm(requests[0]));
            Assert.Equal(new Dictionary<string, string> { { "delete", "[]" } }, await Utils.ReadForm(requests[1]));
            Assert.Equal(new Dictionary<string, string> { { "add", "[11]" }, { "delete", "[9]" } }, await Utils.ReadForm(requests[2]));
            Assert.Equal(new Dictionary<string, string> { { "add", "[]" } }, await Utils.ReadForm(requests[3]));
            for (int index = 0; index < requests.Count; index++)
            {
                Assert.Equal(HttpMethod.Post, requests[index].Method);
                Assert.Equal(index < 2 ? "/api/v1/user_groups/38/members" : "/api/v1/user_groups/38/subgroups",
                    requests[index].RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Membership status reads the actual boolean, with explicit true/false filtering and no implicit query defaults.</summary>
        /// <param name="isMember">The server's membership status.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Groups_MembershipStatus_ActualBooleanAndFiltering(bool isMember)
        {
            string json = "{\"result\":\"success\",\"is_user_group_member\":" + (isMember ? "true" : "false") + "}";
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json, json);
            (bool success, string? details, bool isMember) actual = await client.Users.TryGetGroupMembershipStatus(38, 12);
            Assert.True(actual.success, actual.details);
            Assert.Equal(isMember, actual.isMember);
            Assert.Equal(isMember, await client.Users.GetGroupMembershipStatus(38, 12, true));
            Assert.Equal(isMember, await client.Users.GetGroupMembershipStatus(38, 12, false));
            Assert.Equal("", requests[0].RequestUri!.Query);
            Assert.Equal(new Dictionary<string, string> { { "direct_member_only", "true" } },
                Utils.DecodeForm(requests[1].RequestUri!.Query));
            Assert.Equal(new Dictionary<string, string> { { "direct_member_only", "false" } },
                Utils.DecodeForm(requests[2].RequestUri!.Query));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v1/user_groups/38/members/12", request.RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Missing or malformed membership status cannot become a successful false.</summary>
        /// <param name="fields">The malformed membership fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"is_user_group_member\":null")]
        [InlineData(",\"is_user_group_member\":\"false\"")]
        [InlineData(",\"is_user_group_member\":0")]
        [InlineData(",\"is_member\":false")]
        public async Task Groups_MembershipStatus_RequiresActualBoolean(string fields)
        {
            string json = "{\"result\":\"success\"" + fields + "}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details, bool isMember) actual = await client.Users.TryGetGroupMembershipStatus(38, 12);
            Assert.False(actual.success);
            Assert.Contains("is_user_group_member", actual.details);
            await Assert.ThrowsAsync<Exception>(() => client.Users.GetGroupMembershipStatus(38, 12));
        }

        /// <summary>Subgroup lists accept real integer IDs and empty arrays and retain explicit filtering choices.</summary>
        /// <param name="ids">The actual integer subgroup IDs.</param>
        [Theory]
        [InlineData(new int[] { 2, 3 })]
        [InlineData(new int[] { })]
        public async Task Groups_GetSubgroups_ActualNonemptyAndEmptyLists(int[] ids)
        {
            string json = "{\"result\":\"success\",\"subgroups\":" + JsonSerializer.Serialize(ids) + "}";
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json, json);
            (bool success, string? details, List<int>? subgroups) actual = await client.Users.TryGetGroupSubgroups(38);
            Assert.True(actual.success, actual.details);
            Assert.Equal(ids, actual.subgroups);
            Assert.Equal(ids, await client.Users.GetGroupSubgroups(38, true));
            Assert.Equal(ids, await client.Users.GetGroupSubgroups(38, false));
            Assert.Equal("", requests[0].RequestUri!.Query);
            Assert.Equal(new Dictionary<string, string> { { "direct_subgroup_only", "true" } },
                Utils.DecodeForm(requests[1].RequestUri!.Query));
            Assert.Equal(new Dictionary<string, string> { { "direct_subgroup_only", "false" } },
                Utils.DecodeForm(requests[2].RequestUri!.Query));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v1/user_groups/38/subgroups", request.RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Subgroup reads reject missing lists, wrong envelopes and noninteger entries.</summary>
        /// <param name="fields">The malformed subgroup fields.</param>
        [Theory]
        [InlineData("")]
        [InlineData(",\"subgroups\":null")]
        [InlineData(",\"subgroups\":{}")]
        [InlineData(",\"subgroups\":[{\"id\":2}]")]
        [InlineData(",\"subgroups\":[1.5]")]
        [InlineData(",\"subgroup_ids\":[2,3]")]
        public async Task Groups_GetSubgroups_RequiresIntegerList(string fields)
        {
            string json = "{\"result\":\"success\"" + fields + "}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details, List<int>? subgroups) actual = await client.Users.TryGetGroupSubgroups(38);
            Assert.False(actual.success);
            Assert.Null(actual.subgroups);
            Assert.Contains("subgroups", actual.details);
            await Assert.ThrowsAsync<Exception>(() => client.Users.GetGroupSubgroups(38));
        }

        /// <summary>Deactivation uses POST with no form payload and leaves reactivation to an explicit update.</summary>
        [Fact]
        public async Task Groups_Deactivate_ActualEndpoint()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            Assert.True((await client.Users.TryDeactivateGroup(38)).success);
            await client.Users.DeactivateGroup(38);
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/v1/user_groups/38/deactivate", request.RequestUri!.AbsolutePath);
                Assert.Empty(await Utils.ReadForm(request));
            }
        }

        /// <summary>Invalid creation, governance, mutation and read inputs are rejected before any request.</summary>
        [Fact]
        public async Task Groups_InvalidInputs_NoRequests()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            Assert.False((await client.Users.TryCreateGroupWithId("", "", [])).success);
            Assert.False((await client.Users.TryCreateGroupWithId("Team", null!, [])).success);
            Assert.False((await client.Users.TryCreateGroupWithId("Team", "", null!)).success);
            Assert.False((await client.Users.TryCreateGroup("Team", "", [0])).success);
            Assert.False((await client.Users.TryCreateGroupWithId("Team", "", [], new GroupOptions { Subgroups = [-1] })).success);
            Assert.False((await client.Users.TryCreateGroupWithId("Team", "", [],
                new GroupOptions { CanJoinGroup = new GroupSettingValue([0], []) })).success);
            Assert.False((await client.Users.TryUpdateGroup(0, "Team")).success);
            Assert.False((await client.Users.TryUpdateGroup(38, "")).success);
            Assert.False((await client.Users.TryUpdateGroup(38)).success);
            Assert.False((await client.Users.TryUpdateGroup(38, options: new GroupUpdateOptions
            {
                CanMentionGroup = new GroupSettingUpdate(new GroupSettingValue(11), new GroupSettingValue(0))
            })).success);
            Assert.False((await client.Users.TryUpdateGroupMembers(38)).success);
            Assert.False((await client.Users.TryUpdateGroupMembers(0, add: [])).success);
            Assert.False((await client.Users.TryUpdateGroupMembers(38, add: [0])).success);
            Assert.False((await client.Users.TryUpdateGroupMembers(38, deleteSubgroups: [-1])).success);
            Assert.False((await client.Users.TryGetGroupMembershipStatus(0, 12)).success);
            Assert.False((await client.Users.TryGetGroupMembershipStatus(38, 0)).success);
            Assert.False((await client.Users.TryGetGroupSubgroups(0)).success);
            Assert.False((await client.Users.TryUpdateGroupSubgroups(38)).success);
            Assert.False((await client.Users.TryUpdateGroupSubgroups(38, delete: [0])).success);
            Assert.False((await client.Users.TryDeactivateGroup(0)).success);
            await Assert.ThrowsAsync<Exception>(() => client.Users.CreateGroup("Team", "", [0]));
            await Assert.ThrowsAsync<Exception>(() => client.Users.UpdateGroup(38));
            await Assert.ThrowsAsync<Exception>(() => client.Users.UpdateGroupMembers(38));
            await Assert.ThrowsAsync<Exception>(() => client.Users.GetGroupMembershipStatus(0, 12));
            await Assert.ThrowsAsync<Exception>(() => client.Users.GetGroupSubgroups(0));
            await Assert.ThrowsAsync<Exception>(() => client.Users.UpdateGroupSubgroups(38));
            await Assert.ThrowsAsync<Exception>(() => client.Users.DeactivateGroup(0));
            Assert.Empty(requests);
        }

        /// <summary>Mutation/read pairs retain server errors, including expectation mismatch and deactivation objections, without retries.</summary>
        [Fact]
        public async Task Groups_ServerErrors_PropagateWithoutRetry()
        {
            const string denied = "{\"result\":\"error\",\"msg\":\"permission denied\",\"code\":\"BAD_REQUEST\"}";
            const string mismatch = "{\"result\":\"error\",\"msg\":\"Another user changed this setting\",\"code\":\"EXPECTATION_MISMATCH\"}";
            const string inUse = "{\"result\":\"error\",\"msg\":\"Cannot deactivate user group in use.\",\"code\":\"CANNOT_DEACTIVATE_GROUP_IN_USE\",\"objections\":[{\"type\":\"realm\",\"settings\":[\"can_create_public_channel_group\"]}]}";
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                mismatch, denied, denied, denied, inUse, mismatch, denied, denied, denied, inUse);
            Assert.Contains("Another user changed", (await client.Users.TryUpdateGroup(38, "Team")).details);
            Assert.Contains("permission denied", (await client.Users.TryUpdateGroupMembers(38, add: [12])).details);
            Assert.Contains("permission denied", (await client.Users.TryGetGroupMembershipStatus(38, 12)).details);
            Assert.Contains("permission denied", (await client.Users.TryGetGroupSubgroups(38)).details);
            Assert.Contains("Cannot deactivate", (await client.Users.TryDeactivateGroup(38)).details);
            await Assert.ThrowsAsync<Exception>(() => client.Users.UpdateGroup(38, "Team"));
            await Assert.ThrowsAsync<Exception>(() => client.Users.UpdateGroupMembers(38, add: [12]));
            await Assert.ThrowsAsync<Exception>(() => client.Users.GetGroupMembershipStatus(38, 12));
            await Assert.ThrowsAsync<Exception>(() => client.Users.GetGroupSubgroups(38));
            await Assert.ThrowsAsync<Exception>(() => client.Users.DeactivateGroup(38));
            Assert.Equal(10, requests.Count);

            ZulipClient subgroupClient = Utils.CreateCapturingClient([], denied, denied);
            Assert.Contains("permission denied", (await subgroupClient.Users.TryUpdateGroupSubgroups(38, add: [11])).details);
            await Assert.ThrowsAsync<Exception>(() => subgroupClient.Users.UpdateGroupSubgroups(38, add: [11]));
        }
    }
}
