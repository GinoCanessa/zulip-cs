using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Users resource.</summary>
    public class Users
    {
        /// <summary>The users API endpoint.</summary>
        private const string _usersApiEndpoint = "api/v1/users";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Users class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Users(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets the current user.</summary>
        /// <remarks>Feature level 433: user objects added <c>is_imported_stub</c> in user payloads.</remarks>
        /// <returns>An asynchronous result that yields (success, details, user).</returns>
        public async Task<(bool success, string details, UserObject user)> TryGetOwnUser()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/me", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.User);
            }

            return (false, "Users.GetOwnUser failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets the current user (throwing version).</summary>
        /// <returns>An asynchronous result that yields the user.</returns>
        public async Task<UserObject> GetOwnUser()
        {
            var result = await TryGetOwnUser();
            if (!result.success) throw new Exception(result.details);
            return result.user;
        }

        /// <summary>Gets a user by ID.</summary>
        /// <param name="userId">The user ID.</param>
        /// <remarks>
        /// Feature level 437: user lookup behaviors were refined for compatibility edge cases.
        /// Feature level 433: user payloads include imported-stub metadata.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details, user).</returns>
        public async Task<(bool success, string details, UserObject user)> TryGetUser(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/{userId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.User);
            }

            return (false, "Users.GetUser failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets a user by ID (throwing version).</summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>An asynchronous result that yields the user.</returns>
        public async Task<UserObject> GetUser(int userId)
        {
            var result = await TryGetUser(userId);
            if (!result.success) throw new Exception(result.details);
            return result.user;
        }

        /// <summary>Gets a user by email.</summary>
        /// <param name="email">The user email.</param>
        /// <returns>An asynchronous result that yields (success, details, user).</returns>
        public async Task<(bool success, string details, UserObject user)> TryGetUserByEmail(string email)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/{email}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.User);
            }

            return (false, "Users.GetUserByEmail failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets a user by email (throwing version).</summary>
        /// <param name="email">The user email.</param>
        /// <returns>An asynchronous result that yields the user.</returns>
        public async Task<UserObject> GetUserByEmail(string email)
        {
            var result = await TryGetUserByEmail(email);
            if (!result.success) throw new Exception(result.details);
            return result.user;
        }

        /// <summary>Gets all users.</summary>
        /// <remarks>Feature level 437: user-list responses received compatibility corrections for specific client scenarios.</remarks>
        /// <returns>An asynchronous result that yields (success, details, members).</returns>
        public async Task<(bool success, string details, List<UserObject> members)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _usersApiEndpoint, null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Members ?? new List<UserObject>());
            }

            return (false, "Users.GetAll failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all users (throwing version).</summary>
        /// <returns>An asynchronous result that yields the members list.</returns>
        public async Task<List<UserObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.members;
        }

        /// <summary>Sets typing status.</summary>
        /// <param name="op">The operation (start or stop).</param>
        /// <param name="userIds">The user IDs.</param>
        /// <param name="type">(Optional) The type of message being composed.</param>
        /// <param name="streamId">(Optional) The stream ID for channel typing notifications.</param>
        /// <param name="topic">(Optional) The topic for channel typing notifications.</param>
        /// <remarks>Feature level 372: passing "(no topic)" is interpreted by the server as an empty topic name.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TrySetTypingStatus(
            string op,
            int[] userIds,
            string type = "direct",
            int? streamId = null,
            string topic = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "op", op },
                { "to", JsonSerializer.Serialize(userIds) },
                { "type", type }
            };

            if (streamId != null)
            {
                data["stream_id"] = streamId.Value.ToString();
            }

            if (topic != null)
            {
                data["topic"] = topic;
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/typing", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.SetTypingStatus failed: " + response.GetFailureMessage());
        }

        /// <summary>Sets typing status (throwing version).</summary>
        /// <param name="op">The operation (start or stop).</param>
        /// <param name="userIds">The user IDs.</param>
        /// <param name="type">(Optional) The type.</param>
        /// <param name="streamId">(Optional) The stream ID.</param>
        /// <param name="topic">(Optional) The topic.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task SetTypingStatus(string op, int[] userIds, string type = "direct", int? streamId = null, string topic = null)
        {
            var result = await TrySetTypingStatus(op, userIds, type, streamId, topic);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Creates a new user.</summary>
        /// <param name="email">The email.</param>
        /// <param name="password">The password.</param>
        /// <param name="fullName">The full name.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryCreate(string email, string password, string fullName)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "email", email },
                { "password", password },
                { "full_name", fullName }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _usersApiEndpoint, data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.Create failed: " + response.GetFailureMessage());
        }

        /// <summary>Creates a new user (throwing version).</summary>
        public async Task Create(string email, string password, string fullName)
        {
            var result = await TryCreate(email, password, fullName);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Updates a user.</summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fullName">(Optional) New full name.</param>
        /// <param name="role">(Optional) New role.</param>
        /// <remarks>Feature level 313: API supports <c>new_email</c> updates; this wrapper currently exposes full name and role updates.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryUpdate(int userId, string fullName = null, int? role = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (fullName != null) data.Add("full_name", fullName);
            if (role != null) data.Add("role", role.Value.ToString());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_usersApiEndpoint}/{userId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.Update failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a user (throwing version).</summary>
        public async Task Update(int userId, string fullName = null, int? role = null)
        {
            var result = await TryUpdate(userId, fullName, role);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deactivates a user.</summary>
        /// <param name="userId">The user ID.</param>
        /// <remarks>Feature level 459: deactivation accepts an <c>actions</c> parameter for extra cleanup workflows.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDeactivate(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_usersApiEndpoint}/{userId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.Deactivate failed: " + response.GetFailureMessage());
        }

        /// <summary>Deactivates a user (throwing version).</summary>
        public async Task Deactivate(int userId)
        {
            var result = await TryDeactivate(userId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Reactivates a user.</summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryReactivate(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_usersApiEndpoint}/{userId}/reactivate", new Dictionary<string, string>());

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.Reactivate failed: " + response.GetFailureMessage());
        }

        /// <summary>Reactivates a user (throwing version).</summary>
        public async Task Reactivate(int userId)
        {
            var result = await TryReactivate(userId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets user status.</summary>
        /// <param name="userId">The user ID.</param>
        /// <remarks>Feature level 262: user-status retrieval endpoint is tracked in API history.</remarks>
        /// <returns>An asynchronous result that yields (success, details, status).</returns>
        public async Task<(bool success, string details, UserStatusObject status)> TryGetStatus(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/{userId}/status", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Status);
            }

            return (false, "Users.GetStatus failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets user status (throwing version).</summary>
        public async Task<UserStatusObject> GetStatus(int userId)
        {
            var result = await TryGetStatus(userId);
            if (!result.success) throw new Exception(result.details);
            return result.status;
        }

        /// <summary>Updates the current user's status.</summary>
        /// <param name="statusText">(Optional) Status text.</param>
        /// <param name="away">(Optional) Away flag.</param>
        /// <param name="emojiName">(Optional) Emoji name.</param>
        /// <param name="emojiCode">(Optional) Emoji code.</param>
        /// <param name="reactionType">(Optional) Reaction type.</param>
        /// <remarks>Feature level 148: own-status update endpoint is tracked in API history.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryUpdateOwnStatus(
            string statusText = null,
            bool? away = null,
            string emojiName = null,
            string emojiCode = null,
            string reactionType = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (statusText != null) data.Add("status_text", statusText);
            if (away != null) data.Add("away", away.Value.ToString().ToLowerInvariant());
            if (emojiName != null) data.Add("emoji_name", emojiName);
            if (emojiCode != null) data.Add("emoji_code", emojiCode);
            if (reactionType != null) data.Add("reaction_type", reactionType);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_usersApiEndpoint}/me/status", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.UpdateOwnStatus failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates the current user's status (throwing version).</summary>
        public async Task UpdateOwnStatus(string statusText = null, bool? away = null, string emojiName = null, string emojiCode = null, string reactionType = null)
        {
            var result = await TryUpdateOwnStatus(statusText, away, emojiName, emojiCode, reactionType);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets presence info for a user.</summary>
        /// <param name="idOrEmail">The user ID or email.</param>
        /// <remarks>Feature level 178: user and realm presence APIs were introduced and expanded in this period.</remarks>
        /// <returns>An asynchronous result that yields (success, details, presence).</returns>
        public async Task<(bool success, string details, Dictionary<string, PresenceInfo> presence)> TryGetPresence(string idOrEmail)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/{idOrEmail}/presence", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Presence ?? new Dictionary<string, PresenceInfo>());
            }

            return (false, "Users.GetPresence failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets presence info (throwing version).</summary>
        public async Task<Dictionary<string, PresenceInfo>> GetPresence(string idOrEmail)
        {
            var result = await TryGetPresence(idOrEmail);
            if (!result.success) throw new Exception(result.details);
            return result.presence;
        }

        /// <summary>Mutes a user.</summary>
        /// <param name="userId">The user ID to mute.</param>
        /// <remarks>Feature level 188: muted-users endpoints were introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryMuteUser(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_usersApiEndpoint}/me/muted_users/{userId}", new Dictionary<string, string>());

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.MuteUser failed: " + response.GetFailureMessage());
        }

        /// <summary>Mutes a user (throwing version).</summary>
        public async Task MuteUser(int userId)
        {
            var result = await TryMuteUser(userId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Unmutes a user.</summary>
        /// <param name="userId">The user ID to unmute.</param>
        /// <remarks>Feature level 188: muted-users endpoints were introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryUnmuteUser(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_usersApiEndpoint}/me/muted_users/{userId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.UnmuteUser failed: " + response.GetFailureMessage());
        }

        /// <summary>Unmutes a user (throwing version).</summary>
        public async Task UnmuteUser(int userId)
        {
            var result = await TryUnmuteUser(userId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets alert words.</summary>
        /// <returns>An asynchronous result that yields (success, details, alertWords).</returns>
        public async Task<(bool success, string details, List<string> alertWords)> TryGetAlertWords()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/me/alert_words", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.AlertWords ?? new List<string>());
            }

            return (false, "Users.GetAlertWords failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets alert words (throwing version).</summary>
        public async Task<List<string>> GetAlertWords()
        {
            var result = await TryGetAlertWords();
            if (!result.success) throw new Exception(result.details);
            return result.alertWords;
        }

        /// <summary>Adds alert words.</summary>
        /// <param name="words">The words to add.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryAddAlertWords(string[] words)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "alert_words", JsonSerializer.Serialize(words) }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_usersApiEndpoint}/me/alert_words", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.AddAlertWords failed: " + response.GetFailureMessage());
        }

        /// <summary>Adds alert words (throwing version).</summary>
        public async Task AddAlertWords(string[] words)
        {
            var result = await TryAddAlertWords(words);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets user groups.</summary>
        /// <remarks>Feature level 324: user-group objects gained additional governance fields such as <c>can_remove_members_group</c>.</remarks>
        /// <returns>An asynchronous result that yields (success, details, groups).</returns>
        public async Task<(bool success, string details, List<UserGroupObject> groups)> TryGetGroups()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/user_groups", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.UserGroups ?? new List<UserGroupObject>());
            }

            return (false, "Users.GetGroups failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets user groups (throwing version).</summary>
        public async Task<List<UserGroupObject>> GetGroups()
        {
            var result = await TryGetGroups();
            if (!result.success) throw new Exception(result.details);
            return result.groups;
        }

        /// <summary>Creates a user group.</summary>
        /// <param name="name">The group name.</param>
        /// <param name="description">The group description.</param>
        /// <param name="members">The member user IDs.</param>
        /// <remarks>Feature level 324: creation supports newer group-permission fields in Zulip API payloads.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryCreateGroup(string name, string description, int[] members)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "name", name },
                { "description", description },
                { "members", JsonSerializer.Serialize(members) }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/user_groups/create", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Users.CreateGroup failed: " + response.GetFailureMessage());
        }

        /// <summary>Creates a user group (throwing version).</summary>
        public async Task CreateGroup(string name, string description, int[] members)
        {
            var result = await TryCreateGroup(name, description, members);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets group members.</summary>
        /// <param name="groupId">The group ID.</param>
        /// <remarks>Feature level 303: group-members listing endpoint is tracked in changelog history.</remarks>
        /// <returns>An asynchronous result that yields (success, details, members).</returns>
        public async Task<(bool success, string details, List<int> members)> TryGetGroupMembers(int groupId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"api/v1/user_groups/{groupId}/members", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.UserIds ?? new List<int>());
            }

            return (false, "Users.GetGroupMembers failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets group members (throwing version).</summary>
        public async Task<List<int>> GetGroupMembers(int groupId)
        {
            var result = await TryGetGroupMembers(groupId);
            if (!result.success) throw new Exception(result.details);
            return result.members;
        }
    }
}
