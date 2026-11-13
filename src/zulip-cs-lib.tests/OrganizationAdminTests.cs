using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using zulip_cs_lib;
using zulip_cs_lib.Models;
using zulip_cs_lib.Resources;

namespace zulip_set_lib.tests
{
    /// <summary>Server-shaped contract tests for selected organization administration.</summary>
    public class OrganizationAdminTests
    {
        /// <summary>The documented payload-free mutation success envelope.</summary>
        private const string Success = """{"result":"success","msg":""}""";

        /// <summary>Profile update reuses typed options and preserves explicit false values and empty hints.</summary>
        [Fact]
        public async Task UpdateProfileField_SendsOnlySelectedMutableFields()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, Success);
            ProfileFieldOptions options = new ProfileFieldOptions
            {
                FieldData = JsonSerializer.SerializeToElement(new { python = new { order = "1", text = "Python" } }),
                DisplayInProfileSummary = false,
                Required = true,
                EditableByUser = false,
                UseForUserMatching = false
            };

            var result = await client.Organization.TryUpdateProfileField(4, hint: "", options: options);

            Assert.True(result.success, result.details);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Patch, request.Method);
            Assert.Equal("/api/v1/realm/profile_fields/4", request.RequestUri!.AbsolutePath);
            Dictionary<string, string> form = await Utils.ReadForm(request);
            Assert.Equal("", form["hint"]);
            Assert.Equal("false", form["display_in_profile_summary"]);
            Assert.Equal("true", form["required"]);
            Assert.Equal("false", form["editable_by_user"]);
            Assert.Equal("false", form["use_for_user_matching"]);
            Assert.Equal("Python", JsonSerializer.Deserialize<JsonElement>(form["field_data"])
                .GetProperty("python").GetProperty("text").GetString());
            Assert.False(form.ContainsKey("name"));
            Assert.False(form.ContainsKey("field_type"));
            Assert.Equal(6, form.Count);
        }

        /// <summary>Deletion and reordering use their distinct documented endpoints and exact JSON form fields.</summary>
        [Fact]
        public async Task ProfileAndLinkifierOrder_PreservesOrderAndExplicitEmptyLists()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, Success, Success, Success, Success);

            await client.Organization.DeleteProfileField(4);
            await client.Organization.ReorderProfileFields([4, 2, 3]);
            Assert.True((await client.Organization.TryReorderProfileFields([])).success);
            await client.Organization.ReorderLinkifiers([3, 1, 2]);

            Assert.Equal(HttpMethod.Delete, requests[0].Method);
            Assert.Equal("/api/v1/realm/profile_fields/4", requests[0].RequestUri!.AbsolutePath);
            Assert.Empty(await Utils.ReadForm(requests[0]));
            Assert.Equal(HttpMethod.Patch, requests[1].Method);
            Assert.Equal("/api/v1/realm/profile_fields", requests[1].RequestUri!.AbsolutePath);
            Assert.Equal("[4,2,3]", (await Utils.ReadForm(requests[1]))["order"]);
            Assert.Equal("[]", (await Utils.ReadForm(requests[2]))["order"]);
            Assert.Equal(HttpMethod.Patch, requests[3].Method);
            Assert.Equal("/api/v1/realm/linkifiers", requests[3].RequestUri!.AbsolutePath);
            Assert.Equal("[3,1,2]", (await Utils.ReadForm(requests[3]))["ordered_linkifier_ids"]);
        }

        /// <summary>Invalid profile configuration and null orders fail locally without inventing request defaults.</summary>
        [Fact]
        public async Task ProfileAndOrder_InvalidInputsDoNotMakeRequests()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);
            ProfileFieldOptions options = new ProfileFieldOptions { FieldData = JsonSerializer.SerializeToElement(new[] { 1 }) };

            var result = await client.Organization.TryUpdateProfileField(4, options: options);

            Assert.False(result.success);
            Assert.Contains("field_data", result.details!);
            await Assert.ThrowsAsync<Exception>(() => client.Organization.UpdateProfileField(4, options: options));
            Assert.False((await client.Organization.TryReorderProfileFields(null!)).success);
            Assert.False((await client.Organization.TryReorderLinkifiers(null!)).success);
            Assert.Empty(requests);
        }

        /// <summary>Emoji uploads route encoded logical names and all caller-owned multipart arguments to the focused delegate.</summary>
        [Fact]
        public async Task UploadCustomEmoji_UsesMultipartDelegateAndPreservesCallerOwnership()
        {
            string? endpoint = null;
            string? field = null;
            Stream? input = null;
            string? filename = null;
            string? mime = null;
            CancellationToken observedToken = default;
            int ordinaryRequests = 0;
            int uploadRequests = 0;
            Organization organization = new Organization((_, _, _) =>
            {
                ordinaryRequests++;
                throw new InvalidOperationException("Emoji must use the multipart delegate.");
            }, (path, fieldName, stream, uploadFilename, contentType, token) =>
            {
                uploadRequests++;
                endpoint = path;
                field = fieldName;
                input = stream;
                filename = uploadFilename;
                mime = contentType;
                observedToken = token;
                return Task.FromResult(new ZulipResponse { Result = "success" });
            });
            using MemoryStream image = new MemoryStream([1, 2, 3]);
            using CancellationTokenSource cancellation = new CancellationTokenSource();

            var result = await organization.TryUploadCustomEmoji("Café happy", image, "image.png", "image/png", cancellation.Token);

            Assert.True(result.success, result.details);
            Assert.Equal("api/v1/realm/emoji/Caf%C3%A9%20happy", endpoint);
            Assert.Equal("filename", field);
            Assert.Same(image, input);
            Assert.Equal("image.png", filename);
            Assert.Equal("image/png", mime);
            Assert.Equal(cancellation.Token, observedToken);
            Assert.True(image.CanRead);
            Assert.Equal(0, image.Position);
            Assert.Equal(0, ordinaryRequests);
            Assert.Equal(1, uploadRequests);

            await organization.UploadCustomEmoji("literal%2Fname?query#fragment", image, "image.png",
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("api/v1/realm/emoji/literal%252Fname%3Fquery%23fragment", endpoint);
            Assert.Equal("application/octet-stream", mime);
        }

        /// <summary>Upload permission failures remain explicit and no multipart response payload is invented.</summary>
        [Fact]
        public async Task UploadCustomEmoji_PermissionAndMissingDelegateFailExplicitly()
        {
            Organization denied = new Organization((_, _, _) => throw new InvalidOperationException(),
                (_, _, _, _, _, _) => Task.FromResult(new ZulipResponse
                {
                    Result = "error", ErrorCode = "PERMISSION_DENIED", Message = "Insufficient permission"
                }));
            using MemoryStream image = new MemoryStream([1]);

            var result = await denied.TryUploadCustomEmoji("smile", image, "image.png",
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.success);
            Assert.Contains("PERMISSION_DENIED", result.details!);
            Exception error = await Assert.ThrowsAsync<Exception>(() => denied.UploadCustomEmoji("smile", image, "image.png",
                cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(result.details, error.Message);
            Assert.True(image.CanRead);
            Organization unavailable = new Organization((_, _, _) => throw new InvalidOperationException());
            var missing = await unavailable.TryUploadCustomEmoji("smile", image, "image.png",
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(missing.success);
            Assert.Contains("multipart upload delegate", missing.details!);
        }

        /// <summary>Shared multipart validation rejects invalid inputs before either delegate is invoked.</summary>
        [Fact]
        public async Task UploadCustomEmoji_InvalidInputsAreRejectedWithoutRequests()
        {
            int requests = 0;
            Organization organization = new Organization((_, _, _) => throw new InvalidOperationException(),
                (_, _, _, _, _, _) =>
                {
                    requests++;
                    return Task.FromResult(new ZulipResponse { Result = "success" });
                });
            using MemoryStream image = new MemoryStream([1]);

            Assert.False((await organization.TryUploadCustomEmoji("", image, "image.png",
                cancellationToken: TestContext.Current.CancellationToken)).success);
            Assert.False((await organization.TryUploadCustomEmoji("..", image, "image.png",
                cancellationToken: TestContext.Current.CancellationToken)).success);
            Assert.False((await organization.TryUploadCustomEmoji("smile", null!, "image.png",
                cancellationToken: TestContext.Current.CancellationToken)).success);
            Assert.False((await organization.TryUploadCustomEmoji("smile", image, "",
                cancellationToken: TestContext.Current.CancellationToken)).success);
            Assert.False((await organization.TryUploadCustomEmoji("smile", image, "image.png", "bad\r\nmime",
                TestContext.Current.CancellationToken)).success);
            Assert.Equal(0, requests);
            Assert.True(image.CanRead);
        }

        /// <summary>Allowed domains retain false policies, heterogeneous creation identity and encoded update/delete paths.</summary>
        [Fact]
        public async Task AllowedDomains_ListAndLifecycleMatchServerContract()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"success","msg":"","domains":[{"domain":"exämple.org","allow_subdomains":false}]}""",
                """{"result":"success","msg":"","new_domain":[17,"exämple.org"]}""", Success, Success);

            List<OrganizationAllowedDomain> domains = await client.Organization.GetAllowedDomains();
            var created = await client.Organization.TryAddAllowedDomain("exämple.org", false);
            await client.Organization.UpdateAllowedDomain("exämple.org", true);
            await client.Organization.DeleteAllowedDomain("exämple.org");

            Assert.False(Assert.Single(domains).AllowSubdomains);
            Assert.Equal("exämple.org", domains[0].Domain);
            Assert.True(created.success, created.details);
            Assert.Equal(17, created.domain!.Id);
            Assert.Equal("exämple.org", created.domain.Domain);
            Assert.Equal(HttpMethod.Get, requests[0].Method);
            Assert.Equal("/api/v1/realm/domains", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal(HttpMethod.Post, requests[1].Method);
            Dictionary<string, string> createForm = await Utils.ReadForm(requests[1]);
            Assert.Equal("exämple.org", createForm["domain"]);
            Assert.Equal("false", createForm["allow_subdomains"]);
            Assert.Equal(HttpMethod.Patch, requests[2].Method);
            Assert.Equal("/api/v1/realm/domains/ex%C3%A4mple.org", requests[2].RequestUri!.AbsolutePath);
            Assert.Equal("true", (await Utils.ReadForm(requests[2]))["allow_subdomains"]);
            Assert.Equal(HttpMethod.Delete, requests[3].Method);
            Assert.Equal(requests[2].RequestUri!.AbsolutePath, requests[3].RequestUri!.AbsolutePath);
            Assert.Empty(await Utils.ReadForm(requests[3]));
        }

        /// <summary>Missing domain collections and missing policy booleans do not silently become empty or false.</summary>
        /// <param name="json">The malformed response.</param>
        [Theory]
        [InlineData("""{"result":"success"}""")]
        [InlineData("""{"result":"success","domains":null}""")]
        [InlineData("""{"result":"success","domains":{}}""")]
        [InlineData("""{"result":"success","domains":[{"allow_subdomains":false}]}""")]
        [InlineData("""{"result":"success","domains":[{"domain":"example.com"}]}""")]
        [InlineData("""{"result":"success","domains":[{"domain":"example.com","allow_subdomains":"false"}]}""")]
        public async Task AllowedDomains_MalformedSuccessFailsTryAndThrows(string json)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);

            var result = await client.Organization.TryGetAllowedDomains();

            Assert.False(result.success);
            Assert.Null(result.domains);
            Assert.Contains("Organization.GetAllowedDomains failed:", result.details!);
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.Organization.GetAllowedDomains());
            Assert.Equal(result.details, error.Message);
        }

        /// <summary>Domain creation requires the actual identity and domain tuple, not just success.</summary>
        /// <param name="json">The malformed creation response.</param>
        [Theory]
        [InlineData("""{"result":"success"}""")]
        [InlineData("""{"result":"success","new_domain":[]}""")]
        [InlineData("""{"result":"success","new_domain":[1]}""")]
        [InlineData("""{"result":"success","new_domain":["1","example.com"]}""")]
        [InlineData("""{"result":"success","new_domain":[1,null]}""")]
        [InlineData("""{"result":"success","new_domain":[1,""]}""")]
        public async Task AddAllowedDomain_MissingIdentityFailsTryAndThrows(string json)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);

            var result = await client.Organization.TryAddAllowedDomain("example.com", false);

            Assert.False(result.success);
            Assert.Null(result.domain);
            Assert.Contains("new_domain", result.details!);
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.Organization.AddAllowedDomain("example.com", false));
            Assert.Equal(result.details, error.Message);
        }

        /// <summary>Export records preserve pending, prior-server state, null URLs and fractional timestamps.</summary>
        [Fact]
        public async Task GetExports_PreservesServerStateWithoutPolling()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """
                {"result":"success","msg":"","exports":[
                  {"id":323,"acting_user_id":11,"export_time":1722243168.134179,"deleted_timestamp":null,
                   "failed_timestamp":null,"export_url":null,"pending":true,"export_from_prior_server":false,
                   "export_type":"full_with_consent"},
                  {"id":322,"acting_user_id":6,"export_time":1722243167.25,"deleted_timestamp":null,
                   "failed_timestamp":null,"export_url":null,"pending":false,"export_from_prior_server":true,
                   "export_type":"public"}
                ]}
                """);

            List<OrganizationExport> exports = await client.Organization.GetExports();

            Assert.Equal(2, exports.Count);
            Assert.Equal(323, exports[0].Id);
            Assert.Equal(11, exports[0].ActingUserId);
            Assert.Equal(1722243168.134179, exports[0].ExportTime);
            Assert.True(exports[0].Pending);
            Assert.Equal("full_with_consent", exports[0].ExportType);
            Assert.False(exports[0].ExportFromPriorServer!.Value);
            Assert.True(exports[1].ExportFromPriorServer!.Value);
            Assert.False(exports[1].Pending);
            Assert.Null(exports[1].ExportUrl);
            Assert.Null(exports[1].DeletedTimestamp);
            Assert.Null(exports[1].FailedTimestamp);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/export/realm", request.RequestUri!.AbsolutePath);
        }

        /// <summary>Missing required export state fields cannot become successful CLR default values.</summary>
        /// <param name="field">The required field to omit.</param>
        [Theory]
        [InlineData("id")]
        [InlineData("acting_user_id")]
        [InlineData("export_time")]
        [InlineData("deleted_timestamp")]
        [InlineData("failed_timestamp")]
        [InlineData("export_url")]
        [InlineData("pending")]
        [InlineData("export_type")]
        public async Task GetExports_MissingRequiredStateFailsTryAndThrows(string field)
        {
            Dictionary<string, object?> export = new Dictionary<string, object?>
            {
                { "id", 1 }, { "acting_user_id", 11 }, { "export_time", 1722243168.134179 },
                { "deleted_timestamp", null }, { "failed_timestamp", null }, { "export_url", null },
                { "pending", false }, { "export_type", "public" }
            };
            export.Remove(field);
            string json = JsonSerializer.Serialize(new { result = "success", exports = new[] { export } });
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);

            var result = await client.Organization.TryGetExports();

            Assert.False(result.success);
            Assert.Null(result.exports);
            Assert.Contains(field, result.details!);
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.Organization.GetExports());
            Assert.Equal(result.details, error.Message);
        }

        /// <summary>Exports queue creation with a string selection and require an actual returned ID; deletion is envelope-only.</summary>
        [Fact]
        public async Task ExportLifecycle_UsesDocumentedTypesAndSuccessRequirements()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"success","msg":"","id":323}""",
                """{"result":"success","msg":"","id":324}""", Success, Success, Success);

            Assert.Equal(323, await client.Organization.CreateExport(OrganizationExportType.FullWithConsent));
            var defaultExport = await client.Organization.TryCreateExport();
            Assert.True(defaultExport.success, defaultExport.details);
            Assert.Equal(324, defaultExport.exportId);
            await client.Organization.DeleteExport(323);
            var missing = await client.Organization.TryCreateExport();
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.Organization.CreateExport());

            Assert.False(missing.success);
            Assert.Equal(0, missing.exportId);
            Assert.Equal(missing.details, error.Message);
            Assert.Contains("'id'", missing.details!);
            Assert.Equal(HttpMethod.Post, requests[0].Method);
            Assert.Equal("/api/v1/export/realm", requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("full_with_consent", (await Utils.ReadForm(requests[0]))["export_type"]);
            Assert.Empty(await Utils.ReadForm(requests[1]));
            Assert.Equal(HttpMethod.Delete, requests[2].Method);
            Assert.Equal("/api/v1/export/realm/323", requests[2].RequestUri!.AbsolutePath);
            Assert.Empty(await Utils.ReadForm(requests[2]));
        }

        /// <summary>Consent payloads retain explicit false consent and modern email visibility.</summary>
        [Fact]
        public async Task GetExportConsents_PreservesFalseAndVisibility()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"success","export_consents":[{"user_id":11,"consented":true,"email_address_visibility":1},{"user_id":6,"consented":false,"email_address_visibility":2}]}""");

            List<OrganizationExportConsent> consents = await client.Organization.GetExportConsents();

            Assert.Equal(2, consents.Count);
            Assert.True(consents[0].Consented);
            Assert.False(consents[1].Consented);
            Assert.Equal(6, consents[1].UserId);
            Assert.Equal(2, consents[1].EmailAddressVisibility);
            HttpRequestMessage request = Assert.Single(requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/export/realm/consents", request.RequestUri!.AbsolutePath);
        }

        /// <summary>Consent booleans and identities must actually be present, and malformed modern visibility fails.</summary>
        /// <param name="json">The malformed consent response.</param>
        [Theory]
        [InlineData("""{"result":"success"}""")]
        [InlineData("""{"result":"success","export_consents":null}""")]
        [InlineData("""{"result":"success","export_consents":[{"consented":false}]}""")]
        [InlineData("""{"result":"success","export_consents":[{"user_id":11}]}""")]
        [InlineData("""{"result":"success","export_consents":[{"user_id":11,"consented":"false"}]}""")]
        [InlineData("""{"result":"success","export_consents":[{"user_id":11,"consented":false,"email_address_visibility":null}]}""")]
        public async Task GetExportConsents_MalformedSuccessFailsTryAndThrows(string json)
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, json, json);

            var result = await client.Organization.TryGetExportConsents();

            Assert.False(result.success);
            Assert.Null(result.consents);
            Exception error = await Assert.ThrowsAsync<Exception>(() => client.Organization.GetExportConsents());
            Assert.Equal(result.details, error.Message);
        }

        /// <summary>Required empty collection payloads are valid and do not trigger retries.</summary>
        [Fact]
        public async Task Lists_AcceptEmptyServerArrays()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests,
                """{"result":"success","domains":[]}""",
                """{"result":"success","exports":[]}""",
                """{"result":"success","export_consents":[]}""");

            Assert.Empty(await client.Organization.GetAllowedDomains());
            Assert.Empty(await client.Organization.GetExports());
            Assert.Empty(await client.Organization.GetExportConsents());
            Assert.Equal(3, requests.Count);
        }

        /// <summary>New-member defaults send only selected supported preferences, retaining false and omitting unsupported identity fields.</summary>
        [Fact]
        public async Task UpdateUserSettingsDefaults_SendsOnlySupportedSelectedPreferences()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, Success,
                """{"result":"success","msg":"","ignored_parameters_unsupported":[]}""");
            RealmUserSettingsDefaultsOptions options = new RealmUserSettingsDefaultsOptions
            {
                EnableDesktopNotifications = false, EnableOfflineEmailNotifications = true,
                EnableStreamPushNotifications = false, EnableDigestEmails = false,
                TwentyFourHourTime = true, EnterSends = false
            };

            var result = await client.Organization.TryUpdateUserSettingsDefaults(options);
            await client.Organization.UpdateUserSettingsDefaults(new RealmUserSettingsDefaultsOptions());

            Assert.True(result.success, result.details);
            Assert.Equal(HttpMethod.Patch, requests[0].Method);
            Assert.Equal("/api/v1/realm/user_settings_defaults", requests[0].RequestUri!.AbsolutePath);
            Dictionary<string, string> form = await Utils.ReadForm(requests[0]);
            Assert.Equal("false", form["enable_desktop_notifications"]);
            Assert.Equal("true", form["enable_offline_email_notifications"]);
            Assert.Equal("false", form["enable_stream_push_notifications"]);
            Assert.Equal("false", form["enable_digest_emails"]);
            Assert.Equal("true", form["twenty_four_hour_time"]);
            Assert.Equal("false", form["enter_sends"]);
            Assert.Equal(6, form.Count);
            Assert.False(form.ContainsKey("enable_stream_email_notifications"));
            Assert.False(form.ContainsKey("timezone"));
            Assert.False(form.ContainsKey("default_language"));
            Assert.Empty(await Utils.ReadForm(requests[1]));
        }

        /// <summary>Null defaults and invalid export selections are explicit local failures with no request.</summary>
        [Fact]
        public async Task InvalidAdminInputs_DoNotMakeRequests()
        {
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests);

            Assert.False((await client.Organization.TryUpdateUserSettingsDefaults(null!)).success);
            await Assert.ThrowsAsync<Exception>(() => client.Organization.UpdateUserSettingsDefaults(null!));
            Assert.False((await client.Organization.TryCreateExport((OrganizationExportType)99)).success);
            Assert.False((await client.Organization.TryAddAllowedDomain("", false)).success);
            Assert.False((await client.Organization.TryUpdateAllowedDomain("..", false)).success);
            Assert.False((await client.Organization.TryDeleteAllowedDomain(null!)).success);
            Assert.Empty(requests);
        }

        /// <summary>Role errors are surfaced by every success-only administration mutation without hidden feature probes.</summary>
        /// <param name="operation">The selected mutation.</param>
        [Theory]
        [InlineData("profile-update")]
        [InlineData("profile-delete")]
        [InlineData("profile-order")]
        [InlineData("linkifier-order")]
        [InlineData("domain-update")]
        [InlineData("domain-delete")]
        [InlineData("export-delete")]
        [InlineData("defaults")]
        public async Task Mutations_PermissionDeniedFailsTryAndThrows(string operation)
        {
            const string denied = """{"result":"error","code":"PERMISSION_DENIED","msg":"Must be an organization administrator"}""";
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, denied, denied);

            var result = await TryMutation(client.Organization, operation);
            Exception error = await Assert.ThrowsAsync<Exception>(() => ThrowMutation(client.Organization, operation));

            Assert.False(result.success);
            Assert.Contains("PERMISSION_DENIED", result.details!);
            Assert.Contains("organization administrator", result.details!);
            Assert.Equal(result.details, error.Message);
            Assert.Equal(2, requests.Count);
        }

        /// <summary>Export and domain creation role errors cannot be mistaken for missing-payload success.</summary>
        [Fact]
        public async Task CreationAndLists_PreserveServerErrors()
        {
            const string ownerError = """{"result":"error","code":"PERMISSION_DENIED","msg":"Must be an organization owner"}""";
            const string exportError = """{"result":"error","code":"BAD_REQUEST","msg":"Please request a manual export from zulip-admin@example.com."}""";
            const string denied = """{"result":"error","code":"PERMISSION_DENIED","msg":"Must be an organization administrator"}""";
            List<HttpRequestMessage> requests = [];
            ZulipClient client = Utils.CreateCapturingClient(requests, ownerError, exportError, denied, denied, denied);

            var domain = await client.Organization.TryAddAllowedDomain("example.com", false);
            var export = await client.Organization.TryCreateExport(OrganizationExportType.FullWithoutConsent);
            var domains = await client.Organization.TryGetAllowedDomains();
            var exports = await client.Organization.TryGetExports();
            var consents = await client.Organization.TryGetExportConsents();

            Assert.False(domain.success);
            Assert.Null(domain.domain);
            Assert.Contains("organization owner", domain.details!);
            Assert.False(export.success);
            Assert.Contains("manual export", export.details!);
            Assert.False(domains.success);
            Assert.Null(domains.domains);
            Assert.Contains("PERMISSION_DENIED", domains.details!);
            Assert.False(exports.success);
            Assert.Null(exports.exports);
            Assert.Contains("PERMISSION_DENIED", exports.details!);
            Assert.False(consents.success);
            Assert.Null(consents.consents);
            Assert.Contains("PERMISSION_DENIED", consents.details!);
            Assert.Equal("full_without_consent", (await Utils.ReadForm(requests[1]))["export_type"]);
        }

        /// <summary>Invokes the selected Try mutation for consistent server-error assertions.</summary>
        /// <param name="organization">The tested resource.</param>
        /// <param name="operation">The selected mutation.</param>
        /// <returns>The standard Try mutation result.</returns>
        private static Task<(bool success, string? details)> TryMutation(Organization organization, string operation)
        {
            return operation switch
            {
                "profile-update" => organization.TryUpdateProfileField(4, name: "Phone"),
                "profile-delete" => organization.TryDeleteProfileField(4),
                "profile-order" => organization.TryReorderProfileFields([4]),
                "linkifier-order" => organization.TryReorderLinkifiers([3]),
                "domain-update" => organization.TryUpdateAllowedDomain("example.com", false),
                "domain-delete" => organization.TryDeleteAllowedDomain("example.com"),
                "export-delete" => organization.TryDeleteExport(323),
                "defaults" => organization.TryUpdateUserSettingsDefaults(new RealmUserSettingsDefaultsOptions { EnterSends = false }),
                _ => throw new ArgumentException("Unknown test operation.", nameof(operation))
            };
        }

        /// <summary>Invokes the selected throwing mutation for consistent server-error assertions.</summary>
        /// <param name="organization">The tested resource.</param>
        /// <param name="operation">The selected mutation.</param>
        /// <returns>The mutation task.</returns>
        private static Task ThrowMutation(Organization organization, string operation)
        {
            return operation switch
            {
                "profile-update" => organization.UpdateProfileField(4, name: "Phone"),
                "profile-delete" => organization.DeleteProfileField(4),
                "profile-order" => organization.ReorderProfileFields([4]),
                "linkifier-order" => organization.ReorderLinkifiers([3]),
                "domain-update" => organization.UpdateAllowedDomain("example.com", false),
                "domain-delete" => organization.DeleteAllowedDomain("example.com"),
                "export-delete" => organization.DeleteExport(323),
                "defaults" => organization.UpdateUserSettingsDefaults(new RealmUserSettingsDefaultsOptions { EnterSends = false }),
                _ => throw new ArgumentException("Unknown test operation.", nameof(operation))
            };
        }
    }
}
