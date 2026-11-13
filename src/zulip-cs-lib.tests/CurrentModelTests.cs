using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using zulip_cs_lib.Models;

namespace zulip_set_lib.tests
{
    /// <summary>Tests current workflow metadata and named/anonymous permission group wire contracts.</summary>
    public class CurrentModelTests
    {
        /// <summary>Verifies reactions, content edits and moves retain distinct metadata and embedded history.</summary>
        [Fact]
        public void Message_RetainsReactionsAndEditMoveMetadata()
        {
            MessageObject message = Read<MessageObject>("""
                {
                  "id": 42, "sender_id": 10, "recipient_id": 7, "timestamp": 1700000000,
                  "content": "<p>Updated!</p>", "content_type": "text/html", "type": "stream",
                  "subject": "new topic", "stream_id": 8, "display_recipient": "general",
                  "flags": ["read", "starred"], "is_me_message": false,
                  "last_edit_timestamp": 1700000010, "last_moved_timestamp": 1700000020,
                  "reactions": [
                    {"emoji_name": "thumbs_up", "emoji_code": "1f44d", "reaction_type": "unicode_emoji",
                     "user_id": 11, "future_reaction": {"label": "👍"}},
                    {"emoji_name": "team", "emoji_code": "17", "reaction_type": "realm_emoji", "user_id": 12}
                  ],
                  "edit_history": [
                    {"user_id": 12, "timestamp": 1700000020, "prev_stream": 7, "stream": 8,
                     "prev_topic": "old topic", "topic": "new topic", "future_history": {"reason": "move"}},
                    {"user_id": null, "timestamp": 1700000010, "prev_content": "Original!",
                     "prev_rendered_content": "<p>Original!</p>"}
                  ],
                  "topic_links": [{"text": "#42", "url": "https://example.com/42"}],
                  "future_message": {"state": [1, true, null]}
                }
                """);

            Assert.Equal(1700000010L, message.LastEditTimestamp);
            Assert.Equal(1700000020L, message.LastMovedTimestamp);
            Assert.Equal("new topic", message.Subject);
            Assert.Equal(8, message.StreamId);
            Assert.Equal<string>(["read", "starred"], message.Flags!);
            List<MessageReactionObject> reactions = Assert.IsType<List<MessageReactionObject>>(message.Reactions);
            Assert.Equal(2, reactions.Count);
            Assert.Equal("1f44d", reactions[0].EmojiCode);
            Assert.Equal("unicode_emoji", reactions[0].ReactionType);
            Assert.Equal(11, reactions[0].UserId);
            Assert.Equal("realm_emoji", reactions[1].ReactionType);
            Assert.Equal("17", reactions[1].EmojiCode);
            List<MessageHistoryObject> history = Assert.IsType<List<MessageHistoryObject>>(message.EditHistory);
            Assert.Equal(7, history[0].PrevStream);
            Assert.Equal(8, history[0].Stream);
            Assert.Equal("old topic", history[0].PrevTopic);
            Assert.Equal("new topic", history[0].Topic);
            Assert.Null(history[0].PrevContent);
            Assert.Null(history[1].UserId);
            Assert.Equal("<p>Original!</p>", history[1].PrevRenderedContent);

            MessageObject copy = RoundTrip(message);
            Assert.Equal("👍", copy.Reactions![0].AdditionalProperties["future_reaction"].GetProperty("label").GetString());
            Assert.Equal("move", copy.EditHistory![0].AdditionalProperties["future_history"].GetProperty("reason").GetString());
            Assert.Equal("https://example.com/42", copy.AdditionalProperties["topic_links"][0].GetProperty("url").GetString());
            Assert.Equal(JsonValueKind.Null, copy.AdditionalProperties["future_message"].GetProperty("state")[2].ValueKind);
            Assert.Equal(1700000020L, copy.LastMovedTimestamp);
        }

