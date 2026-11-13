using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>Explicit credentials returned by JWT bootstrap; handle all response data as sensitive.</summary>
    public sealed class ApiKeyResult
    {
        /// <summary>Initializes a validated credential result.</summary>
        /// <param name="apiKey">The returned API key.</param>
        /// <param name="email">The returned API email address.</param>
        /// <param name="user">The requested optional profile.</param>
        /// <param name="response">The request-local response, containing sensitive raw data.</param>
        internal ApiKeyResult(string apiKey, string email, UserObject? user, ZulipResponse response)
        {
            ApiKey = apiKey;
            Email = email;
            User = user;
            Response = response;
        }

        /// <summary>Gets the API key; use it to construct a new authenticated client, never log it.</summary>
        public string ApiKey { get; }

        /// <summary>Gets the API email address associated with the key.</summary>
        public string Email { get; }

        /// <summary>Gets the user profile when requested.</summary>
        public UserObject? User { get; }

        /// <summary>Gets response-local diagnostics, including sensitive raw credential data; never log the response.</summary>
        public ZulipResponse Response { get; }
    }
}
