namespace Emma.Data.Models;

public class Organization
{
    public Guid OwnerAgentId { get; set; }
    public Agent? OwnerAgent { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string FubApiKey { get; set; } = string.Empty;
    public string FubSystem { get; set; } = string.Empty;
    public string FubSystemKey { get; set; } = string.Empty;
    public int? FubId { get; set; }
    
    /// <summary>
    /// Industry code for this organization (e.g., "RealEstate", "Mortgage", "Financial")
    /// Determines which industry-specific EMMA profile to use
    /// </summary>
    public string? IndustryCode { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Interaction> Interactions { get; set; } = new();
    public List<Agent> Agents { get; set; } = new();
    public List<OrganizationSubscription> Subscriptions { get; set; } = new();
}
