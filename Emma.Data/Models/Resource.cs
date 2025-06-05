using System.ComponentModel.DataAnnotations;
using Emma.Data.Enums;

namespace Emma.Data.Models;

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
    
    // For collaborator resources (other agents)
    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }
    
    public Guid CreatedByAgentId { get; set; }
    public Agent CreatedByAgent { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public Dictionary<string, string>? CustomFields { get; set; }
}
