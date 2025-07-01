using System;
using System.Collections.Generic;

namespace Emma.Core.Models.Communication
{
    /// <summary>
    /// Represents the base class for all message types in the system.
    /// </summary>
    public abstract class Message
    {
        /// <summary>
        /// Gets or sets the unique identifier for the message.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the message was sent.
        /// </summary>
        public DateTime SentAt { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who sent the message.
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the recipient of the message.
        /// </summary>
        public Guid RecipientId { get; set; }

        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        public MessageType MessageType { get; protected set; }

        /// <summary>
        /// Gets or sets the status of the message.
        /// </summary>
        public MessageStatus Status { get; set; } = MessageStatus.Pending;

        /// <summary>
        /// Gets or sets the metadata associated with the message.
        /// </summary>
        public MessageMetadata Metadata { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the message was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the message was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the ID of the organization that owns the message.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with the message.
        /// </summary>
        public ICollection<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the optional conversation ID this message belongs to.
        /// </summary>
        public Guid? ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the optional parent message ID for threading.
        /// </summary>
        public Guid? ParentMessageId { get; set; }
    }

    /// <summary>
    /// Represents the type of a message.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Email message type.
        /// </summary>
        Email,

        /// <summary>
        /// SMS message type.
        /// </summary>
        Sms,

        /// <summary>
        /// Phone call message type.
        /// </summary>
        Call,


        /// <summary>
        /// In-app notification message type.
        /// </summary>
        Notification,

        /// <summary>
        /// Chat or instant message type.
        /// </summary>
        Chat,

        /// <summary>
        /// System-generated message type.
        /// </summary>
        System
    }

    /// <summary>
    /// Represents the status of a message.
    /// </summary>
    public enum MessageStatus
    {
        /// <summary>
        /// The message is pending processing.
        /// </summary>
        Pending,


        /// <summary>
        /// The message has been sent successfully.
        /// </summary>
        Sent,

        /// <summary>
        /// The message has been delivered to the recipient.
        /// </summary>
        Delivered,


        /// <summary>
        /// The message has been read by the recipient.
        /// </summary>
        Read,

        /// <summary>
        /// The message failed to send.
        /// </summary>
        Failed,


        /// <summary>
        /// The message has been scheduled for sending.
        /// </summary>
        Scheduled,

        /// <summary>
        /// The message is being processed.
        /// </summary>
        Processing,

        /// <summary>
        /// The message has been cancelled.
        /// </summary>
        Cancelled
    }
}
