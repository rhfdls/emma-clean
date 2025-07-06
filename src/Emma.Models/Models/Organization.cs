using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Emma.Models.Enums;

namespace Emma.Models.Models
{
    /// <summary>
    /// Represents an organization in the EMMA platform, which can have multiple users and AI agents.
    /// </summary>
    [Table("Organizations")]
    // SPRINT1: Added OrgGuid, PlanType, SeatCount for onboarding and plan/seat tracking
public class Organization : BaseEntity
{
    /// <summary>
    /// Gets or sets the collection of subscriptions for this organization.
    /// </summary>
    [InverseProperty(nameof(OrganizationSubscription.Organization))]
    public virtual ICollection<OrganizationSubscription> Subscriptions { get; set; } = new List<OrganizationSubscription>();

        // ===== CORE PROPERTIES =====

        // SPRINT1: Persistent organization GUID
        [Required]
        public Guid OrgGuid { get; set; } = Guid.NewGuid();

        // SPRINT1: Plan type for quick lookup (optional, can be synced with OrganizationSubscription)
        public PlanType? PlanType { get; set; }

        // SPRINT1: Seat count for plan enforcement (optional)
        public int? SeatCount { get; set; }
        
        /// <summary>
        /// The name of the organization.
        /// </summary>
        [Required(ErrorMessage = "Organization name is required")]
        [MaxLength(200, ErrorMessage = "Organization name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the website URL of the organization.
        /// </summary>
        [Url(ErrorMessage = "Invalid website URL format")]
        [MaxLength(255, ErrorMessage = "Website URL cannot exceed 255 characters")]
        public string? Website { get; set; }
        
        /// <summary>
        /// Gets or sets the primary phone number of the organization.
        /// </summary>
        [Phone(ErrorMessage = "Invalid phone number format")]
        [MaxLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
        public string? PhoneNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the URL to the organization's logo.
        /// </summary>
        [Url(ErrorMessage = "Invalid logo URL format")]
        [MaxLength(500, ErrorMessage = "Logo URL cannot exceed 500 characters")]
        public string? LogoUrl { get; set; }
        
        /// <summary>
        /// Gets or sets the timezone of the organization (e.g., "America/New_York").
        /// </summary>
        [MaxLength(100, ErrorMessage = "Timezone cannot exceed 100 characters")]
        public string? TimeZone { get; set; }
        
        /// <summary>
        /// Gets or sets the locale of the organization (e.g., "en-US").
        /// </summary>
        [MaxLength(20, ErrorMessage = "Locale cannot exceed 20 characters")]
        public string? Locale { get; set; }
        
        /// <summary>
        /// Gets or sets the default currency code for the organization (e.g., "USD").
        /// </summary>
        [MaxLength(3, ErrorMessage = "Currency code cannot exceed 3 characters")]
        public string? Currency { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the organization is active.
        /// </summary>
            
        /// <summary>
        /// The primary contact email for the organization.
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// The ID of the user who owns this organization.
        /// </summary>
        [Required(ErrorMessage = "Owner user ID is required")]
        public Guid OwnerUserId { get; set; }
        
        /// <summary>
        /// Navigation property for the user who owns this organization.
        /// </summary>
        [ForeignKey(nameof(OwnerUserId))]
        [InverseProperty("OwnedOrganizations")]
        public virtual User OwnerUser { get; set; } = null!;
        
        // ===== NAVIGATION PROPERTIES =====
        
        /// <summary>
        /// Gets or sets the collection of users belonging to this organization.
        /// </summary>
        [InverseProperty(nameof(User.Organization))]
public virtual ICollection<User> Users { get; set; } = new List<User>();
            
        /// <summary>
        /// Gets or sets the collection of contacts belonging to this organization.
        /// </summary>
        [InverseProperty(nameof(Models.Contact.Organization))]
        public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        
        /// <summary>
        /// Gets or sets the collection of interactions associated with this organization.
        /// </summary>
        [InverseProperty(nameof(Models.Interaction.Organization))]
public virtual ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
            
        /// <summary>
        /// Gets or sets the collection of agents associated with this organization.
        /// </summary>
        [InverseProperty(nameof(Models.Agent.Organization))]
public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
            
        /// <summary>
        /// Gets or sets the collection of subscription plans available to this organization.
        /// </summary>
        [InverseProperty(nameof(SubscriptionPlan.Organization))]
        public virtual ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();
        
        /// <summary>
        /// Industry code for this organization (e.g., "RealEstate", "Mortgage", "Financial").
        /// Determines which industry-specific EMMA profile to use.
        /// </summary>
        [Required(ErrorMessage = "Industry code is required")]
        [MaxLength(50, ErrorMessage = "Industry code cannot exceed 50 characters")]
        public string IndustryCode { get; set; } = "RealEstate"; // Default to RealEstate
        
        /// <summary>
        /// The date and time when the organization was created.
        /// </summary>
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// The date and time when the organization was last updated.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Indicates whether the organization is currently active.
        /// </summary>
            
        // ===== EXTERNAL INTEGRATIONS =====
        
        /// <summary>
        /// API key for Follow Up Boss integration.
        /// </summary>
        [MaxLength(100, ErrorMessage = "FUB API key cannot exceed 100 characters")]
        public string? FubApiKey { get; set; }
        
        /// <summary>
        /// Follow Up Boss system identifier.
        /// </summary>
        [MaxLength(50, ErrorMessage = "FUB system cannot exceed 50 characters")]
        public string? FubSystem { get; set; }
        
        /// <summary>
        /// Follow Up Boss system key.
        /// </summary>
        [MaxLength(100, ErrorMessage = "FUB system key cannot exceed 100 characters")]
        public string? FubSystemKey { get; set; }
        
        /// <summary>
        /// Follow Up Boss organization ID.
        /// </summary>
        public int? FubId { get; set; }
        
        // ===== NAVIGATION PROPERTIES =====
        
        /// <summary>
        /// Collection of users belonging to this organization.
        /// </summary>
            
        /// <summary>
        /// Collection of AI agents configured for this organization.
        /// </summary>
            
        /// <summary>
        /// Collection of subscriptions associated with this organization.
        /// </summary>
            
        /// <summary>
        /// Collection of interactions associated with this organization.
        /// </summary>
            
        // ===== HELPER METHODS =====
        
        /// <summary>
        /// Checks if a user is the owner of this organization.
        /// </summary>
        /// <param name="userId">The ID of the user to check</param>
        /// <returns>True if the user is the owner, otherwise false</returns>
        public bool IsOwner(Guid userId) => OwnerUserId == userId;
        
        /// <summary>
        /// Adds a user to the organization with the specified role.
        /// </summary>
        /// <param name="user">The user to add</param>
        /// <param name="role">The role to assign to the user</param>
        public void AddUser(User user, string role = "Member")
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
                
            if (user.OrganizationId != null && user.OrganizationId != Id)
                throw new InvalidOperationException("User already belongs to a different organization");
                
            user.OrganizationId = Id;
            user.Role = role;
            Users.Add(user);
        }
        
        /// <summary>
        /// Removes a user from the organization.
        /// </summary>
        /// <param name="userId">The ID of the user to remove</param>
        /// <returns>True if the user was removed, false if not found</returns>
        public bool RemoveUser(Guid userId)
        {
            var user = Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.OrganizationId = null;
                return Users.Remove(user);
            }
            return false;
        }
        
        /// <summary>
        /// Adds an AI agent to the organization.
        /// </summary>
        /// <param name="agent">The agent to add</param>
        public void AddAgent(Agent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));
                
            if (agent.OrganizationId != null && agent.OrganizationId != Id)
                throw new InvalidOperationException("Agent already belongs to a different organization");
                
            agent.OrganizationId = Id;
            Agents.Add(agent);
        }
        
        /// <summary>
        /// Validates the organization data.
        /// </summary>
        /// <returns>A tuple indicating whether the organization is valid and any error message.</returns>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return (false, "Organization name is required");
                
            if (string.IsNullOrWhiteSpace(Email))
                return (false, "Email is required");
                
            if (!new EmailAddressAttribute().IsValid(Email))
                return (false, "Invalid email format");
                
            if (OwnerUserId == Guid.Empty)
                return (false, "Owner user ID is required");
                
            if (string.IsNullOrWhiteSpace(IndustryCode))
                return (false, "Industry code is required");
                
            return (true, null);
        }
    }
}
