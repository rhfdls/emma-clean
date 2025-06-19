using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Represents a many-to-many relationship between SubscriptionPlan and Feature
/// to define which features are included in each subscription plan.
/// </summary>
public class SubscriptionPlanFeature : BaseEntity
{
    /// <summary>
    /// Foreign key to the associated SubscriptionPlan
    /// </summary>
    public Guid SubscriptionPlanId { get; set; }
    
    /// <summary>
    /// Navigation property to the associated SubscriptionPlan
    /// </summary>
    [ForeignKey(nameof(SubscriptionPlanId))]
    [InverseProperty(nameof(Models.SubscriptionPlan.Features))]
    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
    
    /// <summary>
    /// Foreign key to the associated Feature
    /// </summary>
    public Guid FeatureId { get; set; }
    
    /// <summary>
    /// Navigation property to the associated Feature
    /// </summary>
    [ForeignKey(nameof(FeatureId))]
    [InverseProperty(nameof(Models.Feature.SubscriptionPlans))]
    public virtual Feature? Feature { get; set; }
    
    /// <summary>
    /// When this feature was added to the plan
    /// </summary>
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Whether this feature is enabled for this plan
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Optional limit for this feature on this plan (if applicable)
    /// </summary>
    public int? Limit { get; set; }
    
    /// <summary>
    /// Optional notes about this feature for this plan
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
