using System.Text.Json.Serialization;
using Emma.Data.Enums;

namespace Emma.Data.Models;

public class Subscription
{
    [JsonIgnore] public Guid AgentId { get; set; }
    [JsonIgnore] public Agent? Agent { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public Guid? PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public int SeatsLimit { get; set; } = 1;
    public bool IsCallProcessingEnabled { get; set; } = true;
}
