using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Models;
using zulip_cs_lib.Resources;

namespace zulip_set_lib.tests
{
    /// <summary>Channel-folder identities/order/archive and user-channel lookup contracts.</summary>
    public class ChannelFolderTests
    {
        /// <summary>Folder creation always includes the required description, including a valid empty description.</summary>
        [Fact]
        public async Task ChannelFolders_Create_ActualIdentityAndRequiredDescription()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"channel_folder_id\":12}", "{\"result\":\"success\",\"channel_folder_id\":13}");
            (bool success, string? details, int channelFolderId) result =
                await client.ChannelFolders.TryCreate("Backend + 雪", "Channels for **backend**.");
            Assert.True(result.success, result.details);
            Assert.Equal(12, result.channelFolderId);
            Assert.Equal(new Dictionary<string, string> { { "name", "Backend + 雪" }, { "description", "Channels for **backend**." } },
                await Utils.ReadForm(requests[0]));
            Assert.Equal(13, await client.ChannelFolders.Create("Frontend", ""));
            Assert.Equal(new Dictionary<string, string> { { "name", "Frontend" }, { "description", "" } },
                await Utils.ReadForm(requests[1]));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/v1/channel_folders/create", request.RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Creation requires its real integer channel_folder_id, never the list model's id.</summary>
        /// <param name="json">The actual response envelope.</param>
        /// <param name="expected">The expected diagnostic.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}", "channel_folder_id")]
        [InlineData("{\"result\":\"success\",\"id\":12}", "channel_folder_id")]
        [InlineData("{\"result\":\"success\",\"channel_folder_id\":null}", "channel_folder_id")]
        [InlineData("{\"result\":\"success\",\"channel_folder_id\":\"12\"}", "channel_folder_id")]
        [InlineData("{\"result\":\"success\",\"channel_folder_id\":1.5}", "channel_folder_id")]
        [InlineData("{\"result\":\"error\",\"msg\":\"Must be an organization administrator\"}", "Must be an organization administrator")]
        public async Task ChannelFolders_Create_RequiresIdentityAndThrows(string json, string expected)
        {
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details, int channelFolderId) result = await client.ChannelFolders.TryCreate("Backend", "");
            Assert.False(result.success);
            Assert.Equal(0, result.channelFolderId);
            Assert.Contains(expected, result.details);
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.ChannelFolders.Create("Backend", ""));
            Assert.Contains(expected, error.Message);
        }

        /// <summary>Lists preserve server ordering, archived state, nullable creator metadata and raw/rendered descriptions.</summary>
        [Fact]
        public async Task ChannelFolders_GetAll_ActualObjectsAndQueryOmission()
        {
            const string json = """
                {"result":"success","channel_folders":[
                  {"id":12,"name":"Backend","order":0,"date_created":1691057093,"creator_id":1,
                   "description":"Channels for **backend**","rendered_description":"<p>Channels for <strong>backend</strong></p>","is_archived":true},
                  {"id":4,"name":"Frontend","date_created":null,"creator_id":null,
                   "description":"","rendered_description":"","is_archived":false}]}
                """;
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json,
                "{\"result\":\"success\",\"channel_folders\":[]}", "{\"result\":\"success\",\"channel_folders\":[]}");
            (bool success, string? details, List<ChannelFolderObject>? channelFolders) result =
                await client.ChannelFolders.TryGetAll(true);
            Assert.True(result.success, result.details);
            Assert.Collection(result.channelFolders!,
                folder =>
                {
                    Assert.Equal(12, folder.Id);
                    Assert.Equal(0, folder.Order);
                    Assert.Equal(1691057093L, folder.DateCreated);
                    Assert.Equal(1, folder.CreatorId);
                    Assert.True(folder.IsArchived);
                    Assert.Equal("Channels for **backend**", folder.Description);
                    Assert.Equal("<p>Channels for <strong>backend</strong></p>", folder.RenderedDescription);
                },
                folder =>
                {
                    Assert.Equal(4, folder.Id);
                    Assert.Null(folder.Order);
                    Assert.Null(folder.DateCreated);
                    Assert.Null(folder.CreatorId);
                    Assert.False(folder.IsArchived);
                    Assert.Equal("", folder.Description);
                });
            Assert.Empty(await client.ChannelFolders.GetAll(false));
            Assert.Empty(await client.ChannelFolders.GetAll());
            Assert.Equal(new Dictionary<string, string> { { "include_archived", "true" } }, Utils.DecodeForm(requests[0].RequestUri!.Query));
            Assert.Equal(new Dictionary<string, string> { { "include_archived", "false" } }, Utils.DecodeForm(requests[1].RequestUri!.Query));
            Assert.Equal("", requests[2].RequestUri!.Query);
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v1/channel_folders", request.RequestUri!.AbsolutePath);
                Assert.Empty(await Utils.ReadForm(request));
            }
        }

        /// <summary>Missing collections, list identities and actual archive Booleans cannot be replaced by CLR defaults.</summary>
        /// <param name="json">The actual response envelope.</param>
        /// <param name="expected">The expected diagnostic.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}", "channel_folders")]
        [InlineData("{\"result\":\"success\",\"channel_folders\":null}", "channel_folders")]
        [InlineData("{\"result\":\"success\",\"channel_folders\":{}}", "channel_folders")]
        [InlineData("{\"result\":\"success\",\"channel_folders\":[{\"channel_folder_id\":12}]}", "id")]
        [InlineData("{\"result\":\"success\",\"channel_folders\":[{\"id\":12,\"name\":\"Backend\",\"description\":\"\",\"rendered_description\":\"\"}]}", "is_archived")]
        [InlineData("{\"result\":\"success\",\"channel_folders\":[{\"id\":12,\"name\":\"Backend\",\"description\":\"\",\"rendered_description\":\"\",\"is_archived\":\"false\"}]}", "is_archived")]
        [InlineData("{\"result\":\"error\",\"msg\":\"Permission denied\"}", "Permission denied")]
        public async Task ChannelFolders_GetAll_RequiresActualPayloadAndThrows(string json, string expected)
        {
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details, List<ChannelFolderObject>? channelFolders) result = await client.ChannelFolders.TryGetAll();
            Assert.False(result.success);
            Assert.Null(result.channelFolders);
            Assert.Contains(expected, result.details);
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.ChannelFolders.GetAll());
            Assert.Contains(expected, error.Message);
        }

        /// <summary>Reorder sends the explicit complete ID order, including archived folders, and allows an empty organization.</summary>
        [Fact]
        public async Task ChannelFolders_Reorder_ActualJsonOrderAndEmpty()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            Assert.True((await client.ChannelFolders.TryReorder([12, 4, 19])).success);
            await client.ChannelFolders.Reorder([]);
            Assert.Equal(new Dictionary<string, string> { { "order", "[12,4,19]" } }, await Utils.ReadForm(requests[0]));
            Assert.Equal(new Dictionary<string, string> { { "order", "[]" } }, await Utils.ReadForm(requests[1]));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Patch, request.Method);
                Assert.Equal("/api/v1/channel_folders", request.RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Folder updates preserve explicit unarchive false, empty descriptions and omission of untouched settings.</summary>
        [Fact]
        public async Task ChannelFolders_Update_ArchiveUnarchiveAndOmission()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\"}", "{\"result\":\"success\"}", "{\"result\":\"success\"}");
            Assert.True((await client.ChannelFolders.TryUpdate(12, name: "Backend + 雪", description: "", isArchived: true)).success);
            await client.ChannelFolders.Update(12, isArchived: false);
            await client.ChannelFolders.Update(12, description: "Channels for **backend**.");
            Assert.Equal(new Dictionary<string, string> { { "name", "Backend + 雪" }, { "description", "" }, { "is_archived", "true" } },
                await Utils.ReadForm(requests[0]));
            Assert.Equal(new Dictionary<string, string> { { "is_archived", "false" } }, await Utils.ReadForm(requests[1]));
            Assert.Equal(new Dictionary<string, string> { { "description", "Channels for **backend**." } }, await Utils.ReadForm(requests[2]));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Patch, request.Method);
                Assert.Equal("/api/v1/channel_folders/12", request.RequestUri!.AbsolutePath);
            }
        }

        /// <summary>Rejected order mappings and updates remain explicit failures in both API styles.</summary>
        /// <param name="reorder">Whether the operation is a reorder rather than an update.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ChannelFolders_Mutations_ServerErrorAndThrowingPairs(bool reorder)
        {
            const string json = "{\"result\":\"error\",\"msg\":\"Must be an organization administrator\",\"code\":\"UNAUTHORIZED_PRINCIPAL\"}";
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details) result = reorder
                ? await client.ChannelFolders.TryReorder([12, 4])
                : await client.ChannelFolders.TryUpdate(12, isArchived: false);
            Assert.False(result.success);
            Assert.Contains("Must be an organization administrator", result.details);
            Exception error = await Assert.ThrowsAsync<Exception>(() => reorder
                ? client.ChannelFolders.Reorder([12, 4])
                : client.ChannelFolders.Update(12, isArchived: false));
            Assert.Contains("Must be an organization administrator", error.Message);
        }

        /// <summary>Null required inputs, invalid IDs and duplicate order entries do not invoke the server.</summary>
        [Fact]
        public async Task ChannelFolders_InvalidInputs_NoRequests()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            Assert.False((await client.ChannelFolders.TryCreate("", "")).success);
            Assert.False((await client.ChannelFolders.TryCreate("Backend", null!)).success);
            Assert.False((await client.ChannelFolders.TryReorder(null!)).success);
            Assert.False((await client.ChannelFolders.TryReorder([12, 12])).success);
            Assert.False((await client.ChannelFolders.TryReorder([0])).success);
            Assert.False((await client.ChannelFolders.TryUpdate(0, isArchived: true)).success);
            Assert.False((await client.ChannelFolders.TryUpdate(12, name: "")).success);
            await Assert.ThrowsAsync<Exception>(() => client.ChannelFolders.Create("Backend", null!));
            await Assert.ThrowsAsync<Exception>(() => client.ChannelFolders.Reorder([-1]));
            await Assert.ThrowsAsync<Exception>(() => client.ChannelFolders.Update(-1, isArchived: false));
            Assert.Empty(requests);
        }

        /// <summary>Body-only injected responses work, and successful-looking envelopes with failed HTTP status do not.</summary>
        [Fact]
        public async Task ChannelFolders_InjectedDelegate_ValidatesEnvelopeAndPayload()
        {
            Queue<ZulipResponse> responses = new Queue<ZulipResponse>(
            [
                new ZulipResponse { Result = "success", HttpResponseBody = "{\"result\":\"success\",\"channel_folder_id\":12}" },
                new ZulipResponse { Result = "success", HttpResponseCode = 403 }
            ]);
            ChannelFolders folders = new ChannelFolders((method, endpoint, data) => Task.FromResult(responses.Dequeue()));
            Assert.Equal(12, await folders.Create("Backend", ""));
            Assert.False((await folders.TryUpdate(12, isArchived: false)).success);
        }

        /// <summary>User-channel lookup returns IDs rather than stream objects, including a legitimate empty list.</summary>
        [Fact]
        public async Task Channels_GetUserChannels_ActualIdsAndEmpty()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                "{\"result\":\"success\",\"subscribed_channel_ids\":[3,5,10]}",
                "{\"result\":\"success\",\"subscribed_channel_ids\":[]}");
            (bool success, string? details, List<int>? subscribedChannelIds) result = await client.Channels.TryGetUserChannels(12);
            Assert.True(result.success, result.details);
            Assert.Equal(new int[] { 3, 5, 10 }, result.subscribedChannelIds);
            Assert.Empty(await client.Channels.GetUserChannels(12));
            foreach (HttpRequestMessage request in requests)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/v1/users/12/channels", request.RequestUri!.AbsolutePath);
                Assert.Equal("", request.RequestUri.Query);
                Assert.Empty(await Utils.ReadForm(request));
            }
        }

        /// <summary>A missing or malformed channel-ID list, and permission errors, are never an empty successful result.</summary>
        /// <param name="json">The actual response envelope.</param>
        /// <param name="expected">The expected diagnostic.</param>
        [Theory]
        [InlineData("{\"result\":\"success\"}", "subscribed_channel_ids")]
        [InlineData("{\"result\":\"success\",\"channels\":[]}", "subscribed_channel_ids")]
        [InlineData("{\"result\":\"success\",\"subscribed_channel_ids\":null}", "subscribed_channel_ids")]
        [InlineData("{\"result\":\"success\",\"subscribed_channel_ids\":[\"3\"]}", "subscribed_channel_ids")]
        [InlineData("{\"result\":\"success\",\"subscribed_channel_ids\":[1.5]}", "subscribed_channel_ids")]
        [InlineData("{\"result\":\"error\",\"msg\":\"Insufficient permission\"}", "Insufficient permission")]
        public async Task Channels_GetUserChannels_RequiresIntegerArrayAndThrows(string json, string expected)
        {
            ZulipClient client = Utils.CreateCapturingClient([], json, json);
            (bool success, string? details, List<int>? subscribedChannelIds) result = await client.Channels.TryGetUserChannels(12);
            Assert.False(result.success);
            Assert.Null(result.subscribedChannelIds);
            Assert.Contains(expected, result.details);
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.Channels.GetUserChannels(12));
            Assert.Contains(expected, error.Message);
        }

        /// <summary>An invalid target user ID is rejected before attempting the lookup.</summary>
        [Fact]
        public async Task Channels_GetUserChannels_InvalidIdNoRequest()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            Assert.False((await client.Channels.TryGetUserChannels(0)).success);
            await Assert.ThrowsAsync<Exception>(() => client.Channels.GetUserChannels(-1));
            Assert.Empty(requests);
        }
    }
}