        /// <summary>Verifies the edit-history endpoint retains original snapshots, content diffs and channel moves.</summary>
        [Fact]
        public void MessageHistory_RetainsSnapshotsAndContentDiff()
        {
            List<MessageHistoryObject> history = Read<List<MessageHistoryObject>>("""
                [
                  {"user_id": 10, "timestamp": 1700000000, "topic": "old topic", "content": "Original!",
                   "rendered_content": "<p>Original!</p>"},
                  {"user_id": 10, "timestamp": 1700000010, "topic": "old topic", "content": "Updated!",
                   "rendered_content": "<p>Updated!</p>", "prev_content": "Original!",
                   "prev_rendered_content": "<p>Original!</p>",
                   "content_html_diff": "<p><ins>Updated!</ins><del>Original!</del></p>"},
                  {"user_id": 12, "timestamp": 1700000020, "prev_stream": 7, "stream": 8,
                   "prev_topic": "old topic", "topic": "new topic", "future_snapshot": [true, 3]}
                ]
                """);

            Assert.Equal("Original!", history[0].Content);
            Assert.Null(history[0].ContentHtmlDiff);
            Assert.Null(history[0].PrevStream);
            Assert.Equal("Updated!", history[1].Content);
            Assert.Equal("<p>Updated!</p>", history[1].RenderedContent);
            Assert.Equal("<p><ins>Updated!</ins><del>Original!</del></p>", history[1].ContentHtmlDiff);
            Assert.Equal(7, history[2].PrevStream);
            Assert.Equal(8, history[2].Stream);
            Assert.Null(history[2].Content);
            List<MessageHistoryObject> copy = RoundTrip(history);
            Assert.Equal(history[1].ContentHtmlDiff, copy[1].ContentHtmlDiff);
            Assert.True(copy[2].AdditionalProperties["future_snapshot"][0].GetBoolean());
        }

