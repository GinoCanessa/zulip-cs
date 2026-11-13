using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Resources;

namespace zulip_set_lib.tests
{
    /// <summary>Contract tests for documented bot-account string storage.</summary>
    public class BotStorageTests
    {
        /// <summary>Selected keys are JSON query values and returned strings are not interpreted as JSON.</summary>
        [Fact]
        public async Task Get_EncodesSelectedKeysAndPreservesStrings()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"success","msg":"","storage":{"state & café":"{\"nested\":[1,false]}","empty":""}}""");

            var result = await client.BotStorage.TryGet(["state & café", "empty"]);

            Assert.True(result.success, result.details);
            Assert.Equal("{\"nested\":[1,false]}", result.storage!["state & café"]);
            Assert.Equal(string.Empty, result.storage["empty"]);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/bot_storage", request.RequestUri!.AbsolutePath);
            Dictionary<string, string> query = Utils.DecodeForm(request.RequestUri.Query);
            Assert.Equal(new[] { "state & café", "empty" }, JsonSerializer.Deserialize<string[]>(query["keys"]));
            Assert.Null(request.Content);
        }

        /// <summary>Get distinguishes omitted keys from an explicitly empty selection and permits empty maps.</summary>
        [Fact]
        public async Task Get_OmittedAndEmptyKeysAreDistinct()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"success","msg":"","storage":{}}""",
                """{"result":"success","msg":"","storage":{}}""");

            Assert.Empty(await client.BotStorage.Get());
            var selected = await client.BotStorage.TryGet([]);

            Assert.True(selected.success, selected.details);
            Assert.Empty(selected.storage!);
            Assert.Equal(string.Empty, requests[0].RequestUri!.Query);
            Assert.Equal("[]", Utils.DecodeForm(requests[1].RequestUri!.Query)["keys"]);
        }

        /// <summary>Missing maps and non-string wire values fail rather than becoming empty data or coerced strings.</summary>
        /// <param name="json">The malformed success response.</param>
        [Theory]
        [InlineData("""{"result":"success","msg":""}""")]
        [InlineData("""{"result":"success","storage":null}""")]
        [InlineData("""{"result":"success","storage":[]}""")]
        [InlineData("""{"result":"success","storage":{"key":null}}""")]
        [InlineData("""{"result":"success","storage":{"key":7}}""")]
        [InlineData("""{"result":"success","storage":{"key":false}}""")]
        [InlineData("""{"result":"success","storage":{"key":{"nested":"value"}}}""")]
        public async Task Get_MalformedSuccessFailsTryAndThrows(string json)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);

            var result = await client.BotStorage.TryGet();

            Assert.False(result.success);
            Assert.Null(result.storage);
            Assert.Contains("BotStorage.Get failed:", result.details!);
            Assert.Contains("storage", result.details!);
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.BotStorage.Get());
            Assert.Equal(result.details, error.Message);
        }

        /// <summary>Updates send the documented PUT form with string values and allow explicit empty maps.</summary>
        [Fact]
        public async Task Update_UsesPutFormAndDoesNotRequireAnInventedPayload()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"success","msg":""}""", """{"result":"success","msg":""}""");
            Dictionary<string, string> storage = new Dictionary<string, string>
            {
                { "state & café", "{\"nested\":[1,false]}" }, { "empty", "" }
            };

            var result = await client.BotStorage.TryUpdate(storage);
            await client.BotStorage.Update(new Dictionary<string, string>());

            Assert.True(result.success, result.details);
            Assert.Equal(HttpMethod.Put, requests[0].Method);
            Assert.Equal("/api/v1/bot_storage", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal(string.Empty, requests[0].RequestUri!.Query);
            Dictionary<string, string> form = await Utils.ReadForm(requests[0]);
            Dictionary<string, string> captured = JsonSerializer.Deserialize<Dictionary<string, string>>(form["storage"])!;
            Assert.Equal(storage["state & café"], captured["state & café"]);
            Assert.Equal(string.Empty, captured["empty"]);
            Assert.Single(form);
            Assert.Equal("{}", (await Utils.ReadForm(requests[1]))["storage"]);
        }

        /// <summary>Deletion always sends an explicit key list, including the valid empty-list no-op.</summary>
        [Fact]
        public async Task Delete_AlwaysSendsAnExplicitJsonKeyList()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"success","msg":""}""", """{"result":"success","msg":""}""");

            var result = await client.BotStorage.TryDelete(["state & café", ""]);
            await client.BotStorage.Delete([]);

            Assert.True(result.success, result.details);
            Assert.Equal(HttpMethod.Delete, requests[0].Method);
            Assert.Equal("/api/v1/bot_storage", requests[0].RequestUri!.AbsolutePath);
            Dictionary<string, string> form = await Utils.ReadForm(requests[0]);
            Assert.Equal(new[] { "state & café", "" }, JsonSerializer.Deserialize<string[]>(form["keys"]));
            Assert.Single(form);
            Assert.Equal("[]", (await Utils.ReadForm(requests[1]))["keys"]);
        }

        /// <summary>Null maps, null values and implicit delete-all calls are rejected without making a request.</summary>
        [Fact]
        public async Task InvalidInputs_DoNotMakeRequestsOrDeleteAll()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);

            Assert.False((await client.BotStorage.TryUpdate(null!)).success);
            Assert.False((await client.BotStorage.TryUpdate(new Dictionary<string, string> { { "key", null! } })).success);
            var deletion = await client.BotStorage.TryDelete(null!);
            Assert.False(deletion.success);
            Assert.Contains("explicit list", deletion.details!);
            Assert.False((await client.BotStorage.TryDelete([null!])).success);
            Assert.False((await client.BotStorage.TryGet([null!])).success);
            await Assert.ThrowsAsync<Exception>(() => client.BotStorage.Update(null!));
            await Assert.ThrowsAsync<Exception>(() => client.BotStorage.Delete(null!));
            Assert.Empty(requests);
        }

        /// <summary>Server-enforced bot-only restrictions remain explicit for Try and throwing variants.</summary>
        /// <param name="operation">The storage operation.</param>
        [Theory]
        [InlineData("get")]
        [InlineData("update")]
        [InlineData("delete")]
        public async Task PermissionDenied_IsNotHiddenOrLocallyProbed(string operation)
        {
            const string json = """{"result":"error","code":"PERMISSION_DENIED","msg":"Must be a bot user"}""";
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);
            string? details;
            if (operation == "get")
            {
                var result = await client.BotStorage.TryGet();
                Assert.False(result.success);
                Assert.Null(result.storage);
                details = result.details;
                Exception error = await Assert.ThrowsAsync<Exception>(() => client.BotStorage.Get());
                Assert.Equal(details, error.Message);
            }
            else
            {
                var result = operation == "update"
                    ? await client.BotStorage.TryUpdate(new Dictionary<string, string> { { "foo", "bar" } })
                    : await client.BotStorage.TryDelete(["foo"]);
                Assert.False(result.success);
                details = result.details;
                Exception error = operation == "update"
                    ? await Assert.ThrowsAsync<Exception>(() => client.BotStorage.Update(new Dictionary<string, string> { { "foo", "bar" } }))
                    : await Assert.ThrowsAsync<Exception>(() => client.BotStorage.Delete(["foo"]));
                Assert.Equal(details, error.Message);
            }
            Assert.Contains("PERMISSION_DENIED", details!);
            Assert.Contains("Must be a bot user", details!);
            Assert.Equal(2, requests.Count);
            Assert.All(requests, request => Assert.Equal("/api/v1/bot_storage", request.RequestUri!.AbsolutePath));
        }

        /// <summary>A missing selected key and storage-limit failures retain the server's specific diagnostics.</summary>
        [Fact]
        public async Task ServerFailures_PreserveMissingKeyAndLimitDetails()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"error","code":"BAD_REQUEST","msg":"Key does not exist."}""",
                """{"result":"error","code":"BAD_REQUEST","msg":"Request exceeds storage limit by 42 characters. The limit is 10000000 characters."}""",
                """{"result":"error","code":"BAD_REQUEST","msg":"Key does not exist."}""");

            var missing = await client.BotStorage.TryGet(["absent"]);
            var limit = await client.BotStorage.TryUpdate(new Dictionary<string, string> { { "foo", "bar" } });
            var deletion = await client.BotStorage.TryDelete(["absent"]);

            Assert.False(missing.success);
            Assert.Null(missing.storage);
            Assert.Contains("Key does not exist.", missing.details!);
            Assert.False(limit.success);
            Assert.Contains("exceeds storage limit", limit.details!);
            Assert.False(deletion.success);
            Assert.Contains("Key does not exist.", deletion.details!);
        }

        /// <summary>Body-only injected delegates work without transport flags or shared response-model edits.</summary>
        [Fact]
        public async Task Get_AcceptsInjectedBodyOnlySuccess()
        {
            BotStorage storage = new BotStorage((_, _, _) => Task.FromResult(new ZulipResponse
            {
                Result = "success", HttpResponseBody = """{"result":"success","storage":{"foo":"bar"}}"""
            }));

            Assert.Equal("bar", (await storage.Get())["foo"]);
        }
    }
}
