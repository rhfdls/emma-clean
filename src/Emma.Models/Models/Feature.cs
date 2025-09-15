using System.ComponentModel.DataAnnotations;

namespace Emma.Models.Models;

/// <summary>
/// Represents a feature that can be included in subscription plans.
/// Features control access to specific functionality in the application.
/// </summary>
public class Feature : BaseEntity
{
    
    /// <summary>
    /// Unique code for the feature (e.g., "ASK_EMMA", "CUSTOM_DOMAINS")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// User-friendly display name for the feature
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the feature
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Category for grouping related features (e.g., "AI Features", "Security")
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }
    
    /// <summary>
    /// Whether this feature is currently active and available
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Navigation property for the subscription plans that include this feature
    /// </summary>
    public virtual ICollection<SubscriptionPlanFeature> SubscriptionPlans { get; set; } = new List<SubscriptionPlanFeature>();
    
    // When this feature was last modified — timestamp is inherited from BaseEntity (UpdatedAt). Avoid redefining.
    
    /// <summary>
    /// Navigation property for the many-to-many relationship with SubscriptionPlan
    /// </summary>
    public virtual ICollection<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; set; } = new List<SubscriptionPlanFeature>();
}
