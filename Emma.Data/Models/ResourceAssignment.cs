using System.ComponentModel.DataAnnotations;
using Emma.Data.Enums;

namespace Emma.Data.Models;

/// <summary>
/// OBSOLETE: This entity is being phased out in favor of Contact-centric approach.
/// Use ContactAssignment instead, which links ClientContact to ServiceContact (both are Contact entities).
/// </summary>
[Obsolete("Use ContactAssignment instead. This will be removed in a future version.")]
public class ResourceAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
    
    public Guid AssignedByAgentId { get; set; }
    public Agent AssignedByAgent { get; set; } = null!;
    
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    [Required]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;
    
    public ResourceAssignmentStatus Status { get; set; } = ResourceAssignmentStatus.Active;
    public Priority Priority { get; set; } = Priority.Normal;
    
    public Guid? InteractionId { get; set; }
    public Interaction? Interaction { get; set; }
    
    [MaxLength(1000)]
    public string? ClientRequest { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? FollowUpAt { get; set; }
    
    public bool WasUsed { get; set; } = false;
    public decimal? ClientRating { get; set; } // 1-5 stars
    
    [MaxLength(1000)]
    public string? ClientFeedback { get; set; }
    
    [MaxLength(1000)]
    public string? OutcomeNotes { get; set; }
    
    [MaxLength(1000)]
    public string? InternalNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, string>? CustomFields { get; set; }
}
