using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Audit log for tracking access to protected resources.
/// Provides comprehensive forensic trail for privacy compliance.
/// </summary>
[Table("AccessAuditLogs")]
public class AccessAuditLog : BaseEntity
{
    /// <summary>
    /// The user who performed the action.
    /// </summary>
    public Guid? UserId { get; set; }
    
    /// <summary>
    /// The user who requested access to the resource.
    /// </summary>
    [Required]
    public Guid RequestingUserId { get; set; }

    /// <summary>
    /// The organization the requesting agent belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The action performed (e.g., Create, Read, Update, Delete).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of entity being accessed (Contact, Interaction, etc.).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the entity being accessed.
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Type of resource being accessed (Contact, Interaction, etc.).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the resource being accessed.
    /// </summary>
    [Required]
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Contact ID if the resource is related to a specific contact.
    /// </summary>
    public Guid? ContactId { get; set; }

    /// <summary>
    /// Whether access was granted or denied.
    /// </summary>
    [Required]
    public bool AccessGranted { get; set; }
    
    /// <summary>
    /// Date and time when the action occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// IP address of the client that made the request.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent string of the client that made the request.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Navigation property for the user who performed the action.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    /// <summary>
    /// Reason for the access decision (e.g., "Owner", "Collaborator", "Denied - No Permission").
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Privacy tags associated with the accessed resource.
    /// JSON array of tags like ["PERSONAL", "PRIVATE", "CRM"].
    /// </summary>
    [MaxLength(1000)]
    public string PrivacyTags { get; set; } = "[]";

    /// <summary>
    /// IP address of the requesting client.
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string of the requesting client.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata about the access request.
    /// JSON object with request details.
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Timestamp when the access was attempted.
    /// </summary>
    [Required]
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the requesting user.
    /// </summary>
    [ForeignKey(nameof(RequestingUserId))]
    public virtual User? RequestingUser { get; set; }

    /// <summary>
    /// Navigation property to the organization.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    /// <summary>
    /// Navigation property to the contact (if applicable).
    /// </summary>
    [ForeignKey(nameof(ContactId))]
    public virtual Contact? Contact { get; set; }
}