        /// <summary>Verifies current channel metadata and every permission setting on streams and subscriptions.</summary>
        /// <param name="subscription">Whether to read the shared channel fixture as a subscription.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Channel_RetainsGovernanceAndSubscriptionDefaults(bool subscription)
        {
            const string json = """
                {
                  "stream_id": 7, "name": "general", "description": "**Team**",
                  "rendered_description": "<p><strong>Team</strong></p>",
                  "invite_only": true, "is_web_public": false, "is_archived": true,
                  "date_created": 1700000000, "creator_id": 10, "folder_id": 4,
                  "topics_policy": "empty_topic_only", "message_retention_days": -1,
                  "default_push_notifications": true, "history_public_to_subscribers": false,
                  "first_message_id": 42, "is_recently_active": true, "stream_weekly_traffic": 15,
                  "subscriber_count": 3, "stream_post_policy": 1,
                  "can_add_subscribers_group": 21, "can_remove_subscribers_group": 22,
                  "can_administer_channel_group": {"direct_members": [10], "direct_subgroups": [23]},
                  "can_delete_any_message_group": 24, "can_delete_own_message_group": 25,
                  "can_move_messages_out_of_channel_group": 26, "can_move_messages_within_channel_group": 27,
                  "can_send_message_group": {"direct_members": [11], "direct_subgroups": [28],
                                             "future_permission": {"label": "posting"}},
                  "can_subscribe_group": 29, "can_resolve_topics_group": 30, "can_create_topic_group": 31,
                  "future_channel": {"policy": "next"},
                  "color": "#aabbcc", "pin_to_top": true, "is_muted": false,
                  "desktop_notifications": null, "audible_notifications": true,
                  "push_notifications": false, "email_notifications": null, "wildcard_mentions_notify": null,
                  "partial_subscribers": [10, 11], "email_address": "general@example.com"
                }
                """;
            StreamObject channel = subscription ? Read<SubscriptionObject>(json) : Read<StreamObject>(json);

            Assert.True(channel.IsArchived);
            Assert.Equal(4, channel.FolderId);
            Assert.Equal("empty_topic_only", channel.TopicsPolicy);
            Assert.True(channel.DefaultPushNotifications);
            Assert.Equal(10, channel.CreatorId);
            Assert.Equal(1700000000L, channel.DateCreated);
            Assert.Equal(-1, channel.MessageRetentionDays);
            Assert.False(channel.HistoryPublicToSubscribers);
            Assert.Equal(42L, channel.FirstMessageId);
            Assert.True(channel.IsRecentlyActive);
            Assert.Equal(15, channel.StreamWeeklyTraffic);
            Assert.Equal(3d, channel.SubscriberCount);
            Assert.Equal(1, channel.StreamPostPolicy);
            Assert.Equal(21, channel.CanAddSubscribersGroup!.GroupId);
            Assert.Equal(22, channel.CanRemoveSubscribersGroup!.GroupId);
            Assert.Equal<int>([10], channel.CanAdministerChannelGroup!.DirectMembers!);
            Assert.Equal<int>([23], channel.CanAdministerChannelGroup.DirectSubgroups!);
            Assert.Equal(24, channel.CanDeleteAnyMessageGroup!.GroupId);
            Assert.Equal(25, channel.CanDeleteOwnMessageGroup!.GroupId);
            Assert.Equal(26, channel.CanMoveMessagesOutOfChannelGroup!.GroupId);
            Assert.Equal(27, channel.CanMoveMessagesWithinChannelGroup!.GroupId);
            Assert.Null(channel.CanSendMessageGroup!.GroupId);
            Assert.Equal<int>([11], channel.CanSendMessageGroup.DirectMembers!);
            Assert.Equal<int>([28], channel.CanSendMessageGroup.DirectSubgroups!);
            Assert.Equal(29, channel.CanSubscribeGroup!.GroupId);
            Assert.Equal(30, channel.CanResolveTopicsGroup!.GroupId);
            Assert.Equal(31, channel.CanCreateTopicGroup!.GroupId);

            string serialized = subscription
                ? JsonSerializer.Serialize((SubscriptionObject)channel)
                : JsonSerializer.Serialize(channel);
            StreamObject copy = subscription ? Read<SubscriptionObject>(serialized) : Read<StreamObject>(serialized);
            Assert.Equal("next", copy.AdditionalProperties["future_channel"].GetProperty("policy").GetString());
            Assert.Equal("posting", copy.CanSendMessageGroup!.AdditionalProperties["future_permission"].GetProperty("label").GetString());
            Assert.Equal(4, copy.FolderId);
            Assert.True(copy.DefaultPushNotifications);

            if (subscription)
            {
                SubscriptionObject preferences = Assert.IsType<SubscriptionObject>(copy);
                Assert.Null(preferences.DesktopNotifications);
                Assert.Null(preferences.EmailNotifications);
                Assert.Null(preferences.WildcardMentionsNotify);
                Assert.False(preferences.PushNotifications);
                Assert.True(preferences.AudibleNotifications);
                Assert.True(preferences.PinToTop);
                Assert.False(preferences.IsMuted);
                Assert.Equal("#aabbcc", preferences.Color);
                Assert.Equal<int>([10, 11], preferences.PartialSubscribers!);
                Assert.Null(preferences.Subscribers);
                Assert.Equal("general@example.com", preferences.EmailAddress);
            }
        }

        /// <summary>Verifies imported and permanently deleted user metadata survives reads and round trips.</summary>
        [Fact]
        public void User_RetainsImportAndDeletionState()
        {
            List<UserObject> users = Read<List<UserObject>>("""
                [
                  {"user_id": 10, "email": "imported@example.com", "delivery_email": null,
                   "full_name": "Imported User", "is_active": true, "is_bot": false, "role": 400,
                   "avatar_url": null, "timezone": "UTC", "date_joined": "2023-11-14T22:13:20Z",
                   "is_imported_stub": true, "profile_data": {"1": {"value": "Imported"}},
                   "future_user": {"source": "archive"}},
                  {"user_id": 11, "email": "deleted@example.com", "full_name": "Deleted User",
                   "is_active": false, "is_imported_stub": false, "is_deleted": true}
                ]
                """);
            Assert.True(users[0].IsImportedStub);
            Assert.Null(users[0].IsDeleted);
            Assert.Equal("2023-11-14T22:13:20Z", users[0].DateJoined);
            Assert.Null(users[0].DeliveryEmail);
            Assert.False(users[1].IsActive);
            Assert.True(users[1].IsDeleted);
            Assert.False(users[1].IsImportedStub);
            List<UserObject> copy = RoundTrip(users);
            Assert.True(copy[0].IsImportedStub);
            Assert.True(copy[1].IsDeleted);
            Assert.Equal("Imported", copy[0].AdditionalProperties["profile_data"].GetProperty("1").GetProperty("value").GetString());
            Assert.Equal("archive", copy[0].AdditionalProperties["future_user"].GetProperty("source").GetString());
        }

