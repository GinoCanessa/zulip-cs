using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>Stores string key-value pairs for the authenticated bot account.</summary>
    /// <remarks>Only bot accounts may access these endpoints as of feature level 494. The server enforces account and storage limits.</remarks>
    public sealed class BotStorage
    {
        /// <summary>The ordinary Zulip request delegate.</summary>
        private readonly Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes the bot storage resource.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal BotStorage(Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets stored strings, optionally selecting explicit keys.</summary>
        /// <param name="keys">The selected keys, or null to retrieve all stored data. An empty list is sent explicitly.</param>
        /// <returns>The stored strings or explicit failure details.</returns>
        public async Task<(bool success, string? details, Dictionary<string, string>? storage)> TryGet(
            IReadOnlyList<string>? keys = null)
        {
            if (keys != null && !ValidateKeys(keys))
                return (false, "BotStorage.Get failed: keys must contain only strings.", null);
            Dictionary<string, string>? data = keys == null ? null :
                new Dictionary<string, string> { { "keys", JsonSerializer.Serialize(keys) } };
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/bot_storage", data);
            if (ResponsePayload.TryRead<Dictionary<string, string>>(response, "BotStorage.Get", "storage",
                JsonValueKind.Object, out Dictionary<string, string>? storage, out string? details,
                ValidateStoragePayload))
                return (true, null, storage);
            return (false, details, null);
        }

        /// <summary>Gets stored strings, throwing on failure.</summary>
        /// <param name="keys">The selected keys, or null to retrieve all stored data.</param>
        /// <returns>The server's stored strings.</returns>
        public async Task<Dictionary<string, string>> Get(IReadOnlyList<string>? keys = null)
        {
            (bool success, string? details, Dictionary<string, string>? storage) result = await TryGet(keys);
            if (!result.success) throw new Exception(result.details);
            return result.storage!;
        }

        /// <summary>Adds or replaces explicitly supplied string key-value pairs.</summary>
        /// <param name="storage">The string map to store. Empty maps are allowed; null values are not strings.</param>
        /// <returns>Whether the update succeeded and explicit failure details.</returns>
        public async Task<(bool success, string? details)> TryUpdate(IReadOnlyDictionary<string, string> storage)
        {
            if (storage == null)
                return (false, "BotStorage.Update failed: storage must be an explicit string map.");
            foreach (KeyValuePair<string, string> entry in storage)
            {
                if (entry.Key == null || entry.Value == null)
                    return (false, "BotStorage.Update failed: storage keys and values must be strings.");
            }
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "storage", JsonSerializer.Serialize(storage) }
            };
            return await Mutate(HttpMethod.Put, data, "BotStorage.Update");
        }

        /// <summary>Adds or replaces stored strings, throwing on failure.</summary>
        /// <param name="storage">The explicit string map to store.</param>
        public async Task Update(IReadOnlyDictionary<string, string> storage)
        {
            (bool success, string? details) result = await TryUpdate(storage);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deletes only an explicitly selected list of keys, never implicitly all storage.</summary>
        /// <param name="keys">The keys to delete. An empty list is sent explicitly; null is rejected.</param>
        /// <returns>Whether deletion succeeded and explicit failure details.</returns>
        public async Task<(bool success, string? details)> TryDelete(IReadOnlyList<string> keys)
        {
            if (keys == null || !ValidateKeys(keys))
                return (false, "BotStorage.Delete failed: an explicit list of string keys is required.");
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "keys", JsonSerializer.Serialize(keys) }
            };
            return await Mutate(HttpMethod.Delete, data, "BotStorage.Delete");
        }

        /// <summary>Deletes explicitly selected keys, throwing on failure.</summary>
        /// <param name="keys">The required list of keys to delete.</param>
        public async Task Delete(IReadOnlyList<string> keys)
        {
            (bool success, string? details) result = await TryDelete(keys);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Runs a success-envelope-only storage mutation.</summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="data">The form fields.</param>
        /// <param name="operation">The operation name for failure diagnostics.</param>
        /// <returns>The standard mutation result.</returns>
        private async Task<(bool success, string? details)> Mutate(
            HttpMethod method, Dictionary<string, string> data, string operation)
        {
            ZulipResponse response = await _doZulipRequest(method, "api/v1/bot_storage", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null)
                : (false, operation + " failed: " + response.GetFailureMessage());
        }

        /// <summary>Checks that explicitly supplied keys are strings, retaining empty strings and lists.</summary>
        /// <param name="keys">The supplied keys.</param>
        /// <returns>Whether every key is non-null.</returns>
        private static bool ValidateKeys(IReadOnlyList<string> keys)
        {
            foreach (string key in keys)
            {
                if (key == null) return false;
            }
            return true;
        }

        /// <summary>Validates actual wire values rather than coercing arbitrary JSON to strings.</summary>
        /// <param name="storage">The server's storage map.</param>
        /// <returns>An error for a non-string value, or null.</returns>
        private static string? ValidateStoragePayload(JsonElement storage)
        {
            foreach (JsonProperty item in storage.EnumerateObject())
            {
                if (item.Value.ValueKind != JsonValueKind.String)
                    return "storage values must be strings.";
            }
            return null;
        }
    }
}
