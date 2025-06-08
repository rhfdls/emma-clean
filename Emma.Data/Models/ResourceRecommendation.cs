using System.ComponentModel.DataAnnotations;

namespace Emma.Data.Models;

public class ResourceRecommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
    
    public Guid RecommendedByAgentId { get; set; }
    public Agent RecommendedByAgent { get; set; } = null!;
    
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    public Guid? InteractionId { get; set; }
    public Interaction? Interaction { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;
    
    public int RecommendationOrder { get; set; } = 1; // 1st, 2nd, 3rd choice
    
    [MaxLength(1000)]
    public string? RecommendationNotes { get; set; }
    
    public DateTime RecommendedAt { get; set; } = DateTime.UtcNow;
    
    public bool WasSelected { get; set; } = false;
    public bool WasContacted { get; set; } = false;
    
    public DateTime? ContactedAt { get; set; }
    public DateTime? SelectedAt { get; set; }
    
    // If client chose someone else instead
    [MaxLength(200)]
    public string? AlternativeResourceName { get; set; }
    
    [MaxLength(500)]
    public string? AlternativeResourceContact { get; set; }
    
    [MaxLength(1000)]
    public string? WhyAlternativeChosen { get; set; }
    
    [MaxLength(1000)]
    public string? ClientFeedback { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, string>? CustomFields { get; set; }
}
