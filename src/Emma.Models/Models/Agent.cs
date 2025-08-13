using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AgentType { get; set; } = string.Empty;  // e.g., "NBA", "Scheduler", "EmmaBot"
    
    [MaxLength(4000)]
    public string? Configuration { get; set; }  // JSON configuration for the agent
    
    public bool IsActive { get; set; } = true;

    // SPRINT2: Added for orchestration/AI registry
    public string? Status { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public List<string> Capabilities { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
    public string? Version { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActive { get; set; }
    
    // Relationships
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    // Reference to the user who created this agent
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    
    // Agent's messages and interactions
    public List<Message> Messages { get; set; } = new();
    
    // Agent's subscription
    public Subscription? Subscription { get; set; }
    
    // Agent's contacts (if this agent is assigned to specific contacts)
    [NotMapped]
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    
    // Agent's interactions
    public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
    
    // Methods
    public void UpdateLastActive()
    {
        LastActive = DateTime.UtcNow;
    }
}
