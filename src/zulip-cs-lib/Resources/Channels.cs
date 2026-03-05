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
        private Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> _doZulipRequest;

        /// <summary>Initializes a new instance of the Channels class.</summary>
        /// <param name="doZulipRequest">The request delegate.</param>
        internal Channels(
            Func<HttpMethod, string, Dictionary<string, string>, Task<ZulipResponse>> doZulipRequest)
        {
            _doZulipRequest = doZulipRequest;
        }

        /// <summary>Gets all channels (streams).</summary>
        /// <remarks>
        /// Feature level 441: stream/subscription payloads include newer channel permission group settings such as <c>can_create_topic_group</c>.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details, streams).</returns>
        public async Task<(bool success, string details, List<StreamObject> streams)> TryGetAll()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, _streamsApiEndpoint, null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Streams ?? new List<StreamObject>());
            }

            return (false, "Channels.GetAll failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets all channels (throwing version).</summary>
        public async Task<List<StreamObject>> GetAll()
        {
            var result = await TryGetAll();
            if (!result.success) throw new Exception(result.details);
            return result.streams;
        }

        /// <summary>Gets a channel by stream ID.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <remarks>Feature level 394: stream objects added <c>subscriber_count</c> in API responses.</remarks>
        /// <returns>An asynchronous result that yields (success, details, stream).</returns>
        public async Task<(bool success, string details, StreamObject stream)> TryGetById(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_streamsApiEndpoint}/{streamId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Stream);
            }

            return (false, "Channels.GetById failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets a channel by stream ID (throwing version).</summary>
        public async Task<StreamObject> GetById(int streamId)
        {
            var result = await TryGetById(streamId);
            if (!result.success) throw new Exception(result.details);
            return result.stream;
        }

        /// <summary>Gets the stream ID by channel name.</summary>
        /// <param name="name">The channel name.</param>
        /// <returns>An asynchronous result that yields (success, details, streamId).</returns>
        public async Task<(bool success, string details, int streamId)> TryGetIdByName(string name)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream", name }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/get_stream_id", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.StreamId);
            }

            return (false, "Channels.GetIdByName failed: " + response.GetFailureMessage(), 0);
        }

        /// <summary>Gets the stream ID by channel name (throwing version).</summary>
        public async Task<int> GetIdByName(string name)
        {
            var result = await TryGetIdByName(name);
            if (!result.success) throw new Exception(result.details);
            return result.streamId;
        }

        /// <summary>Gets the current user's subscriptions.</summary>
        /// <remarks>Feature level 441: subscription payloads include updated channel-level permission group metadata.</remarks>
        /// <returns>An asynchronous result that yields (success, details, subscriptions).</returns>
        public async Task<(bool success, string details, List<SubscriptionObject> subscriptions)> TryGetSubscriptions()
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, "api/v1/users/me/subscriptions", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Subscriptions ?? new List<SubscriptionObject>());
            }

            return (false, "Channels.GetSubscriptions failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets the current user's subscriptions (throwing version).</summary>
        public async Task<List<SubscriptionObject>> GetSubscriptions()
        {
            var result = await TryGetSubscriptions();
            if (!result.success) throw new Exception(result.details);
            return result.subscriptions;
        }

        /// <summary>Subscribes to channels.</summary>
        /// <param name="subscriptions">The subscriptions as JSON array of {name} objects.</param>
        /// <remarks>Feature level 441: subscribe supports newer channel permission-group parameters (for example <c>can_create_topic_group</c>).</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TrySubscribe(string subscriptions)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "subscriptions", subscriptions }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/users/me/subscriptions", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
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
        public async Task<(bool success, string details)> TryUnsubscribe(string[] subscriptions)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "subscriptions", JsonSerializer.Serialize(subscriptions) }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, "api/v1/users/me/subscriptions", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
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
        public async Task<(bool success, string details, List<TopicObject> topics)> TryGetTopics(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_usersApiEndpoint}/me/{streamId}/topics", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Topics ?? new List<TopicObject>());
            }

            return (false, "Channels.GetTopics failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>The users API endpoint for topic queries.</summary>
        private const string _usersApiEndpoint = "api/v1/users";

        /// <summary>Gets topics for a stream (throwing version).</summary>
        public async Task<List<TopicObject>> GetTopics(int streamId)
        {
            var result = await TryGetTopics(streamId);
            if (!result.success) throw new Exception(result.details);
            return result.topics;
        }

        /// <summary>Gets subscribers of a stream.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <remarks>Feature level 79: subscriber listing endpoint has long-standing support tracked in API history.</remarks>
        /// <returns>An asynchronous result that yields (success, details, subscribers).</returns>
        public async Task<(bool success, string details, List<int> subscribers)> TryGetSubscribers(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_streamsApiEndpoint}/{streamId}/members", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.Subscribers ?? new List<int>());
            }

            return (false, "Channels.GetSubscribers failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets subscribers of a stream (throwing version).</summary>
        public async Task<List<int>> GetSubscribers(int streamId)
        {
            var result = await TryGetSubscribers(streamId);
            if (!result.success) throw new Exception(result.details);
            return result.subscribers;
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
        public async Task<(bool success, string details)> TryUpdate(
            int streamId,
            string description = null,
            string newName = null,
            bool? isPrivate = null,
            bool? isWebPublic = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (description != null) data.Add("description", description);
            if (newName != null) data.Add("new_name", newName);
            if (isPrivate != null) data.Add("is_private", isPrivate.Value.ToString().ToLowerInvariant());
            if (isWebPublic != null) data.Add("is_web_public", isWebPublic.Value.ToString().ToLowerInvariant());

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, $"{_streamsApiEndpoint}/{streamId}", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Channels.Update failed: " + response.GetFailureMessage());
        }

        /// <summary>Updates a stream/channel (throwing version).</summary>
        public async Task Update(int streamId, string description = null, string newName = null, bool? isPrivate = null, bool? isWebPublic = null)
        {
            var result = await TryUpdate(streamId, description, newName, isPrivate, isWebPublic);
            if (!result.success) throw new Exception(result.details);
        }

        /// <summary>Archives a stream/channel.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <remarks>Feature level 349: channel/organization admins can archive streams with metadata access under updated rules.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryArchive(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, $"{_streamsApiEndpoint}/{streamId}", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
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
        public async Task<(bool success, string details, string email)> TryGetEmailAddress(int streamId)
        {
            ZulipResponse response = await _doZulipRequest(HttpMethod.Get, $"{_streamsApiEndpoint}/{streamId}/email_address", null);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null, response.EmailAddress);
            }

            return (false, "Channels.GetEmailAddress failed: " + response.GetFailureMessage(), null);
        }

        /// <summary>Gets email address for a stream (throwing version).</summary>
        public async Task<string> GetEmailAddress(int streamId)
        {
            var result = await TryGetEmailAddress(streamId);
            if (!result.success) throw new Exception(result.details);
            return result.email;
        }

        /// <summary>Deletes a topic.</summary>
        /// <param name="streamId">The stream ID.</param>
        /// <param name="topicName">The topic name.</param>
        /// <remarks>Feature level 256: dedicated topic-deletion endpoint support is tracked in changelog history.</remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryDeleteTopic(int streamId, string topicName)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "topic_name", topicName }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, $"{_streamsApiEndpoint}/{streamId}/delete_topic", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
            {
                return (true, null);
            }

            return (false, "Channels.DeleteTopic failed: " + response.GetFailureMessage());
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
        public async Task<(bool success, string details)> TryAddDefaultChannel(int streamId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream_id", streamId.ToString() }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Post, "api/v1/default_streams", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
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
        public async Task<(bool success, string details)> TryRemoveDefaultChannel(int streamId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream_id", streamId.ToString() }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Delete, "api/v1/default_streams", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
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

        /// <summary>Updates topic muting.</summary>
        /// <param name="stream">The stream name.</param>
        /// <param name="topic">The topic name.</param>
        /// <param name="op">The operation (add or remove).</param>
        /// <remarks>
        /// Feature level 170: topic muting support exists via this endpoint.
        /// Newer Zulip API uses <c>POST /user_topics</c>; this wrapper currently targets the older muted-topics route.
        /// </remarks>
        /// <returns>An asynchronous result that yields (success, details).</returns>
        public async Task<(bool success, string details)> TryUpdateTopicMuting(string stream, string topic, string op)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "stream", stream },
                { "topic", topic },
                { "op", op }
            };

            ZulipResponse response = await _doZulipRequest(HttpMethod.Patch, "api/v1/users/me/subscriptions/muted_topics", data);

            if (response.Result == ZulipResponse.ZulipResultSuccess)
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
