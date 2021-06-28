using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zulip_cs_lib.Resources
{
    /// <summary>A narrow.</summary>
    public class Narrow
    {
        /// <summary>Gets the operator.</summary>
        private NarrowOperator _operator;

        /// <summary>The value.</summary>
        private string _value;

        /// <summary>The formatted.</summary>
        private string _formatted;

        /// <summary>Values that represent narrow operators.</summary>
        public enum NarrowOperator
        {
            /// <summary>No operator (e.g., a search term).</summary>
            None,

            /// <summary>Search all group private messages that include the specified users.</summary>
            GroupPrivateMessageWith,

            /// <summary>Search for messages with attachments.</summary>
            HasAttachment,

            /// <summary>Search for messages with links.</summary>
            HasLink,

            /// <summary>Search for messages with images.</summary>
            HasImage,

            /// <summary>Search for a specific message (e.g., get).</summary>
            Id,

            /// <summary>Search for messages with configured alert words.</summary>
            IsAlerted,

            /// <summary>Search for messages which mention the current user.</summary>
            IsMentioned,

            /// <summary>Search all private messages.</summary>
            IsPrivate,

            /// <summary>Search for messages starred by the current user.</summary>
            IsStarred,

            /// <summary>Search for unread messages.</summary>
            IsUnread,

            /// <summary>Search for messages near a specific message.</summary>
            Near,

            /// <summary>Search all public streams.</summary>
            PublicStreams,

            /// <summary>Search a specific stream.</summary>
            Stream,

            /// <summary>Search within a specific topic.</summary>
            Topic,
        };

        /// <summary>Initializes a new instance of the zulip_cs_lib.Resources.Narrow class.</summary>
        /// <param name="op">The operator.</param>
        public Narrow(NarrowOperator op)
        : this(op, null)
        {
        }

        /// <summary>Initializes a new instance of the zulip_cs_lib.Resources.Narrow class.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="op">   The operator.</param>
        /// <param name="value">The value.</param>
        public Narrow(NarrowOperator op, string value)
        {
            _operator = op;
            _value = value;

            switch (_operator)
            {
                case NarrowOperator.GroupPrivateMessageWith:
                    _formatted = "group-pm-with:" + _value;
                    break;

                case NarrowOperator.HasAttachment:
                    _formatted = "has:attachment";
                    break;

                case NarrowOperator.HasLink:
                    _formatted = "has:link";
                    break;

                case NarrowOperator.HasImage:
                    _formatted = "has:image";
                    break;

                case NarrowOperator.Id:
                    _formatted = "id:" + _value;
                    break;

                case NarrowOperator.IsAlerted:
                    _formatted = "is:alerted";
                    break;

                case NarrowOperator.IsMentioned:
                    _formatted = "is:mentioned";
                    break;

                case NarrowOperator.IsPrivate:
                    _formatted = "is:private";
                    break;

                case NarrowOperator.IsStarred:
                    _formatted = "is:starred";
                    break;

                case NarrowOperator.IsUnread:
                    _formatted = "is:unread";
                    break;

                case NarrowOperator.Near:
                    _formatted = "near:" + _value;
                    break;

                case NarrowOperator.PublicStreams:
                    _formatted = "streams:public";
                    break;

                case NarrowOperator.Stream:
                    _formatted = "stream:" + _value;
                    break;

                case NarrowOperator.Topic:
                    _formatted = "topic:" + _value.Replace(' ', '+');
                    break;

                case NarrowOperator.None:
                default:
                    _formatted = _value;
                    break;
            }
        }

        /// <summary>Gets the formatted narrow value.</summary>
        public string Formatted => _formatted;
    }
}
