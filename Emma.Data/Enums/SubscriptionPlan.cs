namespace Emma.Data.Models;

public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; } = new();
}