        /// <summary>Verifies email and reusable invitations retain lifecycle data without confusing their identities.</summary>
        [Fact]
        public void Invite_RetainsExpiryRoleTimeAndNotificationPreference()
        {
            List<InviteObject> invites = Read<List<InviteObject>>("""
                [
                  {"id": 1, "is_multiuse": false, "invited_by_user_id": 10, "email": "guest@example.com",
                   "invited": 1700000000, "expiry_date": 1700864000, "invited_as": 600,
                   "notify_referrer_on_join": false, "future_invite": {"delivery": "pending"}},
                  {"id": 1, "is_multiuse": true, "invited_by_user_id": 11,
                   "link_url": "https://example.com/join/secret", "invited": 1700000001,
                   "expiry_date": null, "invited_as": 300, "notify_referrer_on_join": true}
                ]
                """);
            Assert.Equal(invites[0].Id, invites[1].Id);
            Assert.False(invites[0].IsMultiuse);
            Assert.Equal("guest@example.com", invites[0].Email);
            Assert.Null(invites[0].LinkUrl);
            Assert.Equal(1700000000L, invites[0].Invited);
            Assert.Equal(1700864000L, invites[0].ExpiryDate);
            Assert.Equal(600, invites[0].InvitedAs);
            Assert.False(invites[0].NotifyReferrerOnJoin);
            Assert.True(invites[1].IsMultiuse);
            Assert.Null(invites[1].Email);
            Assert.Null(invites[1].ExpiryDate);
            Assert.Equal(300, invites[1].InvitedAs);
            Assert.True(invites[1].NotifyReferrerOnJoin);
            List<InviteObject> copy = RoundTrip(invites);
            Assert.Equal("https://example.com/join/secret", copy[1].LinkUrl);
            Assert.Null(copy[1].ExpiryDate);
            Assert.Equal("pending", copy[0].AdditionalProperties["future_invite"].GetProperty("delivery").GetString());
        }

        /// <summary>Verifies group membership metadata and all modern permission settings survive round trips.</summary>
        [Fact]
        public void UserGroup_RetainsPermissionsAndGovernance()
        {
            UserGroupObject group = Read<UserGroupObject>("""
                {
                  "id": 20, "name": "Team", "description": "Team members", "members": [10, 11],
                  "direct_subgroup_ids": [21], "date_created": null, "creator_id": null,
                  "is_system_group": false, "deactivated": true,
                  "can_add_members_group": 22, "can_join_group": 23, "can_leave_group": 24,
                  "can_manage_group": {"direct_members": [10], "direct_subgroups": [],
                                       "future_permission": {"approval": true}},
                  "can_mention_group": 25, "can_remove_members_group": 26,
                  "future_group": {"label": "archived"}
                }
                """);
            Assert.Equal<int>([10, 11], group.Members!);
            Assert.Equal<int>([21], group.DirectSubgroupIds!);
            Assert.Null(group.DateCreated);
            Assert.Null(group.CreatorId);
            Assert.False(group.IsSystemGroup);
            Assert.True(group.Deactivated);
            Assert.Equal(22, group.CanAddMembersGroup!.GroupId);
            Assert.Equal(23, group.CanJoinGroup!.GroupId);
            Assert.Equal(24, group.CanLeaveGroup!.GroupId);
            Assert.Equal<int>([10], group.CanManageGroup!.DirectMembers!);
            Assert.Empty(group.CanManageGroup.DirectSubgroups!);
            Assert.Equal(25, group.CanMentionGroup!.GroupId);
            Assert.Equal(26, group.CanRemoveMembersGroup!.GroupId);
            UserGroupObject copy = RoundTrip(group);
            Assert.True(copy.CanManageGroup!.AdditionalProperties["future_permission"].GetProperty("approval").GetBoolean());
            Assert.Equal("archived", copy.AdditionalProperties["future_group"].GetProperty("label").GetString());
        }

