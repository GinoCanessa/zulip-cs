using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Converts the integer-or-anonymous-object permission setting contract.</summary>
    internal sealed class GroupSettingValueConverter : JsonConverter<GroupSettingValue>
    {
        /// <summary>Creates a converter for use by the JSON converter attribute.</summary>
        public GroupSettingValueConverter()
        {
        }

        /// <summary>Gets whether explicit JSON null values are handled and rejected.</summary>
        public override bool HandleNull => true;

        /// <summary>Reads a named group ID or an anonymous group with both required arrays.</summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The target value type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The permission setting value.</returns>
        /// <exception cref="JsonException">The value does not match either supported variant.</exception>
        public override GroupSettingValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int groupId))
            {
                return new GroupSettingValue(groupId);
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("A group setting must be an integer group ID or an anonymous group object.");
            }

            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            List<int>? directMembers = null;
            List<int>? directSubgroups = null;
            Dictionary<string, JsonElement> additionalProperties = [];
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "direct_members":
                        directMembers = ReadIds(property.Value, property.Name);
                        break;
                    case "direct_subgroups":
                        directSubgroups = ReadIds(property.Value, property.Name);
                        break;
                    default:
                        additionalProperties[property.Name] = property.Value.Clone();
                        break;
                }
            }

            if (directMembers is null || directSubgroups is null)
            {
                throw new JsonException("An anonymous group setting requires direct_members and direct_subgroups arrays.");
            }

            GroupSettingValue value = new(directMembers, directSubgroups);
            foreach (KeyValuePair<string, JsonElement> property in additionalProperties)
            {
                value.AdditionalProperties.Add(property.Key, property.Value);
            }
            return value;
        }

        /// <summary>Writes the same wire variant without losing unknown anonymous-object fields.</summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The permission setting value.</param>
        /// <param name="options">The serializer options.</param>
        /// <exception cref="JsonException">The value is null or contains conflicting extension fields.</exception>
        public override void Write(Utf8JsonWriter writer, GroupSettingValue value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                throw new JsonException("A group setting cannot be null.");
            }
            if (value.AdditionalProperties.ContainsKey("direct_members") ||
                value.AdditionalProperties.ContainsKey("direct_subgroups") ||
                (value.GroupId.HasValue && value.AdditionalProperties.Count != 0))
            {
                throw new JsonException("Extension fields cannot replace group members or be attached to a named group ID.");
            }
            if (value.GroupId is int groupId)
            {
                writer.WriteNumberValue(groupId);
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("direct_members");
            JsonSerializer.Serialize(writer, value.DirectMembers, options);
            writer.WritePropertyName("direct_subgroups");
            JsonSerializer.Serialize(writer, value.DirectSubgroups, options);
            foreach (KeyValuePair<string, JsonElement> property in value.AdditionalProperties)
            {
                writer.WritePropertyName(property.Key);
                property.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        /// <summary>Reads an array containing only integral user or subgroup IDs.</summary>
        /// <param name="element">The array value.</param>
        /// <param name="propertyName">The wire property name used in diagnostics.</param>
        /// <returns>The IDs in their original order.</returns>
        /// <exception cref="JsonException">The property is not an array of integer IDs.</exception>
        private static List<int> ReadIds(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException($"{propertyName} must be an array of integer IDs.");
            }
            List<int> ids = [];
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out int id))
                {
                    throw new JsonException($"{propertyName} must contain only integer IDs.");
                }
                ids.Add(id);
            }
            return ids;
        }
    }
}
