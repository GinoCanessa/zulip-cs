using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>Credential bootstrap without an already authenticated Zulip client.</summary>
    public partial class ZulipClient
    {
        /// <summary>Fetches explicit credentials using JWT authentication (FL160).</summary>
        /// <remarks>Requires JWT enabled on the organization. The injected client must not have a default Authorization header.</remarks>
        /// <param name="site">The HTTP(S) Zulip site.</param>
        /// <param name="token">The JWT containing the target user's email claim.</param>
        /// <param name="includeProfile">Request a profile; null omits the parameter and uses the server default.</param>
        /// <param name="httpClient">The optional caller-owned HTTP client.</param>
        /// <param name="cancellationToken">Cancellation for the request and response.</param>
        /// <returns>The explicit credentials/profile/diagnostics, or failure without echoing the token.</returns>
        public static async Task<(bool success, string? details, ApiKeyResult? credentials)> TryFetchApiKeyJwt(
            string site, string token, bool? includeProfile = null, HttpClient? httpClient = null,
            CancellationToken cancellationToken = default)
        {
            const string operation = "FetchApiKeyJwt";
            if (!Uri.TryCreate(site, UriKind.Absolute, out Uri? uri) ||
                uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
                return (false, operation + " failed: site must be an HTTP(S) URL without embedded credentials.", null);
            if (string.IsNullOrWhiteSpace(token))
                return (false, operation + " failed: a JWT token is required.", null);
            if (httpClient?.DefaultRequestHeaders.Authorization != null)
                return (false, operation + " failed: the bootstrap HTTP client must not have a default Authorization header.", null);
            HttpClient client = httpClient ?? new HttpClient();
            try
            {
                Dictionary<string, string> data = new Dictionary<string, string> { { "token", token } };
                if (includeProfile is bool include)
                    data.Add("include_profile", include ? "true" : "false");
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,
                    new Uri(uri, "api/v1/jwt/fetch_api_key"));
                request.Content = new FormUrlEncodedContent(data);
                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                ZulipResponse result = JsonSerializer.Deserialize<ZulipResponse>(body)
                    ?? throw new JsonException("The Zulip response must be a JSON object.");
                result.HttpResponseCode = (int)response.StatusCode;
                result.HttpResponseBody = body;
                if (!ResponsePayload.TryRead<string>(result, operation, "api_key", JsonValueKind.String,
                    out string? key, out string? details, ValidateCredentialText) ||
                    !ResponsePayload.TryRead<string>(result, operation, "email", JsonValueKind.String,
                        out string? email, out details, ValidateCredentialText))
                    return (false, RedactCredentialDetails(result, details!, token), null);
                UserObject? user = null;
                if (includeProfile == true &&
                    !ResponsePayload.TryRead<UserObject>(result, operation, "user", JsonValueKind.Object,
                        out user, out details, ResponsePayload.ValidateUser))
                    return (false, RedactCredentialDetails(result, details!, token), null);
                return (true, null, new ApiKeyResult(key!, email!, user, result));
            }
            catch (OperationCanceledException)
            {
                return (false, operation + " failed: request was canceled.", null);
            }
            catch (HttpRequestException exception)
            {
                return (false, operation + " failed: " + exception.Message.Replace(token, "[redacted]", StringComparison.Ordinal), null);
            }
            catch (JsonException)
            {
                return (false, operation + " failed: invalid JSON credential response.", null);
            }
            finally
            {
                if (httpClient == null) client.Dispose();
            }
        }

        /// <summary>Fetches JWT credentials, throwing on failure (FL160).</summary>
        /// <param name="site">The HTTP(S) Zulip site.</param>
        /// <param name="token">The JWT containing the target user's email claim.</param>
        /// <param name="includeProfile">Whether to request a profile; null omits the parameter.</param>
        /// <param name="httpClient">The optional caller-owned HTTP client without default authentication.</param>
        /// <param name="cancellationToken">Caller cancellation.</param>
        /// <returns>Credentials for constructing a new authenticated client.</returns>
        public static async Task<ApiKeyResult> FetchApiKeyJwt(
            string site, string token, bool? includeProfile = null, HttpClient? httpClient = null,
            CancellationToken cancellationToken = default)
        {
            var result = await TryFetchApiKeyJwt(site, token, includeProfile, httpClient, cancellationToken);
            if (!result.success) throw new Exception(result.details);
            return result.credentials!;
        }

        /// <summary>Requires nonempty credential identity values without echoing them in errors.</summary>
        /// <param name="value">The returned string.</param>
        /// <returns>A validation error, or null.</returns>
        internal static string? ValidateCredentialText(JsonElement value) =>
            string.IsNullOrWhiteSpace(value.GetString()) ? "required credential value must be nonempty." : null;

        /// <summary>Removes returned credentials and the submitted token from failure details.</summary>
        /// <param name="response">The response whose raw data remains caller-visible and sensitive.</param>
        /// <param name="details">The standard failure details.</param>
        /// <param name="token">The optional submitted JWT.</param>
        /// <returns>The redacted failure text.</returns>
        internal static string RedactCredentialDetails(ZulipResponse response, string details, string? token = null)
        {
            if (!string.IsNullOrEmpty(token)) details = details.Replace(token, "[redacted]", StringComparison.Ordinal);
            if (response.RawPayload.ValueKind == JsonValueKind.Object &&
                response.RawPayload.TryGetProperty("api_key", out JsonElement value) &&
                value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(value.GetString()))
                details = details.Replace(value.GetString()!, "[redacted]", StringComparison.Ordinal);
            return details;
        }
    }
}
