namespace Emma.Data.Models;

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FubApiKey { get; set; }
    public int? FubUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = false;
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public List<Message> Messages { get; set; } = new();
    public Subscription? Subscription { get; set; }
    public AgentPhoneNumber? PhoneNumber { get; init; }
    public List<AgentSubscriptionAssignment> SubscriptionAssignments { get; set; } = new();
}
