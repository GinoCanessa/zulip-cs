using System.Text.Json;

namespace zulip_cs_lib
{
    /// <summary>Optional custom profile-field configuration; omit values to retain server defaults.</summary>
    public sealed class ProfileFieldOptions
    {
        /// <summary>Gets or sets structured configuration for dropdown/external-account fields.</summary>
        public JsonElement? FieldData { get; set; }

        /// <summary>Gets or sets summary display (feature level 146; paragraph support at 476).</summary>
        public bool? DisplayInProfileSummary { get; set; }

        /// <summary>Gets or sets whether the field is required (feature level 244).</summary>
        public bool? Required { get; set; }

        /// <summary>Gets or sets whether ordinary users can edit the field (feature level 296).</summary>
        public bool? EditableByUser { get; set; }

        /// <summary>Gets or sets typeahead matching for eligible field types (feature level 455).</summary>
        public bool? UseForUserMatching { get; set; }
    }
}
