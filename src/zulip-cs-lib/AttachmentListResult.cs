using System.Collections.Generic;
using zulip_cs_lib.Models;

namespace zulip_cs_lib
{
    /// <summary>Attachment metadata, organization-wide storage usage and request-local diagnostics.</summary>
    public sealed class AttachmentListResult
    {
        /// <summary>Initializes an attachment-list result.</summary>
        /// <param name="attachments">The requesting user's attachments.</param>
        /// <param name="uploadSpaceUsed">Organization-wide uploaded bytes.</param>
        /// <param name="response">The completed request.</param>
        internal AttachmentListResult(List<AttachmentObject> attachments, long uploadSpaceUsed, ZulipResponse response)
        {
            Attachments = attachments.AsReadOnly();
            UploadSpaceUsed = uploadSpaceUsed;
            Response = response;
        }

        /// <summary>Gets the requesting user's attachments, including an empty list.</summary>
        public IReadOnlyList<AttachmentObject> Attachments { get; }

        /// <summary>Gets the total uploaded bytes for the organization, not just this user.</summary>
        public long UploadSpaceUsed { get; }

        /// <summary>Gets this request's response, including warnings and unknown fields.</summary>
        public ZulipResponse Response { get; }
    }
}
