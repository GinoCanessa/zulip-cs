using System.Collections.Generic;

namespace zulip_cs_lib
{
    /// <summary>Selected notification and display defaults for new organization members.</summary>
    /// <remarks>Omitted values remain unchanged. Profile, identity, language and timezone settings are not supported here.</remarks>
    public sealed class RealmUserSettingsDefaultsOptions
    {
        /// <summary>Gets or sets desktop notifications for direct messages and mentions.</summary>
        public bool? EnableDesktopNotifications { get; set; }

        /// <summary>Gets or sets audible desktop notifications for direct messages and mentions.</summary>
        public bool? EnableSounds { get; set; }

        /// <summary>Gets or sets offline email notifications for direct messages and mentions.</summary>
        public bool? EnableOfflineEmailNotifications { get; set; }

        /// <summary>Gets or sets offline push notifications for direct messages and mentions.</summary>
        public bool? EnableOfflinePushNotifications { get; set; }

        /// <summary>Gets or sets online push notifications for direct messages and mentions.</summary>
        public bool? EnableOnlinePushNotifications { get; set; }

        /// <summary>Gets or sets channel desktop notifications.</summary>
        public bool? EnableStreamDesktopNotifications { get; set; }

        /// <summary>Gets or sets channel email notifications.</summary>
        public bool? EnableStreamEmailNotifications { get; set; }

        /// <summary>Gets or sets channel push notifications.</summary>
        public bool? EnableStreamPushNotifications { get; set; }

        /// <summary>Gets or sets channel audible desktop notifications.</summary>
        public bool? EnableStreamAudibleNotifications { get; set; }

        /// <summary>Gets or sets digest emails while a user is away.</summary>
        public bool? EnableDigestEmails { get; set; }

        /// <summary>Gets or sets 24-hour time display (feature level 99).</summary>
        public bool? TwentyFourHourTime { get; set; }

        /// <summary>Gets or sets whether Enter sends a message or saves an edit.</summary>
        public bool? EnterSends { get; set; }

        /// <summary>Converts explicitly selected defaults to their documented form fields.</summary>
        /// <returns>The form fields, without omitted values.</returns>
        internal Dictionary<string, string> ToForm()
        {
            Dictionary<string, string> data = [];
            Add("enable_desktop_notifications", EnableDesktopNotifications);
            Add("enable_sounds", EnableSounds);
            Add("enable_offline_email_notifications", EnableOfflineEmailNotifications);
            Add("enable_offline_push_notifications", EnableOfflinePushNotifications);
            Add("enable_online_push_notifications", EnableOnlinePushNotifications);
            Add("enable_stream_desktop_notifications", EnableStreamDesktopNotifications);
            Add("enable_stream_email_notifications", EnableStreamEmailNotifications);
            Add("enable_stream_push_notifications", EnableStreamPushNotifications);
            Add("enable_stream_audible_notifications", EnableStreamAudibleNotifications);
            Add("enable_digest_emails", EnableDigestEmails);
            Add("twenty_four_hour_time", TwentyFourHourTime);
            Add("enter_sends", EnterSends);
            return data;

            void Add(string name, bool? value)
            {
                if (value is bool selected) data.Add(name, selected ? "true" : "false");
            }
        }
    }
}
