using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Emma.Models.Enums;

namespace Emma.Models.Models;

/// <summary>
/// Represents a user's subscription to a subscription plan.
/// </summary>
public class Subscription : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the user who owns this subscription.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the user who owns this subscription.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(Models.User.Subscriptions))]
    [JsonIgnore] 
    public virtual User? User { get; set; }
    /// <summary>
    /// Gets or sets the Stripe subscription ID for this subscription.
    /// </summary>
    [MaxLength(100)]
    public string? StripeSubscriptionId { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the subscription plan this subscription is for.
    /// </summary>
    [Required]
    public Guid PlanId { get; set; }
    
    /// <summary>
    /// Gets or sets the subscription plan this subscription is for.
    /// </summary>
    [ForeignKey(nameof(PlanId))]
    [InverseProperty(nameof(Models.SubscriptionPlan.Subscriptions))]
    public virtual SubscriptionPlan? Plan { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the subscription started.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the date and time when the subscription will end, if applicable.
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Gets or sets the status of the subscription.
    /// </summary>
    [Required]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    
    /// <summary>
    /// Gets or sets the maximum number of seats allowed for this subscription.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int SeatsLimit { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets a value indicating whether call processing is enabled for this subscription.
    /// </summary>
    public bool IsCallProcessingEnabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the ID of the organization subscription this user subscription is associated with.
    /// </summary>
    public Guid? OrganizationSubscriptionId { get; set; }
    
    /// <summary>
    /// Gets or sets the organization subscription this user subscription is associated with.
    /// </summary>
    [ForeignKey(nameof(OrganizationSubscriptionId))]
    [InverseProperty(nameof(Models.OrganizationSubscription.UserSubscriptions))]
    [JsonIgnore]
    public virtual OrganizationSubscription? OrganizationSubscription { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of user subscription assignments for this subscription.
    /// </summary>
    [InverseProperty(nameof(UserSubscriptionAssignment.Subscription))]
    [JsonIgnore]
    public virtual ICollection<UserSubscriptionAssignment> UserAssignments { get; set; } = new List<UserSubscriptionAssignment>();
}
