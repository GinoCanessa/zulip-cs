using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Users resource.</summary>
    public partial class Users
    {
        /// <summary>The users API endpoint.</summary>
        private const string _usersApiEndpoint = "api/v1/users";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Users class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Users(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets the current user.</summary>
        /// <remarks>Feature level 433: user objects added <c>is_imported_stub</c> in user payloads.</remarks>
        /// <returns>An asynchronous result that yields (success, details, user).</returns>
        public async Task<(bool success, string? details, UserObject? user)> TryGetOwnUser()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/me", null);

            if (ResponsePayload.TryRead<UserObject>(response, "Users.GetOwnUser", null, JsonValueKind.Object,
                out UserObject? user, out string? details, ResponsePayload.ValidateUser))
            {
                return (true, null, user);
            }

            return (false, details, null);
        }

        /// <summary>Gets the current user (throwing version).</summary>
        /// <returns>An asynchronous result that yields the user.</returns>
        public async Task<UserObject> GetOwnUser()
        {
            var result = await TryGetOwnUser();
            if (!result.success) throw new Exception(result.details);
            return result.user!;
        }

        /// <summary>Gets a user by ID.</summary>
        /// <param name="userId">The user ID.</param>
        /// <remarks>
        /// Feature level 437: user lookup behaviors were refined for compatibility edge cases.
        /// Feature level 433: user payloads include imported-stub metadata.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details, user).</returns>
        public async Task<(bool success, string? details, UserObject? user)> TryGetUser(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/{userId}", null);

            if (ResponsePayload.TryRead<UserObject>(response, "Users.GetUser", "user", JsonValueKind.Object,
                out UserObject? user, out string? details, ResponsePayload.ValidateUser))
            {
                return (true, null, user);
            }

            return (false, details, null);
        }

        /// <summary>Gets a user by ID (throwing version).</summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>An asynchronous result that yields the user.</returns>
        public async Task<UserObject> GetUser(int userId)
        {
            var result = await TryGetUser(userId);
            if (!result.success) throw new Exception(result.details);
            return result.user!;
        }

        /// <summary>Gets a user by email.</summary>
        /// <param name="email">The user email.</param>
        /// <returns>An asynchronous result that yields (success, details, user).</returns>
        public async Task<(bool success, string? details, UserObject? user)> TryGetUserByEmail(string email)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/{email}", null);

            if (ResponsePayload.TryRead<UserObject>(response, "Users.GetUserByEmail", "user", JsonValueKind.Object,
                out UserObject? user, out string? details, ResponsePayload.ValidateUser))
            {
                return (true, null, user);
            }

            return (false, details, null);
        }

        /// <summary>Gets a user by email (throwing version).</summary>
        /// <param name="email">The user email.</param>
        /// <returns>An asynchronous result that yields the user.</returns>
        public async Task<UserObject> GetUserByEmail(string email)
        {
            var result = await TryGetUserByEmail(email);
            if (!result.success) throw new Exception(result.details);
            return result.user!;
        }

        /// <summary>Gets all users.</summary>
        /// <remarks>Feature level 437: user-list responses received compatibility corrections for specific client scenarios.</remarks>
        /// <returns>An asynchronous result that yields (success, details, members).</returns>
        public async Task<(bool success, string? details, List<UserObject>? members)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _usersApiEndpoint, null);

            if (ResponsePayload.TryRead<List<UserObject>>(response, "Users.GetAll", "members", JsonValueKind.Array,
                out List<UserObject>? members, out string? details, ResponsePayload.ValidateUsers))
            {
                return (true, null, members);
            }

            return (false, details, null);
        }

        /// <summary>Gets all users (throwing version).</summary>
        /// <returns>An asynchronous result that yields the members list.</returns>
        public async Task<List<UserObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.members!;
        }

        /// <summary>Sets typing status.</summary>
        /// <param name="op">The operation (start or stop).</param>
        /// <param name="userIds">The user IDs.</param>
        /// <param name="type">(Optional) The type of message being composed.</param>
        /// <param name="streamId">(Optional) The stream ID for channel typing notifications.</param>
        /// <param name="topic">(Optional) The topic for channel typing notifications.</param>
        /// <remarks>Feature level 372: passing "(no topic)" is interpreted by the server as an empty topic name.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TrySetTypingStatus(
            string op,
            int[] userIds,
            string type = "direct",
            int? streamId = null,
            string? topic = null)
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

            if (ResponsePayload.IsSuccessful(response))
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
        public async Task SetTypingStatus(string op, int[] userIds, string type = "direct", int? streamId = null, string? topic = null)
        {
            var result = await TrySetTypingStatus(op, userIds, type, streamId, topic);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Creates a new user.</summary>
        /// <param name="email">The email.</param>
        /// <param name="password">The password.</param>
        /// <param name="fullName">The full name.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryCreate(string email, string password, string fullName)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "email", email },
                { "password", password },
                { "full_name", fullName }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, _usersApiEndpoint, data);

            if (ResponsePayload.IsSuccessful(response))
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
        public async Task<(bool success, string? details)> TryUpdate(int userId, string? fullName = null, int? role = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (fullName != null) data.Add("full_name", fullName);
            if (role != null) data.Add("role", role.Value.ToString());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_usersApiEndpoint}/{userId}", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Users.Update failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a user (throwing version).</summary>
        public async Task Update(int userId, string? fullName = null, int? role = null)
        {
            var result = await TryUpdate(userId, fullName, role);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Deactivates a user.</summary>
        /// <param name="userId">The user ID.</param>
        /// <remarks>Feature level 459: deactivation accepts an <c>actions</c> parameter for extra cleanup workflows.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryDeactivate(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_usersApiEndpoint}/{userId}", null);

            if (ResponsePayload.IsSuccessful(response))
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
        public async Task<(bool success, string? details)> TryReactivate(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_usersApiEndpoint}/{userId}/reactivate", new Dictionary<string, string>());

            if (ResponsePayload.IsSuccessful(response))
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
        public async Task<(bool success, string? details, UserStatusObject? status)> TryGetStatus(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/{userId}/status", null);

            if (ResponsePayload.TryRead<UserStatusObject>(response, "Users.GetStatus", "status", JsonValueKind.Object,
                out UserStatusObject? status, out string? details))
            {
                return (true, null, status);
            }

            return (false, details, null);
        }

        /// <summary>Gets user status (throwing version).</summary>
        public async Task<UserStatusObject> GetStatus(int userId)
        {
            var result = await TryGetStatus(userId);
            if (!result.success) throw new Exception(result.details);
            return result.status!;
        }

        /// <summary>Updates the current user's status.</summary>
        /// <param name="statusText">(Optional) Status text.</param>
        /// <param name="away">(Optional) Away flag.</param>
        /// <param name="emojiName">(Optional) Emoji name.</param>
        /// <param name="emojiCode">(Optional) Emoji code.</param>
        /// <param name="reactionType">(Optional) Reaction type.</param>
        /// <remarks>Feature level 148: own-status update endpoint is tracked in API history.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryUpdateOwnStatus(
            string? statusText = null,
            bool? away = null,
            string? emojiName = null,
            string? emojiCode = null,
            string? reactionType = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (statusText != null) data.Add("status_text", statusText);
            if (away != null) data.Add("away", away.Value.ToString().ToLowerInvariant());
            if (emojiName != null) data.Add("emoji_name", emojiName);
            if (emojiCode != null) data.Add("emoji_code", emojiCode);
            if (reactionType != null) data.Add("reaction_type", reactionType);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_usersApiEndpoint}/me/status", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Users.UpdateOwnStatus failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates the current user's status (throwing version).</summary>
        public async Task UpdateOwnStatus(string? statusText = null, bool? away = null, string? emojiName = null, string? emojiCode = null, string? reactionType = null)
        {
            var result = await TryUpdateOwnStatus(statusText, away, emojiName, emojiCode, reactionType);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets legacy presence entries for a user.</summary>
        /// <param name="idOrEmail">The user ID or email.</param>
        /// <remarks>
        /// This adapter returns only server-supplied legacy entries. Modern-only responses fail with guidance to
        /// GetPresenceDetails; modern timestamps, the server clock and warnings are available through that API.
        /// No active, idle or offline status is inferred from timestamps.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details, presence).</returns>
        public async Task<(bool success, string? details, Dictionary<string, PresenceInfo>? presence)> TryGetPresence(string idOrEmail)
        {
            (bool success, string? details, PresenceResult? presence) = await TryGetPresenceDetails(idOrEmail);
            if (!success)
            {
                return (false, details?.Replace("Users.GetPresenceDetails failed:", "Users.GetPresence failed:"), null);
            }

            Dictionary<string, PresenceInfo>? legacy = presence!.GetLegacyPresence();
            if (legacy == null)
            {
                return (false, "Users.GetPresence failed: Modern-only presence cannot be represented by the legacy dictionary; use GetPresenceDetails for timestamps.", null);
            }

            return (true, null, legacy);
        }

        /// <summary>Gets server-supplied legacy presence entries, failing on modern-only responses.</summary>
        /// <param name="idOrEmail">The user ID or email.</param>
        public async Task<Dictionary<string, PresenceInfo>> GetPresence(string idOrEmail)
        {
            var result = await TryGetPresence(idOrEmail);
            if (!result.success) throw new Exception(result.details);
            return result.presence!;
        }

        /// <summary>Gets modern presence timestamps, optional legacy entries and request-local diagnostics.</summary>
        /// <param name="idOrEmail">The user ID or email.</param>
        /// <returns>The Try result with detailed presence, or standard failure details.</returns>
        public async Task<(bool success, string? details, PresenceResult? presence)> TryGetPresenceDetails(string idOrEmail)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/{idOrEmail}/presence", null);
            if (ResponsePayload.TryRead<UserPresencePayload>(response, "Users.GetPresenceDetails", null, JsonValueKind.Object,
                out UserPresencePayload? payload, out string? details, UserPresencePayload.Validate))
            {
                return (true, null, new PresenceResult(payload!.Presence!, payload.ServerTimestamp, response));
            }

            return (false, details, null);
        }

        /// <summary>Gets detailed presence, throwing on invalid or failed responses.</summary>
        /// <param name="idOrEmail">The user ID or email.</param>
        /// <returns>Modern timestamps, optional legacy entries, server clock and diagnostics.</returns>
        public async Task<PresenceResult> GetPresenceDetails(string idOrEmail)
        {
            (bool success, string? details, PresenceResult? presence) = await TryGetPresenceDetails(idOrEmail);
            if (!success) throw new Exception(details);
            return presence!;
        }

        /// <summary>Mutes a user.</summary>
        /// <param name="userId">The user ID to mute.</param>
        /// <remarks>Feature level 188: muted-users endpoints were introduced.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryMuteUser(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_usersApiEndpoint}/me/muted_users/{userId}", new Dictionary<string, string>());

            if (ResponsePayload.IsSuccessful(response))
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
        public async Task<(bool success, string? details)> TryUnmuteUser(int userId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_usersApiEndpoint}/me/muted_users/{userId}", null);

            if (ResponsePayload.IsSuccessful(response))
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
        public async Task<(bool success, string? details, List<string>? alertWords)> TryGetAlertWords()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/me/alert_words", null);

            if (ResponsePayload.TryRead<List<string>>(response, "Users.GetAlertWords", "alert_words", JsonValueKind.Array,
                out List<string>? alertWords, out string? details,
                words => ResponsePayload.ValidateElements(words, JsonValueKind.String)))
            {
                return (true, null, alertWords);
            }

            return (false, details, null);
        }

        /// <summary>Gets alert words (throwing version).</summary>
        public async Task<List<string>> GetAlertWords()
        {
            var result = await TryGetAlertWords();
            if (!result.success) throw new Exception(result.details);
            return result.alertWords!;
        }

        /// <summary>Adds alert words.</summary>
        /// <param name="words">The words to add.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryAddAlertWords(string[] words)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "alert_words", JsonSerializer.Serialize(words) }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_usersApiEndpoint}/me/alert_words", data);

            if (ResponsePayload.IsSuccessful(response))
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
        public async Task<(bool success, string? details, List<UserGroupObject>? groups)> TryGetGroups()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/user_groups", null);

            if (ResponsePayload.TryRead<List<UserGroupObject>>(response, "Users.GetGroups", "user_groups", JsonValueKind.Array,
                out List<UserGroupObject>? groups, out string? details,
                groups => ResponsePayload.ValidateObjectIds(groups, "id")))
            {
                return (true, null, groups);
            }

            return (false, details, null);
        }

        /// <summary>Gets user groups (throwing version).</summary>
        public async Task<List<UserGroupObject>> GetGroups()
        {
            var result = await TryGetGroups();
            if (!result.success) throw new Exception(result.details);
            return result.groups!;
        }

        /// <summary>Creates a user group, validating its returned ID before projecting to the legacy result.</summary>
        /// <param name="name">The group name.</param>
        /// <param name="description">The group description.</param>
        /// <param name="members">The member user IDs.</param>
        /// <remarks>Feature level 324: creation supports newer group-permission fields in Zulip API payloads.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryCreateGroup(string name, string description, int[] members)
        {
            (bool success, string? details, int groupId) = await TryCreateGroupWithId(name, description, members);
            return (success, details);
        }

        /// <summary>Creates a user group and validates its required returned identity.</summary>
        /// <param name="name">The group name.</param>
        /// <param name="description">The group description; an empty description is allowed.</param>
        /// <param name="members">Initial member user IDs; an empty array is allowed.</param>
        /// <param name="options">Optional subgroups and governance settings.</param>
        /// <remarks>Subgroups require feature level 311; all represented governance settings are supported by 324. Bot callers require 496.</remarks>
        /// <returns>The Try outcome and created group ID.</returns>
        public async Task<(bool success, string? details, int groupId)> TryCreateGroupWithId(
            string name, string description, int[] members, GroupOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(name) || description == null ||
                members == null || !GroupOptions.ValidIds(members))
            {
                return (false, "Users.CreateGroup failed: name, description and an array of positive member IDs are required.", 0);
            }
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "name", name },
                { "description", description },
                { "members", JsonSerializer.Serialize(members) }
            };
            string? inputError = options?.AddTo(data);
            if (inputError != null) return (false, "Users.CreateGroup failed: " + inputError, 0);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/user_groups/create", data);
            if (ResponsePayload.TryRead<int>(response, "Users.CreateGroup", "group_id", JsonValueKind.Number,
                out int groupId, out string? details))
            {
                return (true, null, groupId);
            }
            return (false, details, 0);
        }

        /// <summary>Creates a user group (throwing version).</summary>
        /// <param name="name">The group name.</param>
        /// <param name="description">The group description.</param>
        /// <param name="members">Initial member user IDs.</param>
        public async Task CreateGroup(string name, string description, int[] members)
        {
            await CreateGroupWithId(name, description, members);
        }

        /// <summary>Creates a user group and returns its ID, throwing on request or payload failure.</summary>
        /// <param name="name">The group name.</param>
        /// <param name="description">The group description.</param>
        /// <param name="members">Initial member user IDs.</param>
        /// <param name="options">Optional subgroups and governance settings.</param>
        /// <returns>The created group ID.</returns>
        public async Task<int> CreateGroupWithId(string name, string description, int[] members, GroupOptions? options = null)
        {
            (bool success, string? details, int groupId) = await TryCreateGroupWithId(name, description, members, options);
            if (!success) throw new Exception(details);
            return groupId;
        }

        /// <summary>Updates a group's name, description, governance settings or reactivates it.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="name">An optional new nonempty name.</param>
        /// <param name="description">An optional description, including an empty string.</param>
        /// <param name="options">Optional governance updates and reactivation.</param>
        /// <remarks>Permission updates use new/old values without automatic retries. Optional name/description require feature level 165; represented governance settings require up to 324 and reactivation 386.</remarks>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryUpdateGroup(
            int groupId, string? name = null, string? description = null, GroupUpdateOptions? options = null)
        {
            if (groupId <= 0 || (name != null && string.IsNullOrWhiteSpace(name)))
            {
                return (false, "Users.UpdateGroup failed: groupId must be positive and a supplied name must be nonempty.");
            }
            Dictionary<string, string> data = [];
            if (name != null) data["name"] = name;
            if (description != null) data["description"] = description;
            string? inputError = options?.AddTo(data);
            if (inputError != null) return (false, "Users.UpdateGroup failed: " + inputError);
            if (data.Count == 0) return (false, "Users.UpdateGroup failed: at least one change is required.");

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"api/v1/user_groups/{groupId}", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null) : (false, "Users.UpdateGroup failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a group, throwing on failure.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="name">An optional new name.</param>
        /// <param name="description">An optional description.</param>
        /// <param name="options">Optional governance updates and reactivation.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task UpdateGroup(int groupId, string? name = null, string? description = null, GroupUpdateOptions? options = null)
        {
            (bool success, string? details) = await TryUpdateGroup(groupId, name, description, options);
            if (!success) throw new Exception(details);
        }

        /// <summary>Adds or removes direct group members and optionally subgroups in one request.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="add">Optional user IDs to add.</param>
        /// <param name="delete">Optional user IDs to remove.</param>
        /// <param name="addSubgroups">Optional subgroup IDs to add (feature level 311).</param>
        /// <param name="deleteSubgroups">Optional subgroup IDs to remove (feature level 311).</param>
        /// <remarks>Explicit empty arrays are retained; at least one array is required. Editing deactivated groups requires feature level 391.</remarks>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryUpdateGroupMembers(
            int groupId, int[]? add = null, int[]? delete = null,
            int[]? addSubgroups = null, int[]? deleteSubgroups = null)
        {
            Dictionary<string, string> data = [];
            foreach ((string name, int[]? ids) in new (string, int[]?)[]
            {
                ("add", add), ("delete", delete),
                ("add_subgroups", addSubgroups), ("delete_subgroups", deleteSubgroups)
            })
            {
                if (ids == null) continue;
                if (!GroupOptions.ValidIds(ids)) return (false, "Users.UpdateGroupMembers failed: " + name + " must contain positive integer IDs.");
                data[name] = JsonSerializer.Serialize(ids);
            }
            if (groupId <= 0 || data.Count == 0)
            {
                return (false, "Users.UpdateGroupMembers failed: a positive groupId and at least one member or subgroup array are required.");
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"api/v1/user_groups/{groupId}/members", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null) : (false, "Users.UpdateGroupMembers failed: " + response.GetFailureMessage());
        }

        /// <summary>Adds or removes group members and optional subgroups, throwing on failure.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="add">Optional user IDs to add.</param>
        /// <param name="delete">Optional user IDs to remove.</param>
        /// <param name="addSubgroups">Optional subgroup IDs to add.</param>
        /// <param name="deleteSubgroups">Optional subgroup IDs to remove.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task UpdateGroupMembers(int groupId, int[]? add = null, int[]? delete = null,
            int[]? addSubgroups = null, int[]? deleteSubgroups = null)
        {
            (bool success, string? details) = await TryUpdateGroupMembers(groupId, add, delete, addSubgroups, deleteSubgroups);
            if (!success) throw new Exception(details);
        }

        /// <summary>Checks whether a user belongs to a group, optionally excluding inherited membership.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="userId">The target user ID.</param>
        /// <param name="directMemberOnly">Optional explicit direct-member filtering; null retains the server default.</param>
        /// <remarks>Requires feature level 127; querying bot users requires 458 and bot callers require 496.</remarks>
        /// <returns>The Try outcome and actual membership status, including false.</returns>
        public async Task<(bool success, string? details, bool isMember)> TryGetGroupMembershipStatus(
            int groupId, int userId, bool? directMemberOnly = null)
        {
            if (groupId <= 0 || userId <= 0)
            {
                return (false, "Users.GetGroupMembershipStatus failed: groupId and userId must be positive integers.", false);
            }
            Dictionary<string, string>? data = directMemberOnly.HasValue
                ? new Dictionary<string, string> { { "direct_member_only", directMemberOnly.Value ? "true" : "false" } }
                : null;
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"api/v1/user_groups/{groupId}/members/{userId}", data);
            if (ResponsePayload.TryRead<JsonElement>(response, "Users.GetGroupMembershipStatus",
                null, JsonValueKind.Object, out JsonElement payload, out string? details, root =>
                    root.TryGetProperty("is_user_group_member", out JsonElement value) &&
                    value.ValueKind is JsonValueKind.True or JsonValueKind.False
                        ? null : "required field 'is_user_group_member' must be Boolean."))
            {
                return (true, null, payload.GetProperty("is_user_group_member").GetBoolean());
            }
            return (false, details, false);
        }

        /// <summary>Checks group membership status, throwing on request or payload failure.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="userId">The target user ID.</param>
        /// <param name="directMemberOnly">Optional explicit direct-member filtering.</param>
        /// <returns>Whether the user belongs to the group.</returns>
        public async Task<bool> GetGroupMembershipStatus(int groupId, int userId, bool? directMemberOnly = null)
        {
            (bool success, string? details, bool isMember) = await TryGetGroupMembershipStatus(groupId, userId, directMemberOnly);
            if (!success) throw new Exception(details);
            return isMember;
        }

        /// <summary>Lists direct or recursively included subgroups of a user group.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="directSubgroupOnly">Optional explicit direct-subgroup filtering; null retains the server default.</param>
        /// <remarks>Requires feature level 127; bot callers require 496.</remarks>
        /// <returns>The Try outcome and actual integer subgroup IDs, including an empty list.</returns>
        public async Task<(bool success, string? details, List<int>? subgroups)> TryGetGroupSubgroups(
            int groupId, bool? directSubgroupOnly = null)
        {
            if (groupId <= 0) return (false, "Users.GetGroupSubgroups failed: groupId must be positive.", null);
            Dictionary<string, string>? data = directSubgroupOnly.HasValue
                ? new Dictionary<string, string> { { "direct_subgroup_only", directSubgroupOnly.Value ? "true" : "false" } }
                : null;
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"api/v1/user_groups/{groupId}/subgroups", data);
            if (ResponsePayload.TryRead<List<int>>(response, "Users.GetGroupSubgroups", "subgroups", JsonValueKind.Array,
                out List<int>? subgroups, out string? details))
            {
                return (true, null, subgroups);
            }
            return (false, details, null);
        }

        /// <summary>Lists subgroups, throwing on request or payload failure.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="directSubgroupOnly">Optional explicit direct-subgroup filtering.</param>
        /// <returns>The integer subgroup IDs.</returns>
        public async Task<List<int>> GetGroupSubgroups(int groupId, bool? directSubgroupOnly = null)
        {
            (bool success, string? details, List<int>? subgroups) = await TryGetGroupSubgroups(groupId, directSubgroupOnly);
            if (!success) throw new Exception(details);
            return subgroups!;
        }

        /// <summary>Adds or removes direct subgroups using the dedicated subgroup endpoint.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="add">Optional subgroup IDs to add.</param>
        /// <param name="delete">Optional subgroup IDs to remove.</param>
        /// <remarks>Requires feature level 127. Explicit empty arrays are retained. Editing deactivated groups requires 391.</remarks>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryUpdateGroupSubgroups(int groupId, int[]? add = null, int[]? delete = null)
        {
            if (groupId <= 0 || (add == null && delete == null) ||
                (add != null && !GroupOptions.ValidIds(add)) || (delete != null && !GroupOptions.ValidIds(delete)))
            {
                return (false, "Users.UpdateGroupSubgroups failed: a positive groupId and at least one array of positive subgroup IDs are required.");
            }
            Dictionary<string, string> data = [];
            if (add != null) data["add"] = JsonSerializer.Serialize(add);
            if (delete != null) data["delete"] = JsonSerializer.Serialize(delete);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"api/v1/user_groups/{groupId}/subgroups", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null) : (false, "Users.UpdateGroupSubgroups failed: " + response.GetFailureMessage());
        }

        /// <summary>Adds or removes direct subgroups, throwing on failure.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <param name="add">Optional subgroup IDs to add.</param>
        /// <param name="delete">Optional subgroup IDs to remove.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task UpdateGroupSubgroups(int groupId, int[]? add = null, int[]? delete = null)
        {
            (bool success, string? details) = await TryUpdateGroupSubgroups(groupId, add, delete);
            if (!success) throw new Exception(details);
        }

        /// <summary>Deactivates a group without deleting its audit history.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <remarks>Requires feature level 290. The server can reject deactivation when the group is still in use.</remarks>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryDeactivateGroup(int groupId)
        {
            if (groupId <= 0) return (false, "Users.DeactivateGroup failed: groupId must be positive.");
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"api/v1/user_groups/{groupId}/deactivate", null);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null) : (false, "Users.DeactivateGroup failed: " + response.GetFailureMessage());
        }

        /// <summary>Deactivates a group, throwing on failure.</summary>
        /// <param name="groupId">The target group ID.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task DeactivateGroup(int groupId)
        {
            (bool success, string? details) = await TryDeactivateGroup(groupId);
            if (!success) throw new Exception(details);
        }

        /// <summary>Gets group members.</summary>
        /// <param name="groupId">The group ID.</param>
        /// <remarks>Feature level 303: group-members listing endpoint is tracked in changelog history.</remarks>
        /// <returns>An asynchronous result that yields (success, details, members).</returns>
        public Task<(bool success, string? details, List<int>? members)> TryGetGroupMembers(int groupId)
        {
            return TryGetGroupMembersCore(groupId, null);
        }

        /// <summary>Gets group members with explicit direct-member filtering.</summary>
        /// <param name="groupId">The group ID.</param>
        /// <param name="directMemberOnly">Whether to exclude members inherited from subgroups.</param>
        /// <returns>The Try result with integer member IDs.</returns>
        public Task<(bool success, string? details, List<int>? members)> TryGetGroupMembers(int groupId, bool directMemberOnly)
        {
            return TryGetGroupMembersCore(groupId, directMemberOnly);
        }

        /// <summary>Reads the endpoint's integer-members envelope.</summary>
        /// <param name="groupId">The group ID.</param>
        /// <param name="directMemberOnly">Optional explicit filtering.</param>
        /// <returns>The Try result.</returns>
        private async Task<(bool success, string? details, List<int>? members)> TryGetGroupMembersCore(int groupId, bool? directMemberOnly)
        {
            Dictionary<string, string>? data = directMemberOnly.HasValue
                ? new Dictionary<string, string> { { "direct_member_only", directMemberOnly.Value.ToString().ToLowerInvariant() } }
                : null;
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"api/v1/user_groups/{groupId}/members", data);

            if (ResponsePayload.TryRead<GroupMembersPayload>(response, "Users.GetGroupMembers", null, JsonValueKind.Object,
                out GroupMembersPayload? payload, out string? details, root =>
                    root.TryGetProperty("members", out JsonElement members) && members.ValueKind == JsonValueKind.Array
                        ? null : "required field 'members' must be Array."))
            {
                return (true, null, payload!.Members);
            }

            return (false, details, null);
        }

        /// <summary>Gets group members (throwing version).</summary>
        public async Task<List<int>> GetGroupMembers(int groupId)
        {
            var result = await TryGetGroupMembers(groupId);
            if (!result.success) throw new Exception(result.details);
            return result.members!;
        }

        /// <summary>Gets group members with explicit direct-member filtering, throwing on failure.</summary>
        /// <param name="groupId">The group ID.</param>
        /// <param name="directMemberOnly">Whether to exclude members inherited from subgroups.</param>
        /// <returns>The integer member IDs.</returns>
        public async Task<List<int>> GetGroupMembers(int groupId, bool directMemberOnly)
        {
            (bool success, string? details, List<int>? members) = await TryGetGroupMembers(groupId, directMemberOnly);
            if (!success) throw new Exception(details);
            return members!;
        }
    }
}
