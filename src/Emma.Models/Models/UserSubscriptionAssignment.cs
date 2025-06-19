using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Represents the assignment of a subscription to a user within an organization.
/// </summary>
public class UserSubscriptionAssignment : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the user who is assigned this subscription.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the user who is assigned this subscription.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(Models.User.SubscriptionAssignments))]
    public virtual User? User { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the organization subscription.
    /// </summary>
    [Required]
    public Guid OrganizationSubscriptionId { get; set; }
    
    /// <summary>
    /// Gets or sets the organization subscription.
    /// </summary>
    [ForeignKey(nameof(OrganizationSubscriptionId))]
    [InverseProperty(nameof(Models.OrganizationSubscription.UserAssignments))]
    public virtual OrganizationSubscription? OrganizationSubscription { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the subscription was assigned to the user.
    /// </summary>
    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the ID of the user who assigned this subscription.
    /// </summary>
    public Guid? AssignedByUserId { get; set; }
    
    /// <summary>
    /// Gets or sets the user who assigned this subscription.
    /// </summary>
    [ForeignKey(nameof(AssignedByUserId))]
    [InverseProperty(nameof(Models.User.AssignedSubscriptions))]
    public virtual User? AssignedByUser { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the subscription this assignment is for.
    /// </summary>
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// Gets or sets the subscription this assignment is for.
    /// </summary>
    [ForeignKey(nameof(SubscriptionId))]
    [InverseProperty(nameof(Models.Subscription.UserAssignments))]
    public virtual Subscription? Subscription { get; set; }
    
    /// <summary>
    /// Gets or sets the start date of the subscription for this user (if different from organization subscription).
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Gets or sets the end date of the subscription for this user (if different from organization subscription).
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this assignment is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the date and time when this assignment was deactivated.
    /// </summary>
    public DateTime? DeactivatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the reason for deactivating this assignment.
    /// </summary>
    [MaxLength(500)]
    public string? DeactivationReason { get; set; }
}
