using System;
using System.Text.Json;

namespace zulip_cs_lib
{
    /// <summary>Validates required endpoint payloads inside resources, including injected request delegates.</summary>
    internal static class ResponsePayload
    {
        /// <summary>Checks the API result, parse errors and any known HTTP status without requiring transport flags from injected delegates.</summary>
        /// <param name="response">The completed response.</param>
        /// <returns>Whether the envelope represents a successful request.</returns>
        internal static bool IsSuccessful(ZulipResponse response)
        {
            return response.Result == ZulipResponse.ZulipResultSuccess &&
                string.IsNullOrEmpty(response.CaughtException) &&
                (response.HttpResponseCode == 0 || response.HttpResponseCode is >= 200 and < 300);
        }

        /// <summary>Reads a required field, or the root when field is null, without substituting CLR defaults.</summary>
        /// <remarks>
        /// Success requires result=success and no caught exception; the transport-only Success flag is not required.
        /// The owned wire payload is authoritative. Body-only delegates are reparsed; caller-created responses use explicit property assignments.
        /// Empty collections and numeric zero or negative values are not generically rejected.
        /// </remarks>
        /// <param name="response">The completed response.</param>
        /// <param name="operation">The operation name used in standard failure details.</param>
        /// <param name="field">The required field, or null for the response root.</param>
        /// <param name="kind">The required JSON shape.</param>
        /// <param name="value">The parsed value.</param>
        /// <param name="details">The standard Try failure details.</param>
        /// <param name="validate">Optional endpoint-specific semantic validation.</param>
        /// <returns>Whether the required value is valid.</returns>
        internal static bool TryRead<T>(
            ZulipResponse response, string operation, string? field, JsonValueKind kind,
            out T? value, out string? details, Func<JsonElement, string?>? validate = null)
        {
            value = default;
            details = null;
            if (!IsSuccessful(response))
            {
                details = operation + " failed: " + response.GetFailureMessage();
                return false;
            }

            try
            {
                JsonElement root = response.RawPayload;
                if (root.ValueKind == JsonValueKind.Undefined)
                {
                    root = !string.IsNullOrEmpty(response.HttpResponseBody)
                        ? JsonSerializer.Deserialize<JsonElement>(response.HttpResponseBody)
                        : response.GetAssignedPayload();
                    response.RawPayload = root;
                }

                JsonElement payload = root;
                if (root.ValueKind != JsonValueKind.Object ||
                    (field != null && !root.TryGetProperty(field, out payload)) ||
                    payload.ValueKind != kind)
                {
                    throw new JsonException($"required field '{field ?? "$"}' must be {kind}.");
                }

                string? validationError = validate?.Invoke(payload);
                if (validationError != null)
                {
                    throw new JsonException(validationError);
                }

                value = payload.Deserialize<T>();
                return true;
            }
            catch (JsonException exception)
            {
                details = operation + $" failed: Invalid response payload for '{field ?? "$"}': " + exception.Message;
                return false;
            }
        }

        /// <summary>Requires an integer user ID and string identity fields on a returned user.</summary>
        /// <param name="user">The flat or nested user payload.</param>
        /// <returns>A validation error, or null.</returns>
        internal static string? ValidateUser(JsonElement user)
        {
            if (user.ValueKind != JsonValueKind.Object ||
                !user.TryGetProperty("user_id", out JsonElement id) ||
                id.ValueKind != JsonValueKind.Number || !id.TryGetInt32(out _))
            {
                return "required user field 'user_id' must be an integer.";
            }

            foreach (string field in new string[] { "email", "full_name" })
            {
                if (!user.TryGetProperty(field, out JsonElement value) || value.ValueKind != JsonValueKind.String)
                {
                    return $"required user field '{field}' must be String.";
                }
            }

            return null;
        }

        /// <summary>Validates the identities in a user-list payload.</summary>
        /// <param name="users">The users array.</param>
        /// <returns>A validation error, or null.</returns>
        internal static string? ValidateUsers(JsonElement users)
        {
            foreach (JsonElement user in users.EnumerateArray())
            {
                string? error = ValidateUser(user);
                if (error != null) return error;
            }

            return null;
        }

        /// <summary>Validates collection element shapes without rejecting an empty collection.</summary>
        /// <param name="array">The required array.</param>
        /// <param name="kind">The required element shape.</param>
        /// <returns>A validation error, or null.</returns>
        internal static string? ValidateElements(JsonElement array, JsonValueKind kind)
        {
            foreach (JsonElement element in array.EnumerateArray())
            {
                if (element.ValueKind != kind)
                {
                    return $"required collection elements must be {kind}.";
                }
            }

            return null;
        }

        /// <summary>Validates required integer identities on collection objects while preserving legitimate empty arrays.</summary>
        /// <param name="array">The returned object array.</param>
        /// <param name="field">The required identity field.</param>
        /// <returns>A validation error, or null.</returns>
        internal static string? ValidateObjectIds(JsonElement array, string field)
        {
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty(field, out JsonElement id) ||
                    id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out _))
                {
                    return $"required collection field '{field}' must be an integer.";
                }
            }

            return null;
        }

        /// <summary>Validates dictionary value shapes without rejecting an empty dictionary.</summary>
        /// <param name="dictionary">The required dictionary.</param>
        /// <returns>A validation error, or null.</returns>
        internal static string? ValidateObjectValues(JsonElement dictionary)
        {
            foreach (JsonProperty property in dictionary.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                {
                    return $"required dictionary value '{property.Name}' must be Object.";
                }
            }

            return null;
        }
    }
}