        /// <summary>Verifies named group IDs retain the scalar wire representation.</summary>
        [Fact]
        public void GroupSetting_NamedIdRoundTrips()
        {
            GroupSettingValue value = Read<GroupSettingValue>("23");
            Assert.Equal(23, value.GroupId);
            Assert.Null(value.DirectMembers);
            Assert.Null(value.DirectSubgroups);
            Assert.Equal("23", JsonSerializer.Serialize(value));
            Assert.Equal(23, RoundTrip(value).GroupId);
            Assert.Equal("23", JsonSerializer.Serialize(new GroupSettingValue(23)));
        }

        /// <summary>Verifies anonymous group arrays and owned future data survive disposed JSON documents.</summary>
        [Fact]
        public void GroupSetting_AnonymousRoundTripsOwnedUnknownFields()
        {
            GroupSettingValue value;
            using (JsonDocument document = JsonDocument.Parse("""
                {"direct_members": [10, 11], "direct_subgroups": [20],
                 "future_setting": {"label": "权限", "values": [true, null, 3]}}
                """))
            {
                value = Assert.IsType<GroupSettingValue>(document.RootElement.Deserialize<GroupSettingValue>());
            }
            Assert.Null(value.GroupId);
            Assert.Equal<int>([10, 11], value.DirectMembers!);
            Assert.Equal<int>([20], value.DirectSubgroups!);
            Assert.Equal("权限", value.AdditionalProperties["future_setting"].GetProperty("label").GetString());
            string json = JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            using JsonDocument serialized = JsonDocument.Parse(json);
            Assert.True(serialized.RootElement.TryGetProperty("direct_members", out JsonElement members));
            Assert.Equal(2, members.GetArrayLength());
            Assert.False(serialized.RootElement.TryGetProperty("groupId", out JsonElement unused));
            GroupSettingValue copy = Read<GroupSettingValue>(json);
            Assert.Equal<int>([10, 11], copy.DirectMembers!);
            Assert.Equal<int>([20], copy.DirectSubgroups!);
            Assert.Equal(JsonValueKind.Null, copy.AdditionalProperties["future_setting"].GetProperty("values")[1].ValueKind);

            GroupSettingValue empty = RoundTrip(new GroupSettingValue([], []));
            Assert.Empty(empty.DirectMembers!);
            Assert.Empty(empty.DirectSubgroups!);
        }

        /// <summary>Verifies unsupported union variants and malformed anonymous arrays are explicit JSON failures.</summary>
        /// <param name="json">An invalid group setting JSON value.</param>
        [Theory]
        [InlineData("\"23\"")]
        [InlineData("null")]
        [InlineData("true")]
        [InlineData("[]")]
        [InlineData("1.5")]
        [InlineData("2147483648")]
        [InlineData("{}")]
        [InlineData("{\"direct_members\": []}")]
        [InlineData("{\"direct_subgroups\": []}")]
        [InlineData("{\"direct_members\": null, \"direct_subgroups\": []}")]
        [InlineData("{\"direct_members\": [], \"direct_subgroups\": {}}")]
        [InlineData("{\"direct_members\": [\"10\"], \"direct_subgroups\": []}")]
        [InlineData("{\"direct_members\": [null], \"direct_subgroups\": []}")]
        [InlineData("{\"direct_members\": [1.5], \"direct_subgroups\": []}")]
        [InlineData("{\"direct_members\": [], \"direct_subgroups\": [true]}")]
        [InlineData("{\"direct_members\": [], \"direct_subgroups\": [{}]}")]
        [InlineData("{\"direct_members\": [], \"direct_subgroups\": [[]]}")]
        public void GroupSetting_RejectsInvalidVariants(string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<GroupSettingValue>(json));
        }

        /// <summary>Verifies nullable CLR permissions allow omission but do not permit explicit JSON null.</summary>
        [Fact]
        public void PermissionFields_RejectExplicitNullWithoutBreakingOmittedFields()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StreamObject>("{\"can_send_message_group\": null}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SubscriptionObject>("{\"can_administer_channel_group\": null}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UserGroupObject>("{\"can_manage_group\": null}"));
            Assert.Null(RoundTrip(new StreamObject { StreamId = 7 }).CanSendMessageGroup);
            Assert.Null(RoundTrip(new SubscriptionObject { StreamId = 7 }).CanAdministerChannelGroup);
            Assert.Null(RoundTrip(new UserGroupObject { Id = 20 }).CanManageGroup);
        }

