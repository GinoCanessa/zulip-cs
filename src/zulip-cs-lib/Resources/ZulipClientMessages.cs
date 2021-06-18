using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace zulip_cs_lib
{
    /// <content>A zulip client.</content>
    public partial class ZulipClient
    {
        /// <summary>The message API endpoint.</summary>
        private const string _messageApiEndpoint = "api/v1/messages";

        private enum ZulipMessageType
        {
            Private,
            Stream,
        }

        
        /// <summary>Sends a private message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="userEmails">  A variable-length parameters list containing user email addresses.</param>
        public async Task<ZulipResponse> SendPrivateMessage(string message, params string[] userEmails)
        {
            return await SendMessage(message, ZulipMessageType.Private, userEmails);
        }

        /// <summary>Sends a private message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="userIds">  A variable-length parameters list containing user ids.</param>
        public async Task<ZulipResponse> SendPrivateMessage(string message, params int[] userIds)
        {
            return await SendMessage(message, ZulipMessageType.Private, userIds);
        }

        /// <summary>Sends a stream message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="streamNames">  A variable-length parameters list containing destination stream names.</param>
        public async Task<ZulipResponse> SendStreamMessage(string message, params string[] streamNames)
        {
            return await SendMessage(message, ZulipMessageType.Stream, streamNames);
        }

        /// <summary>Sends a stream message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="streamIds">  A variable-length parameters list containing stream ids.</param>
        public async Task<ZulipResponse> SendStreamMessage(string message, params int[] streamIds)
        {
            return await SendMessage(message, ZulipMessageType.Stream, streamIds);
        }

        /// <summary>Sends a message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="type">The message type (private, stream).</param>
        /// <param name="stringIds">  A variable-length parameters list containing user email addresses or stream names.</param>
        private Task<ZulipResponse> SendMessage(string message, ZulipMessageType type, params string[] stringIds)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            string recipients = string.Join(", ", stringIds);

            switch (type)
            {
                case ZulipMessageType.Private:
                    data.Add("type", "private");
                    break;
                case ZulipMessageType.Stream:
                    data.Add("type", "stream");
                    break;
            }

            data.Add("to", recipients);
            data.Add("content", message);

            return PostAsync(_messageApiEndpoint, data);
        }

        /// <summary>Sends a message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="type">The message type (private, stream).</param>
        /// <param name="intIds">  A variable-length parameters list containing user or stream ids.</param>
        private async Task<ZulipResponse> SendMessage(string message, ZulipMessageType type, params int[] intIds)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            string recipients = "[";
            foreach (int user in intIds)
            {
                if (string.IsNullOrEmpty(recipients))
                {
                    recipients += user;
                }
                else
                {
                    recipients += ", " + user;
                }
            }

            recipients += "]";

            switch (type)
            {
                case ZulipMessageType.Private:
                    data.Add("type", "private");
                    break;
                case ZulipMessageType.Stream:
                    data.Add("type", "stream");
                    break;
            }

            data.Add("to", recipients);
            data.Add("content", message);

            return await PostAsync(_messageApiEndpoint, data);
        }
    }
}
