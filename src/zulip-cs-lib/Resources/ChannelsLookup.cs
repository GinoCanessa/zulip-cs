using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>User-channel subscription lookup workflows.</summary>
    public partial class Channels
    {
        /// <summary>Gets subscribed channel IDs visible to the acting user for a target user.</summary>
        /// <param name="userId">The target user ID.</param>
        /// <remarks>Requires feature level 440; bot targets require 458. Metadata permissions may limit the returned channels; administrators can see all of the user's subscribed channels.</remarks>
        /// <returns>Success and the subscribed channel IDs, including an empty list, or explicit failure details.</returns>
        public async Task<(bool success, string? details, List<int>? subscribedChannelIds)> TryGetUserChannels(int userId)
        {
            if (userId <= 0) return (false, "Channels.GetUserChannels failed: userId must be a positive integer.", null);
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get,
                $"{_usersApiEndpoint}/{userId.ToString(CultureInfo.InvariantCulture)}/channels", null);
            if (ResponsePayload.TryRead<List<int>>(response, "Channels.GetUserChannels",
                "subscribed_channel_ids", JsonValueKind.Array, out List<int>? ids, out string? details))
                return (true, null, ids);
            return (false, details, null);
        }

        /// <summary>Gets a target user's visible subscribed channel IDs, throwing on failure.</summary>
        /// <param name="userId">The target user ID.</param>
        /// <returns>The subscribed channel IDs visible to the acting user.</returns>
        public async Task<List<int>> GetUserChannels(int userId)
        {
            (bool success, string? details, List<int>? subscribedChannelIds) = await TryGetUserChannels(userId);
            if (!success) throw new Exception(details);
            return subscribedChannelIds!;
        }
    }
}
