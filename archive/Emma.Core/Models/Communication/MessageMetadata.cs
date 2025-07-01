using System;
using System.Collections.Generic;

namespace Emma.Core.Models.Communication
{
    /// <summary>
    /// Base class for message metadata that can be extended by specific message types.
    /// </summary>
    public class MessageMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageMetadata"/> class.
        /// </summary>
        public MessageMetadata()
        {
            CustomFields = new Dictionary<string, object>();
            Tags = new List<string>();
        }

        /// <summary>
        /// Gets or sets the ID of the user who created the message.
        /// </summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who last updated the message.
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or sets the source of the message (e.g., "web", "mobile", "api").
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the client information (e.g., browser, device, app version).
        /// </summary>
        public string ClientInfo { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the sender.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent string of the sender.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the geographic location of the sender.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the language of the message.
        /// </summary>
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets the timezone of the sender.
        /// </summary>
        public string Timezone { get; set; } = "UTC";

        /// <summary>
        /// Gets or sets the campaign ID, if this message is part of a campaign.
        /// </summary>
        public string CampaignId { get; set; }

        /// <summary>
        /// Gets or sets the workflow ID, if this message is part of a workflow.
        /// </summary>
        public string WorkflowId { get; set; }

        /// <summary>
        /// Gets or sets the step ID in the workflow, if applicable.
        /// </summary>
        public string WorkflowStepId { get; set; }

        /// <summary>
        /// Gets or sets the template ID used to generate this message, if any.
        /// </summary>
        public string TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the template version used, if any.
        /// </summary>
        public string TemplateVersion { get; set; }

        /// <summary>
        /// Gets or sets the custom fields for the message.
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; }

        /// <summary>
        /// Gets or sets the tags for the message.
        /// </summary>
        public ICollection<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the priority of the message.
        /// </summary>
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        /// <summary>
        /// Gets or sets the sensitivity level of the message.
        /// </summary>
        public MessageSensitivity Sensitivity { get; set; } = MessageSensitivity.Normal;

        /// <summary>
        /// Gets or sets the expiration time of the message, if applicable.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the message was delivered.
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the message was read by the recipient.
        /// </summary>
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// Gets or sets the number of times the message has been viewed.
        /// </summary>
        public int ViewCount { get; set; }

        /// <summary>
        /// Gets or sets the ID of the message this is a reply to, if any.
        /// </summary>
        public Guid? InReplyToId { get; set; }

        /// <summary>
        /// Gets or sets the IDs of messages this message references.
        /// </summary>
        public ICollection<Guid> ReferenceIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Gets or sets the ID of the conversation thread this message belongs to.
        /// </summary>
        public string ThreadId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the message in an external system.
        /// </summary>
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the external system this message is associated with.
        /// </summary>
        public string ExternalSystem { get; set; }

        /// <summary>
        /// Gets or sets the version of the external system's API used.
        /// </summary>
        public string ExternalApiVersion { get; set; }

        /// <summary>
        /// Gets or sets the raw data from the external system.
        /// </summary>
        public string ExternalRawData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message is archived.
        /// </summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the message was archived.
        /// </summary>
        public DateTime? ArchivedAt { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who archived the message.
        /// </summary>
        public Guid? ArchivedBy { get; set; }

        /// <summary>
        /// Gets or sets the reason for archiving the message.
        /// </summary>
        public string ArchiveReason { get; set; }
    }


    /// <summary>
    /// Represents the priority of a message.
    /// </summary>
    public enum MessagePriority
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
        High,
        /// <summary>
        /// Urgent priority.
        /// </summary>
        Urgent
    }


    /// <summary>
    /// Represents the sensitivity level of a message.
    /// </summary>
    public enum MessageSensitivity
    {
        /// <summary>
        /// Normal sensitivity.
        /// </summary>
        Normal,

        /// <summary>
        /// Personal sensitivity.
        /// </summary>
        Personal,

        /// <summary>
        /// Private sensitivity.
        /// </summary>
        Private,
        /// <summary>
        /// Confidential sensitivity.
        /// </summary>
        Confidential
    }
}
