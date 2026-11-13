namespace zulip_cs_lib
{
    /// <summary>Organization data-export selections supported by feature level 449 and later.</summary>
    public enum OrganizationExportType
    {
        /// <summary>Only public organization data.</summary>
        Public,

        /// <summary>Public data and private data for users who consented.</summary>
        FullWithConsent,

        /// <summary>All public and private data; requires the owner_full_content_access server feature.</summary>
        FullWithoutConsent
    }
}
