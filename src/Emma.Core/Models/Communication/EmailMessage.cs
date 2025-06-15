using System;
using System.Collections.Generic;
using System.Linq;

namespace Emma.Core.Models.Communication
{
    /// <summary>
    /// Represents an email message.
    /// </summary>
    public class EmailMessage : Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailMessage"/> class.
        /// </summary>
        public EmailMessage()
        {
            MessageType = MessageType.Email;
            To = new List<string>();
            Cc = new List<string>();
            Bcc = new List<string>();
            Attachments = new List<EmailAttachment>();
        }

        /// <summary>
        /// Gets or sets the subject of the email.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the sender's email address.
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Gets or sets the list of recipient email addresses.
        /// </summary>
        public ICollection<string> To { get; set; }

        /// <summary>
        /// Gets or sets the list of CC recipient email addresses.
        /// </summary>
        public ICollection<string> Cc { get; set; }

        /// <summary>
        /// Gets or sets the list of BCC recipient email addresses.
        /// </summary>
        public ICollection<string> Bcc { get; set; }

        /// <summary>
        /// Gets or sets the list of email attachments.
        /// </summary>
        public ICollection<EmailAttachment> Attachments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the email content is in HTML format.
        /// </summary>
        public bool IsHtml { get; set; }

        /// <summary>
        /// Gets or sets the plain text version of the email content.
        /// </summary>
        public string PlainTextContent { get; set; }

        /// <summary>
        /// Gets or sets the HTML version of the email content.
        /// </summary>
        public string HtmlContent { get; set; }

        /// <summary>
        /// Gets or sets the ID of the email template used, if any.
        /// </summary>
        public string TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the template variables for the email, if using a template.
        /// </summary>
        public Dictionary<string, object> TemplateVariables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the email headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the reply-to email address.
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// Gets or sets the priority of the email.
        /// </summary>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;

        /// <summary>
        /// Gets or sets the tracking settings for the email.
        /// </summary>
        public EmailTracking Tracking { get; set; } = new EmailTracking();

        /// <summary>
        /// Gets or sets the ID of the email in the external email service, if applicable.
        /// </summary>
        public string ExternalEmailId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the email thread/conversation in the external email service.
        /// </summary>
        public string ThreadId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the email this message is in reply to.
        /// </summary>
        public string InReplyTo { get; set; }

        /// <summary>
        /// Gets or sets the message IDs this email references.
        /// </summary>
        public ICollection<string> References { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the categories or labels for the email.
        /// </summary>
        public ICollection<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the metadata specific to the email.
        /// </summary>
        public new EmailMetadata Metadata { get; set; } = new EmailMetadata();
    }

    /// <summary>
    /// Represents an email attachment.
    /// </summary>
    public class EmailAttachment
    {
        /// <summary>
        /// Gets or sets the name of the attachment file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the attachment.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the content ID of the attachment (used for inline images).
        /// </summary>
        public string ContentId { get; set; }

        /// <summary>
        /// Gets or sets the base64-encoded content of the attachment.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the URL of the attachment, if stored externally.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the size of the attachment in bytes.
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attachment is inline.
        /// </summary>
        public bool IsInline { get; set; }
    }

    /// <summary>
    /// Represents the priority of an email.
    /// </summary>
    public enum EmailPriority
    {
        /// <summary>
        /// Low priority.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority.
        /// </summary>
        High
    }

    /// <summary>
    /// Represents tracking settings for an email.
    /// </summary>
    public class EmailTracking
    {
        /// <summary>
        /// Gets or sets a value indicating whether to track opens.
        /// </summary>
        public bool TrackOpens { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to track clicks.
        /// </summary>
        public bool TrackClicks { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to track link clicks.
        /// </summary>
        public bool TrackLinkClicks { get; set; } = true;

        /// <summary>
        /// Gets or sets the UTM source parameter for tracking.
        /// </summary>
        public string UtmSource { get; set; }

        /// <summary>
        /// Gets or sets the UTM medium parameter for tracking.
        /// </summary>
        public string UtmMedium { get; set; }

        /// <summary>
        /// Gets or sets the UTM campaign parameter for tracking.
        /// </summary>
        public string UtmCampaign { get; set; }

        /// <summary>
        /// Gets or sets the UTM term parameter for tracking.
        /// </summary>
        public string UtmTerm { get; set; }

        /// <summary>
        /// Gets or sets the UTM content parameter for tracking.
        /// </summary>
        public string UtmContent { get; set; }
    }

    /// <summary>
    /// Represents metadata specific to an email message.
    /// </summary>
    public class EmailMetadata : MessageMetadata
    {
        /// <summary>
        /// Gets or sets the email headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the email authentication results.
        /// </summary>
        public EmailAuthenticationResults AuthenticationResults { get; set; } = new EmailAuthenticationResults();

        /// <summary>
        /// Gets or sets the spam score from spam filters.
        /// </summary>
        public float? SpamScore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the email passed SPF check.
        /// </summary>
        public bool? SpfPassed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the email passed DKIM check.
        /// </summary>
        public bool? DkimPassed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the email passed DMARC check.
        /// </summary>
        public bool? DmarcPassed { get; set; }
    }


    /// <summary>
    /// Represents email authentication results.
    /// </summary>
    public class EmailAuthenticationResults
    {
        /// <summary>
        /// Gets or sets the SPF authentication result.
        /// </summary>
        public string Spf { get; set; }

        /// <summary>
        /// Gets or sets the DKIM authentication result.
        /// </summary>
        public string Dkim { get; set; }

        /// <summary>
        /// Gets or sets the DMARC authentication result.
        /// </summary>
        public string Dmarc { get; set; }

        /// <summary>
        /// Gets or sets the composite authentication result.
        /// </summary>
        public string Composite { get; set; }
    }
}
