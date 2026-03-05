using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>Server settings resource.</summary>
    public class Server
    {
        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Server class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Server(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets server settings.</summary>
        /// <returns>An asynchronous result that yields (success, details, zulipVersion, featureLevel).</returns>
        public async Task<(bool success, string details, string zulipVersion, int? featureLevel)> TryGetSettings()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/server_settings", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.ZulipVersion, response.ZulipFeatureLevel);
            }

            return (false, "Server.GetSettings failed: " + response.GetFailureMessage(), null, null);
        }

        /// <summary>Gets server settings (throwing version).</summary>
        /// <returns>An asynchronous result that yields (zulipVersion, featureLevel).</returns>
        public async Task<(string zulipVersion, int? featureLevel)> GetSettings()
        {
            var result = await TryGetSettings();

            if (!result.success)
            {
                throw new Exception(result.details);
            }

            return (result.zulipVersion, result.featureLevel);
        }
    }
}
