using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Emma.Models.Models;

// SPRINT1: Added AccountStatus, VerificationToken, IsVerified for onboarding/email verification
public class User : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? FubApiKey { get; set; }
    
    public int? FubUserId { get; set; }
    
    public bool IsActive { get; set; } = true;

    // SPRINT1: Account status for onboarding/email verification
    public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingVerification;

    // SPRINT1: Email verification token (nullable)
    [MaxLength(200)]
    public string? VerificationToken { get; set; }

    // SPRINT1: Quick check for verification
    public bool IsVerified { get; set; } = false;
    
    /// <summary>
    /// Indicates whether this user has administrative privileges.
    /// Admins have elevated permissions across the system.
    /// </summary>
    public bool IsAdmin { get; set; }
    
    /// <summary>
    /// The role of the user within their organization (e.g., Admin, Member, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? Role { get; set; }
    
    // Relationships
    public Guid? OrganizationId { get; set; }
    
    /// <summary>
    /// Gets or sets the organization this user belongs to.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of organizations owned by this user.
    /// </summary>
    [InverseProperty(nameof(Models.Organization.OwnerUser))]
    public virtual ICollection<Organization> OwnedOrganizations { get; set; } = new List<Organization>();
    
    // Navigation properties
    
    /// <summary>
    /// Gets or sets the collection of email addresses associated with this user.
    /// </summary>
    [InverseProperty(nameof(Models.EmailAddress.User))]
    public virtual ICollection<EmailAddress> EmailAddresses { get; set; } = new List<EmailAddress>();
    
    /// <summary>
    /// Gets or sets the collection of tasks assigned to this user.
    /// </summary>
    [InverseProperty(nameof(TaskItem.AssignedToUser))]
    public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    
    /// <summary>
    /// Gets or sets the collection of device tokens for push notifications.
    /// </summary>
    [InverseProperty(nameof(DeviceToken.User))]
    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
    
    /// <summary>
    /// Gets or sets the collection of phone numbers associated with this user.
    /// </summary>
    [InverseProperty(nameof(PhoneNumber.User))]
    public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = new List<PhoneNumber>();
    
    /// <summary>
    /// Gets or sets the collection of contacts owned by this user.
    /// </summary>
    [InverseProperty(nameof(Models.Contact.Owner))]
    public virtual ICollection<Contact> OwnedContacts { get; set; } = new List<Contact>();
    
    /// <summary>
    /// Gets or sets the collection of interactions created by this user.
    /// </summary>
    [InverseProperty(nameof(Models.Interaction.CreatedBy))]
    public virtual ICollection<Interaction> CreatedInteractions { get; set; } = new List<Interaction>();
    
    /// <summary>
    /// Gets or sets the collection of subscription assignments for this user.
    /// </summary>
    [InverseProperty(nameof(UserSubscriptionAssignment.User))]
    public virtual ICollection<UserSubscriptionAssignment> SubscriptionAssignments { get; set; } = new List<UserSubscriptionAssignment>();
    
    /// <summary>
    /// Gets or sets the collection of agents created by this user.
    /// </summary>
    [InverseProperty(nameof(Models.Agent.CreatedBy))]
    public virtual ICollection<Agent> CreatedAgents { get; set; } = new List<Agent>();
    
    /// <summary>
    /// Gets or sets the collection of subscriptions for this user.
    /// </summary>
    [InverseProperty(nameof(Models.Subscription.User))]
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    
    /// <summary>
    /// Gets or sets the collection of subscriptions assigned by this user to others.
    /// </summary>
    [InverseProperty(nameof(UserSubscriptionAssignment.AssignedByUser))]
    public virtual ICollection<UserSubscriptionAssignment> AssignedSubscriptions { get; set; } = new List<UserSubscriptionAssignment>();
    
    // Convenience properties
    
    /// <summary>
    /// Gets the primary phone number for this user, if available.
    /// </summary>
    [NotMapped]
    public PhoneNumber? PrimaryPhoneNumber => PhoneNumbers.FirstOrDefault(p => p.IsPrimary);
    
    /// <summary>
    /// Gets the primary email address for this user, if available.
    /// </summary>
    [NotMapped]
    public EmailAddress? PrimaryEmail => EmailAddresses.FirstOrDefault(e => e.IsPrimary);
    
    // Methods
    public string FullName => $"{FirstName} {LastName}";
    
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
