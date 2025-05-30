using Emma.Data.Enums;

namespace Emma.Data.Models;

public class OrganizationSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }
    public int SeatsLimit { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public List<AgentSubscriptionAssignment> AgentAssignments { get; set; } = new();
}
