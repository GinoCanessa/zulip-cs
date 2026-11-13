using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>Explicit own/bot API-key lifecycle; no client credential is changed implicitly.</summary>
    public partial class Users
    {
        /// <summary>Regenerates the caller's API key and returns it without changing this client's authentication.</summary>
        /// <remarks>Invalidates the old key and mobile device registrations. Construct a new client with the returned key. Do not log LastResponse.</remarks>
        /// <returns>The new key, or explicit failure. A malformed response does not undo rotation.</returns>
        public Task<(bool success, string? details, string? apiKey)> TryRegenerateOwnApiKey() =>
            RequestApiKey(HttpMethod.Post, "api/v1/users/me/api_key/regenerate", "Users.RegenerateOwnApiKey");

        /// <summary>Regenerates the caller's key, throwing on failure; the current client's credentials remain unchanged.</summary>
        /// <returns>The new key for constructing a new client.</returns>
        public async Task<string> RegenerateOwnApiKey()
        {
            var result = await TryRegenerateOwnApiKey();
            if (!result.success) throw new Exception(result.details);
            return result.apiKey!;
        }

        /// <summary>Gets a bot key (FL463); requires the bot owner or an organization administrator.</summary>
        /// <param name="botId">The bot's user ID.</param>
        /// <returns>The key, or explicit failure.</returns>
        public Task<(bool success, string? details, string? apiKey)> TryGetBotApiKey(int botId) =>
            RequestBotApiKey(botId, false);

        /// <summary>Gets a bot key, throwing on failure (FL463).</summary>
        /// <param name="botId">The bot's user ID.</param>
        /// <returns>The bot's key; never log it or the raw response.</returns>
        public async Task<string> GetBotApiKey(int botId)
        {
            var result = await TryGetBotApiKey(botId);
            if (!result.success) throw new Exception(result.details);
            return result.apiKey!;
        }

        /// <summary>Regenerates a bot key; requires the bot owner or an organization administrator.</summary>
        /// <param name="botId">The bot's user ID.</param>
        /// <returns>The new key, or explicit failure; no client's credentials are changed implicitly.</returns>
        public Task<(bool success, string? details, string? apiKey)> TryRegenerateBotApiKey(int botId) =>
            RequestBotApiKey(botId, true);

        /// <summary>Regenerates a bot key, throwing on failure.</summary>
        /// <param name="botId">The bot's user ID.</param>
        /// <returns>The new bot key.</returns>
        public async Task<string> RegenerateBotApiKey(int botId)
        {
            var result = await TryRegenerateBotApiKey(botId);
            if (!result.success) throw new Exception(result.details);
            return result.apiKey!;
        }

        /// <summary>Validates the target identity and chooses the explicit bot credential endpoint.</summary>
        /// <param name="botId">The bot's user ID.</param>
        /// <param name="regenerate">Whether to rotate the key.</param>
        /// <returns>The credential outcome.</returns>
        private Task<(bool success, string? details, string? apiKey)> RequestBotApiKey(int botId, bool regenerate)
        {
            string operation = regenerate ? "Users.RegenerateBotApiKey" : "Users.GetBotApiKey";
            if (botId <= 0)
                return Task.FromResult<(bool, string?, string?)>((false, operation + " failed: botId must be positive.", null));
            string path = "api/v1/bots/" + botId.ToString(CultureInfo.InvariantCulture) + "/api_key";
            return RequestApiKey(regenerate ? HttpMethod.Post : HttpMethod.Get,
                regenerate ? path + "/regenerate" : path, operation);
        }

        /// <summary>Reads a required credential without implicitly replacing the client's key.</summary>
        /// <param name="method">The endpoint method.</param>
        /// <param name="path">The endpoint path.</param>
        /// <param name="operation">The operation name.</param>
        /// <returns>The explicit key or failure.</returns>
        private async Task<(bool success, string? details, string? apiKey)> RequestApiKey(
            HttpMethod method, string path, string operation)
        {
            ZulipResponse response = await _doZulipRequest(method, path, null);
            if (ResponsePayload.TryRead<string>(response, operation, "api_key", JsonValueKind.String,
                out string? key, out string? details, ZulipClient.ValidateCredentialText))
                return (true, null, key);
            string failure = ZulipClient.RedactCredentialDetails(response, details!);
            if (method == HttpMethod.Post)
                failure += " The key may already have been rotated; this client retains its original credential.";
            return (false, failure, null);
        }
    }
}
