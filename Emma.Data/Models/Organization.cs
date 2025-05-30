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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Conversation> Conversations { get; set; } = new();
    public List<Agent> Agents { get; set; } = new();
    public List<OrganizationSubscription> Subscriptions { get; set; } = new();
}
