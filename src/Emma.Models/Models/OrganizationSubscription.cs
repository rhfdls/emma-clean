using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Emma.Models.Enums;

namespace Emma.Models.Models;

/// <summary>
/// Represents an organization's subscription to a specific plan.
/// </summary>
public class OrganizationSubscription : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the organization that owns this subscription.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Gets or sets the organization that owns this subscription.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    [InverseProperty(nameof(Models.Organization.Subscriptions))]
    public virtual Organization? Organization { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the subscription plan.
    /// </summary>
    [Required]
    public Guid SubscriptionPlanId { get; set; }
    
    /// <summary>
    /// Gets or sets the subscription plan.
    /// </summary>
    [ForeignKey(nameof(SubscriptionPlanId))]
    [InverseProperty(nameof(Models.SubscriptionPlan.OrganizationSubscriptions))]
    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of seats allowed by this subscription.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int SeatsLimit { get; set; }
    
    /// <summary>
    /// Gets or sets the Stripe subscription ID for billing purposes.
    /// </summary>
    [MaxLength(100)]
    public string? StripeSubscriptionId { get; set; }
    
    /// <summary>
    /// Gets or sets the start date of the subscription.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the end date of the subscription (if applicable).
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Gets or sets the status of the subscription.
    /// </summary>
    [Required]
    public SubscriptionStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of user assignments for this subscription.
    /// </summary>
    [InverseProperty(nameof(UserSubscriptionAssignment.OrganizationSubscription))]
    public virtual ICollection<UserSubscriptionAssignment> UserAssignments { get; set; } = new List<UserSubscriptionAssignment>();
    
    /// <summary>
    /// Gets or sets the collection of user subscriptions associated with this organization subscription.
    /// </summary>
    [InverseProperty(nameof(Models.Subscription.OrganizationSubscription))]
    public virtual ICollection<Subscription> UserSubscriptions { get; set; } = new List<Subscription>();
}
