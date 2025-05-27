namespace Emma.Data.Models;

public class SubscriptionPlanFeature
{
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }
    public Guid FeatureId { get; set; }
    public Feature? Feature { get; set; }
}
