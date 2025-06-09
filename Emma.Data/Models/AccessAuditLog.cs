using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Data.Models;

/// <summary>
/// Audit log for tracking access to protected resources.
/// Provides comprehensive forensic trail for privacy compliance.
/// </summary>
[Table("AccessAuditLogs")]
public class AccessAuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The agent who requested access to the resource.
    /// </summary>
    [Required]
    public Guid RequestingAgentId { get; set; }

    /// <summary>
    /// The organization the requesting agent belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

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
    /// Navigation property to the requesting agent.
    /// </summary>
    [ForeignKey(nameof(RequestingAgentId))]
    public virtual Agent? RequestingAgent { get; set; }

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
