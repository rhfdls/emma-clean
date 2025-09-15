using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models
{
    /// <summary>
    /// Represents a subscription plan that can be assigned to organizations.
    /// </summary>
    public class SubscriptionPlan : BaseEntity
    {
        
        /// <summary>
        /// The name of the subscription plan (e.g., "Starter", "Professional", "Enterprise")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// The features included in this subscription plan
        /// </summary>
        [InverseProperty(nameof(SubscriptionPlanFeature.SubscriptionPlan))]
        public virtual ICollection<SubscriptionPlanFeature> Features { get; set; } = new List<SubscriptionPlanFeature>();
        
        // CreatedAt and UpdatedAt are inherited from BaseEntity. Avoid redefining to prevent member hiding.
        
        /// <summary>
        /// Whether this plan is currently active and available for new subscriptions
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Optional description of the plan
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Gets or sets the collection of subscriptions that use this plan.
        /// </summary>
        [InverseProperty(nameof(Models.Subscription.Plan))]
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        
        /// <summary>
        /// Gets or sets the collection of organization subscriptions that use this plan.
        /// </summary>
        [InverseProperty(nameof(Models.OrganizationSubscription.SubscriptionPlan))]
        public virtual ICollection<OrganizationSubscription> OrganizationSubscriptions { get; set; } = new List<OrganizationSubscription>();
        
        /// <summary>
        /// The ID of the organization this subscription plan belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }
        
        /// <summary>
        /// The organization this subscription plan belongs to.
        /// </summary>
        [ForeignKey(nameof(OrganizationId))]
        public virtual Organization Organization { get; set; } = null!;
        
        /// <summary>
        /// The monthly price of the plan in the smallest currency unit (e.g., cents for USD)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyPrice { get; set; }
        
        /// <summary>
        /// The annual price of the plan in the smallest currency unit (if different from monthly)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AnnualPrice { get; set; }
        
        /// <summary>
        /// The maximum number of users allowed on this plan (null for unlimited)
        /// </summary>
        public int? MaxUsers { get; set; }
        
        /// <summary>
        /// The maximum number of contacts allowed on this plan (null for unlimited)
        /// </summary>
        public int? MaxContacts { get; set; }
        
        /// <summary>
        /// The maximum storage in MB allowed on this plan (null for unlimited)
        /// </summary>
        public long? MaxStorageMb { get; set; }
    }
}
