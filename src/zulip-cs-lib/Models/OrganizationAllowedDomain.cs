using System.Text.Json.Serialization;

namespace zulip_cs_lib.Models
{
    /// <summary>An allowed organization email domain returned by the server.</summary>
    public sealed class OrganizationAllowedDomain
    {
        /// <summary>Gets or sets the logical domain name.</summary>
        [JsonPropertyName("domain")]
        public string Domain { get; set; } = string.Empty;

        /// <summary>Gets or sets whether email addresses in subdomains are allowed.</summary>
        [JsonPropertyName("allow_subdomains")]
        public bool AllowSubdomains { get; set; }
    }

    /// <summary>The identity and name returned when an allowed domain is created.</summary>
    public sealed class OrganizationAllowedDomainCreationResult
    {
        /// <summary>Gets the server-assigned domain ID.</summary>
        public int Id { get; }

        /// <summary>Gets the domain name returned by the server.</summary>
        public string Domain { get; }

        /// <summary>Initializes the created domain result.</summary>
        /// <param name="id">The server-assigned ID.</param>
        /// <param name="domain">The returned domain name.</param>
        internal OrganizationAllowedDomainCreationResult(int id, string domain)
        {
            Id = id;
            Domain = domain;
        }
    }
}
