using System.ComponentModel.DataAnnotations;

namespace Emma.Data.Models;

/// <summary>
/// Defines collaboration access between team members and client contacts.
/// Enables team members to access business interactions while respecting privacy.
/// </summary>
public enum CollaboratorRole
{
    BackupAgent,        // Can handle all business interactions in primary agent's absence
    Specialist,         // Has expertise in specific area (luxury, commercial, etc.)
    Mentor,            // Senior agent mentoring junior agent
    Assistant,         // Administrative support with limited access
    TeamLead,          // Team leader with oversight access
    Observer           // Read-only access for training/oversight
}

public class ContactCollaborator
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The client contact that the collaborator has access to.
    /// </summary>
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    
    /// <summary>
    /// The agent who is collaborating on this contact.
    /// </summary>
    public Guid CollaboratorAgentId { get; set; }
    public Agent CollaboratorAgent { get; set; } = null!;
    
    /// <summary>
    /// The agent who granted this collaboration access.
    /// </summary>
    public Guid GrantedByAgentId { get; set; }
    public Agent GrantedByAgent { get; set; } = null!;
    
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    /// <summary>
    /// Role defining the type and scope of collaboration.
    /// </summary>
    public CollaboratorRole Role { get; set; } = CollaboratorRole.Assistant;
    
    /// <summary>
    /// Can access business interactions (CRM tagged interactions).
    /// </summary>
    public bool CanAccessBusinessInteractions { get; set; } = true;
    
    /// <summary>
    /// Can access personal interactions (PERSONAL tagged interactions).
    /// Usually false to respect privacy.
    /// </summary>
    public bool CanAccessPersonalInteractions { get; set; } = false;
    
    /// <summary>
    /// Can create new interactions on behalf of the primary agent.
    /// </summary>
    public bool CanCreateInteractions { get; set; } = false;
    
    /// <summary>
    /// Can modify existing interactions.
    /// </summary>
    public bool CanEditInteractions { get; set; } = false;
    
    /// <summary>
    /// Can assign resources to this contact.
    /// </summary>
    public bool CanAssignResources { get; set; } = false;
    
    /// <summary>
    /// Can view contact's financial/transaction details.
    /// </summary>
    public bool CanAccessFinancialData { get; set; } = false;
    
    /// <summary>
    /// Optional reason for granting collaboration access.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    /// <summary>
    /// When collaboration access expires (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether this collaboration is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Helper method to check if collaborator can access a specific interaction.
    /// </summary>
    public bool CanAccessInteraction(Interaction interaction)
    {
        if (!IsActive) return false;
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) return false;
        
        // Check interaction privacy tags
        var hasPersonalTag = interaction.Tags.Contains("PERSONAL", StringComparer.OrdinalIgnoreCase);
        var hasBusinessTag = interaction.Tags.Contains("CRM", StringComparer.OrdinalIgnoreCase);
        
        if (hasPersonalTag && !CanAccessPersonalInteractions) return false;
        if (hasBusinessTag && !CanAccessBusinessInteractions) return false;
        
        // If no specific tags, default to business interaction rules
        if (!hasPersonalTag && !hasBusinessTag && !CanAccessBusinessInteractions) return false;
        
        return true;
    }
}
