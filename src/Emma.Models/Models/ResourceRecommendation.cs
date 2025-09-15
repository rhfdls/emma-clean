using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Represents a recommendation of a service provider (Contact) to a client (Contact).
/// This replaces the older Resource/ResourceAssignment model with a Contact-centric approach.
/// </summary>
[System.Obsolete("ResourceRecommendation is obsolete. Use lightweight recommendation result objects based on Contact instead.")]
// OBSOLETE: ResourceRecommendation is no longer used. Use recommendation results based on Contact.
public class ResourceRecommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The client contact receiving the recommendation.
    /// </summary>
    public Guid ContactId { get; set; }
    [ForeignKey(nameof(ContactId))]
    public Contact ClientContact { get; set; } = null!;
    
    /// <summary>
    /// The service provider contact being recommended.
    /// </summary>
    public Guid ServiceContactId { get; set; }
    public Contact ServiceContact { get; set; } = null!;
    
    /// <summary>
    /// The user who made the recommendation.
    /// </summary>
    public Guid RecommendedByUserId { get; set; }
    public User RecommendedByUser { get; set; } = null!;
    
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    /// <summary>
    /// The interaction that triggered this recommendation, if any.
    /// </summary>
    public Guid? InteractionId { get; set; }
    public Interaction? Interaction { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;
    
    /// <summary>
    /// The order of this recommendation in the list (1st, 2nd, 3rd choice).
    /// </summary>
    public int RecommendationOrder { get; set; } = 1;
    
    [MaxLength(1000)]
    public string? RecommendationNotes { get; set; }
    
    public DateTime RecommendedAt { get; set; } = DateTime.UtcNow;
    
    public bool WasSelected { get; set; } = false;
    public bool WasContacted { get; set; } = false;
    
    public DateTime? ContactedAt { get; set; }
    public DateTime? SelectedAt { get; set; }
    
    // If client chose someone else instead
    [MaxLength(200)]
    public string? AlternativeProviderName { get; set; }
    
    [MaxLength(500)]
    public string? AlternativeProviderContact { get; set; }
    
    [MaxLength(1000)]
    public string? WhyAlternativeChosen { get; set; }
    
    [MaxLength(1000)]
    public string? ClientFeedback { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, string>? CustomFields { get; set; }
}
