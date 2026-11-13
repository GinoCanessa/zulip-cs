using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib
{
    /// <summary>Retains the exact response envelope while honoring its annotated wire properties.</summary>
    internal sealed class ZulipResponseConverter : JsonConverter<ZulipResponse>
    {
        /// <summary>The annotated response fields, excluding client-side metadata and extension data.</summary>
        private static readonly Dictionary<string, PropertyInfo> _properties = typeof(ZulipResponse)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => property.GetCustomAttribute<JsonPropertyNameAttribute>() != null)
            .ToDictionary(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name);

        /// <summary>Reads an owned response, preserving the envelope even when a typed field is invalid.</summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The response type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The response.</returns>
        public override ZulipResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("The Zulip response must be a JSON object.");
            }

            ZulipResponse response = new ZulipResponse { RawPayload = root };
            foreach (JsonProperty field in root.EnumerateObject())
            {
                PropertyInfo? property = _properties.GetValueOrDefault(field.Name);
                if (property == null && options.PropertyNameCaseInsensitive)
                {
                    property = _properties.FirstOrDefault(pair =>
                        string.Equals(pair.Key, field.Name, StringComparison.OrdinalIgnoreCase)).Value;
                }

                if (property == null)
                {
                    response.ExtensionData ??= [];
                    response.ExtensionData[field.Name] = field.Value.Clone();
                    continue;
                }

                try
                {
                    object? value = field.Value.Deserialize(property.PropertyType, options);
                    property.SetValue(response, value);
                    if (field.Name == "messages")
                    {
                        response.RetainUnobservedMessages(options);
                    }
                }
                catch (JsonException exception)
                {
                    response.CaughtException ??= $"Invalid response payload field '{field.Name}': {exception.Message}";
                }
            }

            return response;
        }

        /// <summary>Writes the current typed properties and unknown fields, not the original snapshot.</summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The response.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, ZulipResponse value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<string, PropertyInfo> field in _properties)
            {
                object? fieldValue = field.Value.GetValue(value);
                if (fieldValue == null &&
                    options.DefaultIgnoreCondition is JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.WhenWritingDefault)
                {
                    continue;
                }

                if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault &&
                    field.Value.PropertyType.IsValueType &&
                    Equals(fieldValue, Activator.CreateInstance(field.Value.PropertyType)))
                {
                    continue;
                }

                writer.WritePropertyName(field.Key);
                JsonSerializer.Serialize(writer, fieldValue, field.Value.PropertyType, options);
            }

            if (value.ExtensionData != null)
            {
                foreach (KeyValuePair<string, JsonElement> field in value.ExtensionData)
                {
                    if (!_properties.ContainsKey(field.Key))
                    {
                        writer.WritePropertyName(field.Key);
                        field.Value.WriteTo(writer);
                    }
                }
            }

            writer.WriteEndObject();
        }
    }
}
