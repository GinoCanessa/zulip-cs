using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>Reads supported event fields by kind and retains all other fields as owned JSON.</summary>
    internal sealed class EventObjectConverter : JsonConverter<EventObject>
    {
        /// <summary>The annotated event fields, including collision-safe message backing.</summary>
        private static readonly Dictionary<string, PropertyInfo> _properties = typeof(EventObject)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => property.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
            .ToDictionary(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name);

        /// <summary>Reads an event without applying unrelated payload schemas to unknown event kinds.</summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The target event type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The event and its owned additional fields.</returns>
        public override EventObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new JsonException("An event must be a JSON object.");
            EventObject value = new EventObject();
            foreach (JsonProperty field in root.EnumerateObject())
            {
                (string name, PropertyInfo? property) = FindProperty(field.Name, options);
                if (name is "id" or "type")
                {
                    property!.SetValue(value, field.Value.Deserialize(property.PropertyType, options));
                }
            }

            foreach (JsonProperty field in root.EnumerateObject())
            {
                (string name, PropertyInfo? property) = FindProperty(field.Name, options);
                if (name is "id" or "type") continue;
                if (property != null && IsTypedField(value.Type, name))
                {
                    property.SetValue(value, field.Value.Deserialize(property.PropertyType, options));
                }
                else
                {
                    value.ExtensionData ??= [];
                    value.ExtensionData[field.Name] = field.Value.Clone();
                }
            }

            return value;
        }

        /// <summary>Writes supported typed fields and additional payloads without duplicate names.</summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The event.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, EventObject value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<string, PropertyInfo> field in _properties)
            {
                if (!IsTypedField(value.Type, field.Key) || value.ExtensionData?.ContainsKey(field.Key) == true) continue;
                object? propertyValue = field.Value.GetValue(value);
                if (propertyValue == null ||
                    (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault &&
                        field.Value.PropertyType.IsValueType &&
                        Equals(propertyValue, Activator.CreateInstance(field.Value.PropertyType)))) continue;
                writer.WritePropertyName(field.Key);
                JsonSerializer.Serialize(writer, propertyValue, field.Value.PropertyType, options);
            }

            if (value.ExtensionData != null)
            {
                foreach (KeyValuePair<string, JsonElement> field in value.ExtensionData)
                {
                    if (field.Key is "id" or "type") throw new JsonException("Extension fields cannot replace event identity.");
                    writer.WritePropertyName(field.Key);
                    field.Value.WriteTo(writer);
                }
            }
            writer.WriteEndObject();
        }

        /// <summary>Finds the canonical annotated property, honoring case-insensitive serializer options.</summary>
        /// <param name="name">The incoming field name.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The canonical name and property, or the unmodeled name and null.</returns>
        private static (string name, PropertyInfo? property) FindProperty(string name, JsonSerializerOptions options)
        {
            if (_properties.TryGetValue(name, out PropertyInfo? property)) return (name, property);
            if (options.PropertyNameCaseInsensitive)
            {
                KeyValuePair<string, PropertyInfo> match = _properties.FirstOrDefault(
                    pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase));
                if (match.Value != null) return (match.Key, match.Value);
            }
            return (name, null);
        }

        /// <summary>Limits typed payload fields to the supported contract for their event kind.</summary>
        /// <param name="type">The event kind.</param>
        /// <param name="field">The canonical field name.</param>
        /// <returns>Whether the typed property describes this field for this kind.</returns>
        private static bool IsTypedField(string? type, string field)
        {
            if (field is "id" or "type") return true;
            return type switch
            {
                "message" => field is "message" or "flags" or "local_message_id",
                "update_message" => field is "user_id" or "rendering_only" or "message_id" or "message_ids" or
                    "flags" or "edit_timestamp" or "stream_id" or "stream_name" or "new_stream_id" or
                    "propagate_mode" or "orig_subject" or "subject" or "orig_content" or "content" or
                    "orig_rendered_content" or "rendered_content" or "is_me_message",
                "delete_message" => field is "message_id" or "message_ids" or "message_type" or "stream_id" or "topic",
                "update_message_flags" => field is "op" or "operation" or "flag" or "messages" or "all",
                "subscription" => field is "op" or "stream_id" or "stream_ids" or "user_ids",
                "reaction" => field is "op" or "message_id" or "user_id",
                _ => false,
            };
        }
    }
}