        /// <summary>Verifies caller-created legacy models keep their established property types and wire names.</summary>
        [Fact]
        public void LegacyModels_RemainSourceAndSerializationCompatible()
        {
            MessageObject message = RoundTrip(new MessageObject
            {
                Id = 42UL, SenderId = 10, Content = "<p>Hello</p>", RecipientId = 7, Timestamp = 1700000000L,
                Subject = "topic", Type = "stream", SenderEmail = "sender@example.com", SenderFullName = "Sender",
                SenderRealmStr = "example", DisplayRecipient = "general", StreamId = 7, Client = "test",
                ContentType = "text/html", Flags = ["read"], AvatarUrl = "https://example.com/avatar", IsMeMessage = false
            });
            Assert.Equal(42UL, message.Id);
            Assert.Equal("general", Assert.IsType<JsonElement>(message.DisplayRecipient).GetString());
            Assert.Equal("sender@example.com", message.SenderEmail);
            Assert.Equal("Sender", message.SenderFullName);
            Assert.Equal("example", message.SenderRealmStr);
            Assert.Equal("test", message.Client);
            Assert.Equal("https://example.com/avatar", message.AvatarUrl);
            Assert.Null(message.LastEditTimestamp);
            Assert.Null(message.LastMovedTimestamp);
            Assert.Null(message.EditHistory);

            SubscriptionObject subscription = RoundTrip(new SubscriptionObject
            {
                StreamId = 7, Name = "general", Description = "Team", RenderedDescription = "<p>Team</p>",
                InviteOnly = true, Color = "#abcdef", PinToTop = false, DesktopNotifications = true,
                AudibleNotifications = false, PushNotifications = null, EmailNotifications = true,
                IsMuted = true, EmailAddress = "channel@example.com"
            });
            Assert.Equal(7, subscription.StreamId);
            Assert.Equal("general", subscription.Name);
            Assert.Equal("Team", subscription.Description);
            Assert.Equal("<p>Team</p>", subscription.RenderedDescription);
            Assert.True(subscription.InviteOnly);
            Assert.Null(subscription.PushNotifications);
            Assert.True(subscription.EmailNotifications);
            Assert.Equal("channel@example.com", subscription.EmailAddress);
            Assert.Null(subscription.CanSendMessageGroup);

            UserGroupObject group = RoundTrip(new UserGroupObject { Id = 20, Name = "Team", Description = "Members", Members = [10] });
            Assert.Equal<int>([10], group.Members!);
            Assert.Null(group.CanManageGroup);
            UserObject user = RoundTrip(new UserObject { UserId = 10, Email = "user@example.com", FullName = "User", Role = 400, DateJoined = "2023-11-14T22:13:20Z" });
            Assert.Equal(400, user.Role);
            Assert.Equal("2023-11-14T22:13:20Z", user.DateJoined);
            Assert.Null(user.IsImportedStub);
            Assert.Null(user.IsDeleted);
            InviteObject invite = RoundTrip(new InviteObject { Id = 1, InvitedByUserId = 10, Email = "user@example.com", IsMultiuse = false });
            Assert.False(invite.IsMultiuse);
            Assert.Equal("user@example.com", invite.Email);
            Assert.Null(invite.ExpiryDate);
        }

        /// <summary>Deserializes a populated fixture and verifies its model type.</summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="json">The JSON fixture.</param>
        /// <returns>The deserialized model.</returns>
        private static T Read<T>(string json) where T : class => Assert.IsType<T>(JsonSerializer.Deserialize<T>(json));

        /// <summary>Serializes and reads a model using its public wire contract.</summary>
        /// <typeparam name="T">The model type.</typeparam>
        /// <param name="value">The populated model.</param>
        /// <returns>The model after a JSON round trip.</returns>
        private static T RoundTrip<T>(T value) where T : class => Read<T>(JsonSerializer.Serialize(value));
    }
}
