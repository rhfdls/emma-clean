namespace Emma.Data.Models;

public class Feature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty; // e.g., "ASK_EMMA"
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; } = new();
}
