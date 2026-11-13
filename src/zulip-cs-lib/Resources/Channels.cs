using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using zulip_cs_lib.Models;

namespace zulip_cs_lib.Resources
{
    /// <summary>Channels (streams) resource.</summary>
    public class Channels
    {
        /// <summary>The streams API endpoint.</summary>
        private const string _streamsApiEndpoint = "api/v1/streams";

        /// <summary>The Zulip request delegate.</summary>
        private Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Channels class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Channels(
            Func<HttpMethod, string, Dictionary<string, string>?, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all channels (streams).</summary>
        /// <remarks>
        /// Feature level 441: stream/subscription payloads include newer channel permission group settings such as <c>can_create_topic_group</c>.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details, streams).</returns>
        public async Task<(bool success, string? details, List<StreamObject>? streams)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _streamsApiEndpoint, null);

            if (ResponsePayload.TryRead<List<StreamObject>>(response, "Channels.GetAll",
                "streams", JsonValueKind.Array, out List<StreamObject>? streams, out string? details,
                array => ResponsePayload.ValidateObjectIds(array, "stream_id")))
            {
                return (true, null, streams);
            }

            return (false, details, null);
        }

        /// <summary>Gets all channels (throwing version).</summary>
        public async Task<List<StreamObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.streams!;
        }

        /// <summary>Gets a channel by stream ID.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <remarks>Feature level 394: stream objects added <c>subscriber_count</c> in API responses.</remarks>
        /// <returns>An asynchronous result that yields (success, details, stream).</returns>
        public async Task<(bool success, string? details, StreamObject? stream)> TryGetById(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_streamsApiEndpoint}/{streamId}", null);

            if (ResponsePayload.TryRead<StreamObject>(response, "Channels.GetById",
                "stream", JsonValueKind.Object, out StreamObject? stream, out string? details,
                value => ResponsePayload.ValidateObjectId(value, "stream_id")))
            {
                return (true, null, stream);
            }

            return (false, details, null);
        }

        /// <summary>Gets a channel by stream ID (throwing version).</summary>
        public async Task<StreamObject> GetById(int streamId)
        {
            var result = await TryGetById(streamId);
            if (!result.success) throw new Exception(result.details);
            return result.stream!;
        }

        /// <summary>Gets the stream ID by channel name.</summary>
        /// <param name="name">The channel name.</param>
        /// <returns>An asynchronous result that yields (success, details, streamId).</returns>
        public async Task<(bool success, string? details, int streamId)> TryGetIdByName(string name)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream", name }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/get_stream_id", data);

            if (ResponsePayload.TryRead<int>(response, "Channels.GetIdByName",
                "stream_id", JsonValueKind.Number, out int streamId, out string? details))
            {
                return (true, null, streamId);
            }

            return (false, details, 0);
        }

        /// <summary>Gets the stream ID by channel name (throwing version).</summary>
        public async Task<int> GetIdByName(string name)
        {
            var result = await TryGetIdByName(name);
            if (!result.success) throw new Exception(result.details);
            return result.streamId;
        }

        /// <summary>Creates a channel and returns its required identity without implicit discovery or subscription retries.</summary>
        /// <param name="name">The new channel name.</param>
        /// <param name="subscribers">The initial subscriber user IDs; an empty array is allowed.</param>
        /// <param name="options">Optional initial settings; omitted values retain server defaults.</param>
        /// <remarks>Requires feature level 417. The creation endpoint returns the identity as <c>id</c>, exposed here as streamId.</remarks>
        /// <returns>The Try outcome and channel ID.</returns>
        public async Task<(bool success, string? details, int streamId)> TryCreateWithId(
            string name, int[] subscribers, ChannelCreationOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "Channels.Create failed: name must be nonempty.", 0);
            }
            if (subscribers == null || !GroupOptions.ValidIds(subscribers))
            {
                return (false, "Channels.Create failed: subscribers must be an array of positive integer IDs.", 0);
            }

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "name", name },
                { "subscribers", JsonSerializer.Serialize(subscribers) }
            };
            string? inputError = options?.AddTo(data);
            if (inputError != null) return (false, "Channels.Create failed: " + inputError, 0);

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/channels/create", data);
            if (ResponsePayload.TryRead<int>(response, "Channels.Create",
                "id", JsonValueKind.Number, out int streamId, out string? details))
            {
                return (true, null, streamId);
            }
            return (false, details, 0);
        }

        /// <summary>Creates a channel and returns its ID, throwing on an invalid response or request.</summary>
        /// <param name="name">The new channel name.</param>
        /// <param name="subscribers">The initial subscriber user IDs.</param>
        /// <param name="options">Optional initial settings.</param>
        /// <returns>The created channel ID.</returns>
        public async Task<int> CreateWithId(string name, int[] subscribers, ChannelCreationOptions? options = null)
        {
            (bool success, string? details, int streamId) = await TryCreateWithId(name, subscribers, options);
            if (!success) throw new Exception(details);
            return streamId;
        }

        /// <summary>Gets the current user's subscriptions.</summary>
        /// <remarks>Feature level 441: subscription payloads include updated channel-level permission group metadata.</remarks>
        /// <returns>An asynchronous result that yields (success, details, subscriptions).</returns>
        public async Task<(bool success, string? details, List<SubscriptionObject>? subscriptions)> TryGetSubscriptions()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/users/me/subscriptions", null);

            if (ResponsePayload.TryRead<List<SubscriptionObject>>(response, "Channels.GetSubscriptions",
                "subscriptions", JsonValueKind.Array, out List<SubscriptionObject>? subscriptions, out string? details,
                array => ResponsePayload.ValidateObjectIds(array, "stream_id")))
            {
                return (true, null, subscriptions);
            }

            return (false, details, null);
        }

        /// <summary>Gets the current user's subscriptions (throwing version).</summary>
        public async Task<List<SubscriptionObject>> GetSubscriptions()
        {
            var result = await TryGetSubscriptions();
            if (!result.success) throw new Exception(result.details);
            return result.subscriptions!;
        }

        /// <summary>Checks whether a user is subscribed to a channel.</summary>
        /// <param name="userId">The target user ID.</param>
        /// <param name="streamId">The channel ID.</param>
        /// <remarks>Requires feature level 12; bot-user subscriptions can be queried at feature level 458.</remarks>
        /// <returns>The Try outcome and actual subscription status, including false.</returns>
        public async Task<(bool success, string? details, bool isSubscribed)> TryGetSubscriptionStatus(int userId, int streamId)
        {
            if (userId <= 0 || streamId <= 0)
            {
                return (false, "Channels.GetSubscriptionStatus failed: userId and streamId must be positive integers.", false);
            }

            ZulipResponse response = await _doZulipRequest(HttpMethod.Get,
                $"{_usersApiEndpoint}/{userId}/subscriptions/{streamId}", null);
            if (ResponsePayload.TryRead<JsonElement>(response, "Channels.GetSubscriptionStatus",
                null, JsonValueKind.Object, out JsonElement payload, out string? details, root =>
                    root.TryGetProperty("is_subscribed", out JsonElement value) &&
                    value.ValueKind is JsonValueKind.True or JsonValueKind.False
                        ? null : "required field 'is_subscribed' must be Boolean."))
            {
                return (true, null, payload.GetProperty("is_subscribed").GetBoolean());
            }
            return (false, details, false);
        }

        /// <summary>Checks subscription status, throwing on request or payload failure.</summary>
        /// <param name="userId">The target user ID.</param>
        /// <param name="streamId">The channel ID.</param>
        /// <returns>Whether the user is subscribed.</returns>
        public async Task<bool> GetSubscriptionStatus(int userId, int streamId)
        {
            (bool success, string? details, bool isSubscribed) = await TryGetSubscriptionStatus(userId, streamId);
            if (!success) throw new Exception(details);
            return isSubscribed;
        }

        /// <summary>Updates typed personal subscription properties in one bulk request.</summary>
        /// <param name="changes">The property changes; an empty list is sent as an empty array.</param>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryUpdateSubscriptionSettings(
            IReadOnlyList<SubscriptionPropertyChange> changes)
        {
            if (changes == null) return (false, "Channels.UpdateSubscriptionSettings failed: changes must be an array.");
            foreach (SubscriptionPropertyChange change in changes)
            {
                string? error = change == null ? "changes must not contain null." : change.Validate();
                if (error != null) return (false, "Channels.UpdateSubscriptionSettings failed: " + error);
            }

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "subscription_data", JsonSerializer.Serialize(changes) }
            };
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post,
                $"{_usersApiEndpoint}/me/subscriptions/properties", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null) : (false, "Channels.UpdateSubscriptionSettings failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates typed personal subscription properties, throwing on failure.</summary>
        /// <param name="changes">The property changes.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task UpdateSubscriptionSettings(IReadOnlyList<SubscriptionPropertyChange> changes)
        {
            (bool success, string? details) = await TryUpdateSubscriptionSettings(changes);
            if (!success) throw new Exception(details);
        }

        /// <summary>Updates one personal subscription property using the single-channel PATCH endpoint.</summary>
        /// <param name="change">The channel, property and typed new value.</param>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryUpdateSubscriptionProperty(SubscriptionPropertyChange change)
        {
            string? error = change == null ? "change must not be null." : change.Validate();
            if (error != null) return (false, "Channels.UpdateSubscriptionProperty failed: " + error);

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "property", change!.PropertyName },
                { "value", change.Property == SubscriptionProperty.Color ? change.Value.GetString()! : change.Value.GetRawText() }
            };
            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch,
                $"{_usersApiEndpoint}/me/subscriptions/{change.StreamId}", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null) : (false, "Channels.UpdateSubscriptionProperty failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates one personal subscription property, throwing on failure.</summary>
        /// <param name="change">The channel, property and typed new value.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task UpdateSubscriptionProperty(SubscriptionPropertyChange change)
        {
            (bool success, string? details) = await TryUpdateSubscriptionProperty(change);
            if (!success) throw new Exception(details);
        }

        /// <summary>Subscribes to channels.</summary>
        /// <param name="subscriptions">The subscriptions as JSON array of {name} objects.</param>
        /// <remarks>Feature level 441: subscribe supports newer channel permission-group parameters (for example <c>can_create_topic_group</c>).</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TrySubscribe(string subscriptions)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "subscriptions", subscriptions }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/users/me/subscriptions", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Channels.Subscribe failed: " + response.GetFailureMessage());
        }

        /// <summary>Subscribes to channels (throwing version).</summary>
        public async Task Subscribe(string subscriptions)
        {
            var result = await TrySubscribe(subscriptions);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Unsubscribes from channels.</summary>
        /// <param name="subscriptions">The channel names to unsubscribe from.</param>
        /// <remarks>Feature level 362: unsubscribe/edit operations support archived channels when permissions allow.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryUnsubscribe(string[] subscriptions)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "subscriptions", JsonSerializer.Serialize(subscriptions) }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, "api/v1/users/me/subscriptions", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Channels.Unsubscribe failed: " + response.GetFailureMessage());
        }

        /// <summary>Unsubscribes from channels (throwing version).</summary>
        public async Task Unsubscribe(string[] subscriptions)
        {
            var result = await TryUnsubscribe(subscriptions);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets topics for a stream.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <remarks>Feature level 334: topic-list APIs added support for empty topic names via <c>allow_empty_topic_name</c>.</remarks>
        /// <returns>An asynchronous result that yields (success, details, topics).</returns>
        public async Task<(bool success, string? details, List<TopicObject>? topics)> TryGetTopics(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/me/{streamId}/topics", null);

            if (ResponsePayload.TryRead<List<TopicObject>>(response, "Channels.GetTopics",
                "topics", JsonValueKind.Array, out List<TopicObject>? topics, out string? details,
                array => ResponsePayload.ValidateElements(array, JsonValueKind.Object)))
            {
                return (true, null, topics);
            }

            return (false, details, null);
        }

        /// <summary>The users API endpoint for topic queries.</summary>
        private const string _usersApiEndpoint = "api/v1/users";

        /// <summary>Gets topics for a stream (throwing version).</summary>
        public async Task<List<TopicObject>> GetTopics(int streamId)
        {
            var result = await TryGetTopics(streamId);
            if (!result.success) throw new Exception(result.details);
            return result.topics!;
        }

        /// <summary>Gets subscribers of a stream.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <remarks>Feature level 79: subscriber listing endpoint has long-standing support tracked in API history.</remarks>
        /// <returns>An asynchronous result that yields (success, details, subscribers).</returns>
        public async Task<(bool success, string? details, List<int>? subscribers)> TryGetSubscribers(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_streamsApiEndpoint}/{streamId}/members", null);

            if (ResponsePayload.TryRead<List<int>>(response, "Channels.GetSubscribers",
                "subscribers", JsonValueKind.Array, out List<int>? subscribers, out string? details))
            {
                return (true, null, subscribers);
            }

            return (false, details, null);
        }

        /// <summary>Gets subscribers of a stream (throwing version).</summary>
        public async Task<List<int>> GetSubscribers(int streamId)
        {
            var result = await TryGetSubscribers(streamId);
            if (!result.success) throw new Exception(result.details);
            return result.subscribers!;
        }

        /// <summary>Updates a stream/channel.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <param name="description">(Optional) New description.</param>
        /// <param name="newName">(Optional) New name.</param>
        /// <param name="isPrivate">(Optional) Privacy setting.</param>
        /// <param name="isWebPublic">(Optional) Web public setting.</param>
        /// <remarks>
        /// Feature level 441: stream update supports newer permission-group parameters in Zulip API.
        /// Feature level 349: administrators with metadata access can modify many stream settings without content access.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryUpdate(
            int streamId,
            string? description = null,
            string? newName = null,
            bool? isPrivate = null,
            bool? isWebPublic = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (description != null) data.Add("description", description);
            if (newName != null) data.Add("new_name", newName);
            if (isPrivate != null) data.Add("is_private", isPrivate.Value.ToString().ToLowerInvariant());
            if (isWebPublic != null) data.Add("is_web_public", isWebPublic.Value.ToString().ToLowerInvariant());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_streamsApiEndpoint}/{streamId}", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Channels.Update failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a stream/channel (throwing version).</summary>
        public async Task Update(int streamId, string? description = null, string? newName = null, bool? isPrivate = null, bool? isWebPublic = null)
        {
            var result = await TryUpdate(streamId, description, newName, isPrivate, isWebPublic);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Archives a stream/channel.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <remarks>Feature level 349: channel/organization admins can archive streams with metadata access under updated rules.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryArchive(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_streamsApiEndpoint}/{streamId}", null);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Channels.Archive failed: " + response.GetFailureMessage());
        }

        /// <summary>Archives a stream/channel (throwing version).</summary>
        public async Task Archive(int streamId)
        {
            var result = await TryArchive(streamId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Gets email address for a stream.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <remarks>Feature level 448: access to a channel email address depends on permission to post to that channel.</remarks>
        /// <returns>An asynchronous result that yields (success, details, email).</returns>
        public async Task<(bool success, string? details, string? email)> TryGetEmailAddress(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_streamsApiEndpoint}/{streamId}/email_address", null);

            if (ResponsePayload.TryRead<string>(response, "Channels.GetEmailAddress",
                "email", JsonValueKind.String, out string? email, out string? details,
                value => string.IsNullOrWhiteSpace(value.GetString()) ? "required field 'email' must be nonempty." : null))
            {
                return (true, null, email);
            }

            return (false, details, null);
        }

        /// <summary>Gets email address for a stream (throwing version).</summary>
        public async Task<string> GetEmailAddress(int streamId)
        {
            var result = await TryGetEmailAddress(streamId);
            if (!result.success) throw new Exception(result.details);
            return result.email!;
        }

        /// <summary>Deletes a topic.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <param name="topicName">The topic name.</param>
        /// <remarks>Feature level 256: dedicated topic-deletion endpoint support is tracked in changelog history.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryDeleteTopic(int streamId, string topicName)
        {
            var result = await TryDeleteTopicBatch(streamId, topicName);
            return result.success
                ? result.batch!.ToCompletionResult("Channels.DeleteTopic", nameof(TryDeleteTopicBatch))
                : (false, result.details);
        }

        /// <summary>Deletes one topic batch without automatically repeating partial work.</summary>
        /// <param name="streamId">The channel ID.</param>
        /// <param name="topicName">The topic name.</param>
        /// <returns>The Try outcome and explicit completion state.</returns>
        public async Task<(bool success, string? details, BatchOperationResult? batch)> TryDeleteTopicBatch(
            int streamId, string topicName)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "topic_name", topicName }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_streamsApiEndpoint}/{streamId}/delete_topic", data);

            return BatchOperationResult.Read(response, "Channels.DeleteTopic");
        }

        /// <summary>Deletes one topic batch, throwing on request failure but returning partial completion.</summary>
        /// <param name="streamId">The channel ID.</param>
        /// <param name="topicName">The topic name.</param>
        /// <returns>The batch outcome; callers control continuation.</returns>
        public async Task<BatchOperationResult> DeleteTopicBatch(int streamId, string topicName)
        {
            var result = await TryDeleteTopicBatch(streamId, topicName);
            if (!result.success) throw new Exception(result.details);
            return result.batch!;
        }

        /// <summary>Deletes a topic (throwing version).</summary>
        public async Task DeleteTopic(int streamId, string topicName)
        {
            var result = await TryDeleteTopic(streamId, topicName);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Adds a default channel.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryAddDefaultChannel(int streamId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream_id", streamId.ToString() }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/default_streams", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Channels.AddDefaultChannel failed: " + response.GetFailureMessage());
        }

        /// <summary>Adds a default channel (throwing version).</summary>
        public async Task AddDefaultChannel(int streamId)
        {
            var result = await TryAddDefaultChannel(streamId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Removes a default channel.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryRemoveDefaultChannel(int streamId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream_id", streamId.ToString() }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, "api/v1/default_streams", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Channels.RemoveDefaultChannel failed: " + response.GetFailureMessage());
        }

        /// <summary>Removes a default channel (throwing version).</summary>
        public async Task RemoveDefaultChannel(int streamId)
        {
            var result = await TryRemoveDefaultChannel(streamId);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Updates the current user's visibility policy for one channel/topic pair.</summary>
        /// <param name="streamId">The channel ID.</param>
        /// <param name="topic">The topic name; empty topics require feature level 334.</param>
        /// <param name="policy">The policy to set, or None to remove a previous policy.</param>
        /// <remarks>Requires feature level 170; Followed requires feature level 219. No messages need to exist in the topic.</remarks>
        /// <returns>The Try outcome.</returns>
        public async Task<(bool success, string? details)> TryUpdateTopicVisibility(
            int streamId, string topic, TopicVisibilityPolicy policy)
        {
            if (streamId <= 0 || topic == null || (int)policy is < 0 or > 3)
            {
                return (false, "Channels.UpdateTopicVisibility failed: a positive streamId, topic and visibility policy 0-3 are required.");
            }
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream_id", streamId.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "topic", topic },
                { "visibility_policy", ((int)policy).ToString(System.Globalization.CultureInfo.InvariantCulture) }
            };
            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/user_topics", data);
            return ResponsePayload.IsSuccessful(response)
                ? (true, null) : (false, "Channels.UpdateTopicVisibility failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a personal topic visibility policy, throwing on failure.</summary>
        /// <param name="streamId">The channel ID.</param>
        /// <param name="topic">The topic name.</param>
        /// <param name="policy">The policy to set.</param>
        /// <returns>An asynchronous result.</returns>
        public async Task UpdateTopicVisibility(int streamId, string topic, TopicVisibilityPolicy policy)
        {
            (bool success, string? details) = await TryUpdateTopicVisibility(streamId, topic, policy);
            if (!success) throw new Exception(details);
        }

        /// <summary>Updates topic muting.</summary>
        /// <param name="stream">The stream name.</param>
        /// <param name="topic">The topic name.</param>
        /// <param name="op">The operation (add or remove).</param>
        /// <remarks>
        /// Feature level 170: topic muting support exists via this endpoint.
        /// Retained for compatibility. Prefer <see cref="UpdateTopicVisibility"/> with <c>POST /user_topics</c>,
        /// which also supports explicit unmuting and following.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string? details)> TryUpdateTopicMuting(string stream, string topic, string op)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream", stream },
                { "topic", topic },
                { "op", op }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, "api/v1/users/me/subscriptions/muted_topics", data);

            if (ResponsePayload.IsSuccessful(response))
            {
                return (true, null);
            }

            return (false, "Channels.UpdateTopicMuting failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates topic muting (throwing version).</summary>
        public async Task UpdateTopicMuting(string stream, string topic, string op)
        {
            var result = await TryUpdateTopicMuting(stream, topic, op);
            if (!result.success) throw new Exception(result.details);
        }
    }
}
