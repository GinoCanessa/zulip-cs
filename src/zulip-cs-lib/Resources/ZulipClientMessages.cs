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

        /// <summary>Sends a private message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="userEmails">  A variable-length parameters list containing user email addresses.</param>
        public async Task<ZulipResponse> SendPrivateMessage(string message, params string[] userEmails)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            string recipients = string.Join(", ", userEmails);

            data.Add("type", "private");
            data.Add("to", recipients);
            data.Add("content", message);

            return await PostAsync(_messageApiEndpoint, data);
        }

        /// <summary>Sends a private message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="userIds">  A variable-length parameters list containing user ids.</param>
        public async Task<ZulipResponse> SendPrivateMessage(string message, params int[] userIds)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            string recipients = "[";
            foreach (int user in userIds)
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

            data.Add("type", "private");
            data.Add("to", recipients);
            data.Add("content", message);

            return await PostAsync(_messageApiEndpoint, data);
        }
    }
}
