using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Represents an interaction between users, contacts, and systems in the EMMA platform.
/// </summary>
public class Interaction : BaseEntity
{
    /// <summary>
    /// Gets or sets the user who created this interaction.
    /// </summary>
    [ForeignKey(nameof(CreatedById))]
    public virtual User? CreatedBy { get; set; }

    // Core Identifiers
    public Guid OrganizationId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ContactId { get; set; }
    public Guid? ParentInteractionId { get; set; }
    public string? ThreadId { get; set; }
    public int Version { get; set; } = 1;

    // Interaction Metadata
    public string Type { get; set; } = "other"; // Email, Call, Meeting, Sms, Chat, Form, WebVisit, Social, Document, System, Note, Task
    public string Direction { get; set; } = "inbound"; // inbound, outbound, system
    public string Status { get; set; } = "draft"; // draft, pending, inprogress, completed, failed, cancelled, snoozed
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public string? Summary { get; set; }
    
    // AI & Analytics
    public float[]? VectorEmbedding { get; set; }
    public float? SentimentScore { get; set; } // -1.0 to 1.0
    public string? SentimentLabel { get; set; } // VeryNegative, Negative, Neutral, Positive, VeryPositive
    [NotMapped]
    public Dictionary<string, object>? AiMetadata { get; set; }
    public List<ActionItem>? ActionItems { get; set; }
    
    // Privacy & Security
    public string PrivacyLevel { get; set; } = "internal"; // public, internal, private, confidential
    public string? Confidentiality { get; set; } // standard, sensitive, highly_sensitive
    public string? RetentionPolicy { get; set; }
    public bool IsRead { get; set; }
    public bool IsStarred { get; set; }
    public bool FollowUpRequired { get; set; }
    
    // Channel & Participants
    public string Channel { get; set; } = "other"; // email, phone, sms, whatsapp, web, mobileapp, inperson, mail, social, other
    [NotMapped]
    public Dictionary<string, object>? ChannelData { get; set; }
    public List<Participant> Participants { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
    
    // Related Entities
    public List<RelatedEntity> RelatedEntities { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    [NotMapped]
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    // Navigation properties
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    
    /// <summary>
    /// Gets or sets the collection of messages that are part of this interaction.
    /// </summary>
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    
    /// <summary>
    /// Gets or sets the contact associated with this interaction.
    /// </summary>
    [ForeignKey(nameof(ContactId))]
    public virtual Contact? Contact { get; set; }
    
    /// <summary>
    /// Gets or sets the parent interaction, if this is a reply or follow-up.
    /// </summary>
    [ForeignKey(nameof(ParentInteractionId))]
    public virtual Interaction? ParentInteraction { get; set; }
    
    /// <summary>
    /// Gets or sets the child interactions, if this is a parent interaction.
    /// </summary>
    public virtual ICollection<Interaction> ChildInteractions { get; set; } = new List<Interaction>();
    
    /// <summary>
    /// Gets or sets the organization this interaction belongs to.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }
    [NotMapped]
    public Dictionary<string, string> ExternalIds { get; set; } = new();
    [NotMapped]
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Timing
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime? FollowUpBy { get; set; }
    
    // Audit
    public Guid CreatedById { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    

}

/// <summary>
/// Represents a participant in an interaction
/// </summary>
[NotMapped]
public class Participant
{
    public Guid ContactId { get; set; }
    public string Role { get; set; } = "participant"; // sender, recipient, cc, bcc, etc.
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    [NotMapped]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Represents an attachment in an interaction
/// </summary>
public class Attachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Url { get; set; }
    public string? PreviewUrl { get; set; }
    [NotMapped]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Represents an action item extracted from an interaction
/// </summary>
public class ActionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, inprogress, completed, cancelled
    public string Type { get; set; } = "general"; // task, follow_up, information_request, appointment, etc.
    public string? Priority { get; set; } // low, normal, high, urgent
    public Guid? AssignedTo { get; set; }
    public DateTime? DueBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    [NotMapped]
    public Dictionary<string, object>? Metadata { get; set; }
}
