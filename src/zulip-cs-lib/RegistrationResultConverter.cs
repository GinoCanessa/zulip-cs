using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace zulip_cs_lib
{
    /// <summary>Serializes the lossless cursor under the legacy property's policy-aware name.</summary>
    internal sealed class RegistrationResultConverter : JsonConverter<RegistrationResult>
    {
        /// <summary>The public result properties, excluding the checked legacy cursor getter.</summary>
        private static readonly PropertyInfo[] _properties = typeof(RegistrationResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always)
            .ToArray();

        /// <summary>The native object contracts for each caller's immutable serializer options.</summary>
        private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonTypeInfo<RegistrationResult>> _contracts = new();

        /// <summary>Rejects deserialization, which requires a validated registration response.</summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The result type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>No result; direct deserialization is unsupported.</returns>
        /// <exception cref="NotSupportedException">Registration results must come from event registration.</exception>
        public override RegistrationResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("RegistrationResult must be obtained through event registration.");

        /// <summary>Writes the existing result shape without evaluating the narrowing cursor getter.</summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The result.</param>
        /// <param name="options">The caller's naming, omission and nested serialization options.</param>
        public override void Write(Utf8JsonWriter writer, RegistrationResult value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, _contracts.GetValue(options, CreateContract));
        }

        /// <summary>Builds native property metadata so the serializer retains its omission and nested-value behavior.</summary>
        /// <param name="options">The caller's serializer options.</param>
        /// <returns>An object contract that bypasses this converter and aliases only the long cursor.</returns>
        private static JsonTypeInfo<RegistrationResult> CreateContract(JsonSerializerOptions options)
        {
            JsonTypeInfo<RegistrationResult> contract = JsonMetadataServices.CreateObjectInfo<RegistrationResult>(
                options, new JsonObjectInfoValues<RegistrationResult>
                {
                    PropertyMetadataInitializer = _ => [],
                    NumberHandling = options.NumberHandling
                });
            foreach (PropertyInfo property in _properties)
            {
                if (options.IgnoreReadOnlyProperties && property.SetMethod == null &&
                    options.GetTypeInfo(property.PropertyType).Kind is not JsonTypeInfoKind.Enumerable and not JsonTypeInfoKind.Dictionary)
                {
                    continue;
                }

                string logicalName = property.Name == nameof(RegistrationResult.LastEventId64)
                    ? nameof(RegistrationResult.LastEventId) : property.Name;
                string name = options.PropertyNamingPolicy?.ConvertName(logicalName) ?? logicalName;
                JsonPropertyInfo metadata = contract.CreateJsonPropertyInfo(property.PropertyType, name);
                metadata.Get = property.GetValue;
                metadata.AttributeProvider = property;
                contract.Properties.Add(metadata);
            }
            return contract;
        }
    }
}
