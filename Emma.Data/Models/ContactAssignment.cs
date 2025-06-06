using System.ComponentModel.DataAnnotations;
using Emma.Data.Enums;

namespace Emma.Data.Models;

/// <summary>
/// Represents the assignment of a service provider contact to a client contact.
/// Replaces the ResourceAssignment model with a contact-centric approach.
/// </summary>
public class ContactAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The client contact receiving the service.
    /// </summary>
    public Guid ClientContactId { get; set; }
    public Contact ClientContact { get; set; } = null!;
    
    /// <summary>
    /// The service provider contact being assigned.
    /// </summary>
    public Guid ServiceContactId { get; set; }
    public Contact ServiceContact { get; set; } = null!;
    
    /// <summary>
    /// The agent who made the assignment.
    /// </summary>
    public Guid AssignedByAgentId { get; set; }
    public Agent AssignedByAgent { get; set; } = null!;
    
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    /// <summary>
    /// Purpose of the assignment (e.g., "Home inspection", "Loan pre-approval").
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;
    
    public ResourceAssignmentStatus Status { get; set; } = ResourceAssignmentStatus.Active;
    public Priority Priority { get; set; } = Priority.Normal;
    
    /// <summary>
    /// Optional interaction that triggered this assignment.
    /// </summary>
    public Guid? InteractionId { get; set; }
    public Interaction? Interaction { get; set; }
    
    /// <summary>
    /// Client's specific request or requirements.
    /// </summary>
    [MaxLength(1000)]
    public string? ClientRequest { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? FollowUpAt { get; set; }
    
    /// <summary>
    /// Whether the client actually used this service provider.
    /// </summary>
    public bool WasUsed { get; set; } = false;
    
    /// <summary>
    /// Client's rating of the service provider (1-5 stars).
    /// </summary>
    public decimal? ClientRating { get; set; }
    
    /// <summary>
    /// Client's feedback about the service provider.
    /// </summary>
    [MaxLength(1000)]
    public string? ClientFeedback { get; set; }
    
    /// <summary>
    /// Notes about the outcome of the assignment.
    /// </summary>
    [MaxLength(1000)]
    public string? OutcomeNotes { get; set; }
    
    /// <summary>
    /// Internal notes for the agent/team.
    /// </summary>
    [MaxLength(1000)]
    public string? InternalNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, string>? CustomFields { get; set; }
}
