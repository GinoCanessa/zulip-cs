using System.Collections.Generic;

namespace zulip_cs_lib
{
    /// <summary>Selected personal preferences; null values leave the corresponding settings unchanged.</summary>
    /// <remarks>Timezone and language are personal settings, not realm-level new-user defaults. Password and identity changes are excluded.</remarks>
    public sealed class UserSettingsOptions
    {
        /// <summary>Gets or sets the user's IANA profile time zone.</summary>
        public string? Timezone { get; set; }

        /// <summary>Gets or sets a language code supported by the server.</summary>
        public string? DefaultLanguage { get; set; }

        /// <summary>Gets or sets whether times use 24-hour notation.</summary>
        public bool? TwentyFourHourTime { get; set; }

        /// <summary>Gets or sets whether Enter sends a message or saves a message edit.</summary>
        public bool? EnterSends { get; set; }

        /// <summary>Gets or sets visual desktop notifications for direct messages and mentions.</summary>
        public bool? EnableDesktopNotifications { get; set; }

        /// <summary>Gets or sets offline email notifications for direct messages and mentions.</summary>
        public bool? EnableOfflineEmailNotifications { get; set; }

        /// <summary>Gets or sets offline push notifications for direct messages and mentions.</summary>
        public bool? EnableOfflinePushNotifications { get; set; }

        /// <summary>Gets or sets push notifications for direct messages and mentions while online.</summary>
        public bool? EnableOnlinePushNotifications { get; set; }

        /// <summary>Gets or sets visual desktop notifications for channel messages.</summary>
        public bool? EnableStreamDesktopNotifications { get; set; }

        /// <summary>Gets or sets email notifications for channel messages.</summary>
        public bool? EnableStreamEmailNotifications { get; set; }

        /// <summary>Gets or sets push notifications for channel messages.</summary>
        public bool? EnableStreamPushNotifications { get; set; }

        /// <summary>Gets or sets digest emails when the user is away.</summary>
        public bool? EnableDigestEmails { get; set; }

        /// <summary>Encodes supplied preferences as form fields, preserving explicit false values.</summary>
        /// <returns>The selected settings without omitted nullable values.</returns>
        internal Dictionary<string, string> ToForm()
        {
            Dictionary<string, string> data = [];
            if (Timezone != null) data.Add("timezone", Timezone);
            if (DefaultLanguage != null) data.Add("default_language", DefaultLanguage);
            AddBoolean(data, "twenty_four_hour_time", TwentyFourHourTime);
            AddBoolean(data, "enter_sends", EnterSends);
            AddBoolean(data, "enable_desktop_notifications", EnableDesktopNotifications);
            AddBoolean(data, "enable_offline_email_notifications", EnableOfflineEmailNotifications);
            AddBoolean(data, "enable_offline_push_notifications", EnableOfflinePushNotifications);
            AddBoolean(data, "enable_online_push_notifications", EnableOnlinePushNotifications);
            AddBoolean(data, "enable_stream_desktop_notifications", EnableStreamDesktopNotifications);
            AddBoolean(data, "enable_stream_email_notifications", EnableStreamEmailNotifications);
            AddBoolean(data, "enable_stream_push_notifications", EnableStreamPushNotifications);
            AddBoolean(data, "enable_digest_emails", EnableDigestEmails);
            return data;
        }

        /// <summary>Adds a Boolean setting only when explicitly supplied.</summary>
        /// <param name="data">The outgoing form.</param>
        /// <param name="key">The setting name.</param>
        /// <param name="value">The optional setting value.</param>
        private static void AddBoolean(Dictionary<string, string> data, string key, bool? value)
        {
            if (value.HasValue) data.Add(key, value.Value ? "true" : "false");
        }
    }
}
