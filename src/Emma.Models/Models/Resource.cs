using System.ComponentModel.DataAnnotations;
using Emma.Models.Enums;

namespace Emma.Models.Models;

/// <summary>
/// OBSOLETE: This entity is being phased out in favor of Contact-centric approach.
/// Service providers are now represented as Contact entities with RelationshipState.ServiceProvider.
/// Use ContactAssignment instead of ResourceAssignment for service provider assignments.
/// </summary>
[Obsolete("Use Contact with RelationshipState.ServiceProvider instead. This will be removed in a future version.")]
public class Resource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CategoryId { get; set; }
    public ResourceCategory Category { get; set; } = null!;
    
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? CompanyName { get; set; }
    
    [MaxLength(255)]
    public string? Email { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(500)]
    public string? Website { get; set; }
    
    public Address? Address { get; set; }
    
    [MaxLength(100)]
    public string? LicenseNumber { get; set; }
    
    public List<string> Specialties { get; set; } = new();
    public List<string> ServiceAreas { get; set; } = new();
    
    public ResourceRelationshipType RelationshipType { get; set; } = ResourceRelationshipType.Referral;
    
    public decimal? Rating { get; set; } // 1-5 stars
    public int ReviewCount { get; set; } = 0;
    
    public bool IsPreferred { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    // For collaborator resources (other users)
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public Dictionary<string, string>? CustomFields { get; set; }
}
