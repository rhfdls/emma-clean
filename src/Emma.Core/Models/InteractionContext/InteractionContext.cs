using System;
using System.Collections.Generic;

namespace Emma.Core.Models.InteractionContext
{
    /// <summary>
    /// Represents the complete context of an interaction between users and agents.
    /// </summary>
    public class InteractionContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for the interaction.
        /// </summary>
        public Guid InteractionId { get; set; }
        
        /// <summary>
        /// Gets or sets the current state of the interaction.
        /// </summary>
        public string State { get; set; } = "Initialized";
        
        /// <summary>
        /// Gets or sets the ID of the user who initiated the interaction.
        /// </summary>
        public string? UserId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the contact associated with this interaction.
        /// </summary>
        public Guid? ContactId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the organization this interaction belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }
        
        /// <summary>
        /// Gets or sets the type of interaction (e.g., "chat", "email", "call").
        /// </summary>
        public string InteractionType { get; set; } = "chat";
        
        /// <summary>
        /// Gets or sets the timestamp when the interaction was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Gets or sets the timestamp when the interaction was last updated.
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Gets or sets the collection of messages in this interaction.
        /// </summary>
        public ICollection<InteractionMessage> Messages { get; set; } = new List<InteractionMessage>();
        
        /// <summary>
        /// Gets or sets the metadata associated with this interaction.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the audit ID for tracking purposes.
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the reason for the last state change.
        /// </summary>
        public string? Reason { get; set; }
    }
}
