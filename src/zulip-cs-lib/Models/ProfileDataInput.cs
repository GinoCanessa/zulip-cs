using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>An update to a custom profile field, whose value is a string, null or an array of user IDs.</summary>
    public sealed class ProfileDataInput
    {
        /// <summary>Whether the value was supplied as a Users-type selection rather than text or removal.</summary>
        private readonly bool _isUsersField;

        /// <summary>Initializes a text, link, date or dropdown field update, or removes its value with null.</summary>
        /// <param name="id">The custom profile field ID.</param>
        /// <param name="value">The field value, or null to remove it.</param>
        public ProfileDataInput(int id, string? value)
        {
            Id = id;
            Value = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>Initializes an update for a Users-type custom profile field.</summary>
        /// <param name="id">The custom profile field ID.</param>
        /// <param name="userIds">The selected user IDs; an empty array clears the selection.</param>
        public ProfileDataInput(int id, int[] userIds)
        {
            Id = id;
            Value = JsonSerializer.SerializeToElement(userIds);
            _isUsersField = true;
        }

        /// <summary>Gets the custom profile field ID.</summary>
        [JsonPropertyName("id")]
        public int Id { get; }

        /// <summary>Gets the typed JSON field value, including an explicit null when removing data.</summary>
        [JsonPropertyName("value")]
        public JsonElement Value { get; }

        /// <summary>Checks identifiers without imposing server-specific field types or lengths.</summary>
        /// <returns>An input error, or null.</returns>
        internal string? Validate()
        {
            if (Id <= 0) return "profile field IDs must be positive integers.";
            if (_isUsersField && Value.ValueKind != JsonValueKind.Array)
                return "profile user IDs must be an array.";
            if (Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement userId in Value.EnumerateArray())
                {
                    if (!userId.TryGetInt32(out int id) || id <= 0)
                        return "profile user IDs must be positive integers.";
                }
            }
            return null;
        }
    }
}
