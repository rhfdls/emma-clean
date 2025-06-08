namespace Emma.Data.Models;

public class AgentSubscriptionAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }
    public Guid OrganizationSubscriptionId { get; set; }
    public OrganizationSubscription? OrganizationSubscription { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
