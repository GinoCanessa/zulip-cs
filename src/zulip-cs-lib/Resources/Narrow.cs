using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>A narrow filter for Zulip message queries.</summary>
    public class Narrow
    {
        /// <summary>The operator.</summary>
        private NarrowOperator _operator;

        /// <summary>The operand value.</summary>
        private string _operand;

        /// <summary>Whether this narrow is negated.</summary>
        private bool _negated;

        /// <summary>Values that represent narrow operators.</summary>
        public enum NarrowOperator
        {
            /// <summary>Search a specific channel.</summary>
            Channel,

            /// <summary>Search all public channels.</summary>
            Channels,

            /// <summary>Search within a specific topic.</summary>
            Topic,

            /// <summary>Search direct messages with specified users.</summary>
            Dm,

            /// <summary>Search all group DMs that include the specified user.</summary>
            DmIncluding,

            /// <summary>Search messages sent by a specific user.</summary>
            Sender,

            /// <summary>Full-text search.</summary>
            Search,

            /// <summary>Search for messages near a specific message.</summary>
            Near,

            /// <summary>Search for a specific message (e.g., get).</summary>
            Id,

            /// <summary>Search for messages with attachments.</summary>
            HasAttachment,

            /// <summary>Search for messages with links.</summary>
            HasLink,

            /// <summary>Search for messages with images.</summary>
            HasImage,

            /// <summary>Search for messages with reactions.</summary>
            HasReaction,

            /// <summary>Search for messages with configured alert words.</summary>
            IsAlerted,

            /// <summary>Search for messages which mention the current user.</summary>
            IsMentioned,

            /// <summary>Search all direct messages.</summary>
            IsDm,

            /// <summary>Search for messages starred by the current user.</summary>
            IsStarred,

            /// <summary>Search for unread messages.</summary>
            IsUnread,

            /// <summary>Search for followed topics.</summary>
            IsFollowed,

            /// <summary>Search for muted topics.</summary>
            IsMuted,

            /// <summary>Search with conversation context.</summary>
            With,

            /// <summary>Search for mentions.</summary>
            Mentions,
        }

        /// <summary>Initializes a new instance of the Narrow class.</summary>
        /// <param name="op">The operator.</param>
        /// <param name="operand">(Optional) The operand value.</param>
        /// <param name="negated">(Optional) Whether this narrow is negated.</param>
        public Narrow(NarrowOperator op, string operand = null, bool negated = false)
        {
            _operator = op;
            _operand = operand;
            _negated = negated;
        }

        /// <summary>Gets the operator string for the API.</summary>
        /// <returns>The operator string.</returns>
        private string GetOperatorString()
        {
            switch (_operator)
            {
                case NarrowOperator.Channel: return "channel";
                case NarrowOperator.Channels: return "channels";
                case NarrowOperator.Topic: return "topic";
                case NarrowOperator.Dm: return "dm";
                case NarrowOperator.DmIncluding: return "dm-including";
                case NarrowOperator.Sender: return "sender";
                case NarrowOperator.Search: return "search";
                case NarrowOperator.Near: return "near";
                case NarrowOperator.Id: return "id";
                case NarrowOperator.HasAttachment: return "has";
                case NarrowOperator.HasLink: return "has";
                case NarrowOperator.HasImage: return "has";
                case NarrowOperator.HasReaction: return "has";
                case NarrowOperator.IsAlerted: return "is";
                case NarrowOperator.IsMentioned: return "is";
                case NarrowOperator.IsDm: return "is";
                case NarrowOperator.IsStarred: return "is";
                case NarrowOperator.IsUnread: return "is";
                case NarrowOperator.IsFollowed: return "is";
                case NarrowOperator.IsMuted: return "is";
                case NarrowOperator.With: return "with";
                case NarrowOperator.Mentions: return "mentions";
                default: return _operator.ToString().ToLowerInvariant();
            }
        }

        /// <summary>Gets the operand string for the API.</summary>
        /// <returns>The operand string.</returns>
        private string GetOperandString()
        {
            switch (_operator)
            {
                case NarrowOperator.HasAttachment: return "attachment";
                case NarrowOperator.HasLink: return "link";
                case NarrowOperator.HasImage: return "image";
                case NarrowOperator.HasReaction: return "reaction";
                case NarrowOperator.IsAlerted: return "alerted";
                case NarrowOperator.IsMentioned: return "mentioned";
                case NarrowOperator.IsDm: return "dm";
                case NarrowOperator.IsStarred: return "starred";
                case NarrowOperator.IsUnread: return "unread";
                case NarrowOperator.IsFollowed: return "followed";
                case NarrowOperator.IsMuted: return "muted";
                case NarrowOperator.Channels: return "public";
                default: return _operand ?? string.Empty;
            }
        }

        /// <summary>Converts this narrow to a JSON object string.</summary>
        /// <returns>A JSON string representing this narrow filter.</returns>
        public string ToJson()
        {
            var obj = new Dictionary<string, object>
            {
                { "operator", GetOperatorString() },
                { "operand", GetOperandString() },
                { "negated", _negated }
            };

            return JsonSerializer.Serialize(obj);
        }

        /// <summary>Converts an array of narrows to a JSON array string.</summary>
        /// <param name="narrows">The narrow filters.</param>
        /// <returns>A JSON array string.</returns>
        public static string ToJsonArray(params Narrow[] narrows)
        {
            if (narrows == null || narrows.Length == 0)
            {
                return "[]";
            }

            var items = new List<Dictionary<string, object>>();

            foreach (var narrow in narrows)
            {
                items.Add(new Dictionary<string, object>
                {
                    { "operator", narrow.GetOperatorString() },
                    { "operand", narrow.GetOperandString() },
                    { "negated", narrow._negated }
                });
            }

            return JsonSerializer.Serialize(items);
        }
    }
}
