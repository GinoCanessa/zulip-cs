using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace zulip_cs_lib
{
    /// <summary>Writes only the cursor extension field while native serialization owns the surrounding object.</summary>
    internal sealed class RegistrationCursorConverter : JsonConverter<Dictionary<string, object>>
    {
        /// <summary>Rejects reads because registration results require a validated event-registration response.</summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The extension-data type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>No extension data.</returns>
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("RegistrationResult must be obtained through event registration.");

        /// <summary>Writes the legacy logical property name with caller naming, omission and number options.</summary>
        /// <param name="writer">The writer inside the native registration object.</param>
        /// <param name="value">The serialization-only cursor field.</param>
        /// <param name="options">The caller's serializer options.</param>
        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        {
            if (options.IgnoreReadOnlyProperties) return;
            foreach (KeyValuePair<string, object> field in value)
            {
                if (field.Value is not long cursor)
                {
                    throw new JsonException("The registration cursor must be an Int64 value.");
                }
                if (cursor == 0 && options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault) continue;
                writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(field.Key) ?? field.Key);
                JsonSerializer.Serialize(writer, cursor, options);
            }
        }
    }
}
