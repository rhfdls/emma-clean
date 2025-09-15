using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Emma.Models.Enums;

namespace Emma.Models.Models;

/// <summary>
/// Represents the relationship state of a contact in the GoG system.
/// Contacts can transition between states as relationships evolve.
/// </summary>
public enum RelationshipState
{
    Lead,           // Initial contact, not yet engaged
    Prospect,       // Engaged but no active business relationship
    Client,         // Active business relationship/transaction
    PastClient,     // Previous client, transaction completed
    ServiceProvider,// Service provider (lender, inspector, contractor, etc.)
    Agent,          // Real estate agent (team member or external)
    Vendor,         // General business vendor or supplier
    Friend,         // Personal relationship
    Family,         // Family member
    Colleague,      // Industry colleague
    Other           // Catch-all for undefined relationships
}

/// <summary>
/// Represents a contact in the system, which could be a lead, client, partner, or other entity
/// that the organization interacts with. Contacts can be associated with multiple interactions,
/// tasks, and other entities in the system.
/// </summary>
public class Contact : BaseEntity
{
    #region EFCore Mapping
    // Enforced by ModelSync-ContactUserInteraction-2025Q3

    // Navigation properties
    [NotMapped]
    public virtual ICollection<EmailAddress> EmailAddresses { get; set; } = new List<EmailAddress>();
    [NotMapped]
    public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = new List<PhoneNumber>();
    [NotMapped]
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    [NotMapped]
    public virtual ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
    [NotMapped]
    public virtual ICollection<ContactAssignment> AssignedResources { get; set; } = new List<ContactAssignment>();
    [NotMapped]
    public virtual ICollection<ContactCollaborator> Collaborators { get; set; } = new List<ContactCollaborator>();
    [NotMapped]
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    // [Obsolete] legacy fields; ignored in EF mapping
    [Obsolete("Use EmailAddresses instead. Field is ignored in EF mapping.")]
    [NotMapped]
    public List<string>? Emails { get; set; }
    [Obsolete("Use PhoneNumbers instead. Field is ignored in EF mapping.")]
    [NotMapped]
    public List<string>? Phones { get; set; }
    #endregion

    /// <summary>
    /// Gets or sets the ID of the organization that owns this contact.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's first name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the contact's last name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the contact's middle name or initial.
    /// </summary>
    [MaxLength(100)]
    public string? MiddleName { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's preferred name or nickname.
    /// </summary>
    [MaxLength(100)]
    public string? PreferredName { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's title (e.g., Mr., Mrs., Dr.).
    /// </summary>
    [MaxLength(50)]
    public string? Title { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's job title or position.
    /// </summary>
    [MaxLength(200)]
    public string? JobTitle { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's company or organization name.
    /// </summary>
    [MaxLength(200)]
    public string? Company { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's department or team within their organization.
    /// </summary>
    [MaxLength(200)]
    public string? Department { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's current relationship state.
    /// </summary>
    public RelationshipState RelationshipState { get; set; } = RelationshipState.Lead;
    
    /// <summary>
    /// Gets or sets the date when this contact was last contacted.
    /// </summary>
    public DateTime? LastContactedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date when this contact should be followed up with.
    /// </summary>
    public DateTime? NextFollowUpAt { get; set; }
    
    /// <summary>
    /// Gets or sets the source of this contact (e.g., website, referral, event).
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the user who owns or is responsible for this contact.
    /// </summary>
    public Guid? OwnerId { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's preferred method of communication.
    /// </summary>
    [MaxLength(50)]
    public string? PreferredContactMethod { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's preferred time of day for contact.
    /// </summary>
    [MaxLength(50)]
    public string? PreferredContactTime { get; set; }
    
    /// <summary>
    /// Gets or sets any notes or additional information about the contact.
    /// </summary>
    [Column(TypeName = "text")]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Gets or sets the contact's profile picture URL.
    /// </summary>
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }
    
    // Navigation properties
    
    /// <summary>
    /// Gets or sets the organization that owns this contact.
    /// Not mapped to EF; relationship is configured via FK in DbContext.
    /// </summary>
    [NotMapped]
    public virtual Organization? Organization { get; set; }
    
    /// <summary>
    /// Gets or sets the user who owns or is responsible for this contact.
    /// Not mapped to EF; relationship is configured via FK in DbContext.
    /// </summary>
    [NotMapped]
    public virtual User? Owner { get; set; }
    
    /// <summary>
    /// Gets or sets the history of relationship state changes for this contact.
    /// </summary>
    [NotMapped]
    public virtual ICollection<ContactStateHistory> StateHistory { get; set; } = new List<ContactStateHistory>();
    
    // Legacy properties (to be migrated/removed)

    
    
    /// <summary>
    /// Boolean flag for compliance and legal triggers when someone becomes an active client.
    /// Set to true when client signs representation agreement or enters active transaction.
    /// </summary>
    public bool IsActiveClient { get; set; } = false;

    /// <summary>
    /// Soft-archive flag. When true, contact is excluded from default lists and active workflows.
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Timestamp when this contact was archived (if archived).
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    // Deletion audit fields are provided by BaseEntity (DeletedAt, DeletedById). Avoid redefining.
    
    /// <summary>
    /// Timestamp when contact first became a client (for analytics and compliance).
    /// </summary>
    public DateTime? ClientSince { get; set; }
    
    // Service Provider specific fields (when RelationshipState is ServiceProvider or Agent)
    /// <summary>
    /// Company name for service providers (e.g., "ABC Lending", "XYZ Inspections").
    /// </summary>
    public string? CompanyName { get; set; }
    
    /// <summary>
    /// Professional license number for licensed service providers.
    /// </summary>
    public string? LicenseNumber { get; set; }
    
    /// <summary>
    /// Service specialties (e.g., "FHA Loans", "Commercial Properties", "New Construction").
    /// </summary>
    [NotMapped]
    public List<string> Specialties { get; set; } = new();
    
    /// <summary>
    /// Geographic service areas (e.g., "Downtown", "North County", "San Diego County").
    /// </summary>
    [NotMapped]
    public List<string> ServiceAreas { get; set; } = new();
    
    /// <summary>
    /// Professional rating (1-5 stars) based on client feedback.
    /// </summary>
    public decimal? Rating { get; set; }
    
    /// <summary>
    /// Number of reviews/ratings received.
    /// </summary>
    public int ReviewCount { get; set; } = 0;
    
    /// <summary>
    /// Indicates if this is a preferred service provider.
    /// </summary>
    public bool IsPreferred { get; set; } = false;
    
    /// <summary>
    /// Website URL for service providers.
    /// </summary>
    public string? Website { get; set; }
    
    /// <summary>
    /// For agent contacts - links to their User record if they're part of the organization.
    /// </summary>
    
    
    /// <summary>
    /// Segmentation tags only (e.g., VIP, Buyer, Region). DO NOT use for privacy/business logic (CRM, PERSONAL, PRIVATE, etc.).
    /// All privacy/business logic must be enforced via Interaction.Tags.
    /// </summary>
    [Emma.Models.Validation.NoPrivacyBusinessTags]
    [NotMapped]
    public List<string> Tags { get; set; } = new();
    public string? LeadSource { get; set; }
    // Timestamps are provided by BaseEntity (CreatedAt: DateTimeOffset, UpdatedAt: DateTimeOffset?). Avoid redefining.
    [NotMapped]
    public Dictionary<string, string>? CustomFields { get; set; }
    
    // Navigation properties
    /// <summary>
    /// The user who owns/manages this contact (for agent contacts, this is the human agent managing the AI)
    /// </summary>
    
    /// <summary>
    /// The organization this contact belongs to
    /// </summary>
    
    /// <summary>
    /// The user currently assigned to manage this contact
    /// </summary>
    [NotMapped]
    public User? AssignedTo { get; set; }
    
    /// <summary>
    /// All interactions with this contact
    /// </summary>
    
    /// <summary>
    /// Resources (service providers) assigned to this contact
    /// </summary>

    
    // NOTE: PrivacyLevel property has been removed. Run EF Core migration to drop the PrivacyLevel column from the database.
    
    /// <summary>
    /// Helper method to transition contact to a new relationship state.
    /// Automatically tracks history and updates relevant flags.
    /// </summary>
    public void TransitionToState(RelationshipState newState, string? reason = null, Guid? changedByUserId = null)
    {
        if (RelationshipState == newState) return;
        
        var oldState = RelationshipState;
        RelationshipState = newState;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        // Update client-specific flags
        if (newState == RelationshipState.Client && !IsActiveClient)
        {
            IsActiveClient = true;
            ClientSince ??= DateTime.UtcNow;
        }
        else if (newState != RelationshipState.Client && IsActiveClient)
        {
            IsActiveClient = false;
        }
        
        // Record state transition
        StateHistory.Add(new ContactStateHistory
        {
            ContactId = Id,
            FromState = oldState,
            ToState = newState,
            Reason = reason,
            ChangedByUserId = changedByUserId
        });
    }
    
    /// <summary>
    /// Checks if this contact is a service provider (ServiceProvider or Agent).
    /// </summary>
    public bool IsServiceProvider()
    {
        return RelationshipState == RelationshipState.ServiceProvider || 
               RelationshipState == RelationshipState.Agent;
    }
    
    /// <summary>
    /// Checks if the contact is a potential business lead (Lead or Prospect).
    /// </summary>
    public bool IsBusinessLead()
    {
        return RelationshipState == RelationshipState.Lead || 
               RelationshipState == RelationshipState.Prospect;
    }
    
    /// <summary>
    /// Checks if the contact is currently in any client state (active or past).
    /// </summary>
    public bool IsClient()
    {
        return RelationshipState == RelationshipState.Client || 
               RelationshipState == RelationshipState.PastClient;
    }
    
    /// <summary>
    /// Checks if this contact can be assigned as a resource to clients.
    /// </summary>
    public bool CanBeAssignedAsResource()
    {
        return IsServiceProvider() && !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName);
    }
    
    /// <summary>
    /// Assigns this contact as a service provider to a client contact.
    /// </summary>
    /// <param name="clientContact">The contact that will receive this service provider</param>
    /// <param name="assigningUser">The user making the assignment</param>
    /// <param name="purpose">The purpose of this assignment</param>
    /// <param name="clientRequest">Optional client request or notes</param>
    /// <returns>The created ContactAssignment</returns>
    /// <exception cref="InvalidOperationException">Thrown if the contact cannot be assigned as a resource</exception>
    public ContactAssignment AssignToClient(Contact clientContact, User assigningUser, string purpose, string? clientRequest = null)
    {
        if (!CanBeAssignedAsResource())
            throw new InvalidOperationException("Contact cannot be assigned as a resource");
        
        if (!clientContact.IsBusinessLead() && !clientContact.IsClient())
            throw new InvalidOperationException("Can only assign resources to business leads or clients");
        
        if (assigningUser.OrganizationId == null)
            throw new InvalidOperationException("Assigning user must belong to an organization");
            
        var assignment = new ContactAssignment
        {
            ContactId = clientContact.Id,
            ServiceContactId = Id,
            AssignedByUserId = assigningUser.Id,
            AssignedByUser = assigningUser,
            OrganizationId = assigningUser.OrganizationId.Value,
            Purpose = purpose,
            ClientRequest = clientRequest,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Update both sides of the relationship
        // Add to assignments for both sides
        // If you have a collection of assignments, add here; otherwise, only update clientContact.AssignedResources if defined as ContactAssignment
        clientContact.AssignedResources.Add(assignment);
        
        return assignment;
    }
    
    /// <summary>
    /// Grants collaboration access to another user for this contact.
    /// </summary>
    /// <param name="collaboratorUser">The user being granted access</param>
    /// <param name="grantingUser">The user granting the access</param>
    /// <param name="role">The role to assign to the collaborator</param>
    /// <param name="reason">Optional reason for granting access</param>
    /// <returns>The created ContactCollaborator</returns>
    /// <exception cref="InvalidOperationException">Thrown if the contact is not in a valid state for collaboration</exception>
    /// <exception cref="ArgumentNullException">Thrown if required parameters are null</exception>
    public ContactCollaborator GrantCollaborationAccess(User collaboratorUser, User grantingUser, CollaboratorRole role, string? reason = null)
    {
        if (collaboratorUser == null)
            throw new ArgumentNullException(nameof(collaboratorUser));
            
        if (grantingUser == null)
            throw new ArgumentNullException(nameof(grantingUser));
            
        if (grantingUser.OrganizationId == null)
            throw new InvalidOperationException("Granting user must belong to an organization");
            
        if (!IsBusinessLead() && !IsClient())
            throw new InvalidOperationException("Collaboration access can only be granted for business leads or clients");
            
        // Ensure the contact belongs to the same organization as the granting user
        if (OrganizationId != grantingUser.OrganizationId)
            throw new InvalidOperationException("Contact must belong to the same organization as the granting user");
        
        var collaboration = new ContactCollaborator
        {
            ContactId = Id,
            Contact = this,
            CollaboratorUserId = collaboratorUser.Id,
            CollaboratorUser = collaboratorUser,
            GrantedByUserId = grantingUser.Id,
            GrantedByUser = grantingUser,
            OrganizationId = grantingUser.OrganizationId.Value,
            Role = role,
            Reason = reason,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Set default permissions based on role
        switch (role)
        {
            case CollaboratorRole.BackupUser:
                collaboration.CanCreateInteractions = true;
                collaboration.CanEditInteractions = true;
                collaboration.CanAssignResources = true;
                collaboration.CanAccessFinancialData = true;
                collaboration.CanAccessBusinessInteractions = true;
                collaboration.CanAccessPersonalInteractions = false; // Explicitly false for privacy
                break;
                
            case CollaboratorRole.Specialist:
                collaboration.CanCreateInteractions = true;
                collaboration.CanEditInteractions = false;
                collaboration.CanAssignResources = true;
                collaboration.CanAccessFinancialData = false;
                collaboration.CanAccessBusinessInteractions = true;
                collaboration.CanAccessPersonalInteractions = false;
                break;
                
            case CollaboratorRole.TeamLead:
                collaboration.CanCreateInteractions = true;
                collaboration.CanEditInteractions = true;
                collaboration.CanAssignResources = true;
                collaboration.CanAccessFinancialData = true;
                collaboration.CanAccessBusinessInteractions = true;
                collaboration.CanAccessPersonalInteractions = true; // Team leads get full access
                break;
                
            case CollaboratorRole.Assistant:
                collaboration.CanCreateInteractions = true;
                collaboration.CanEditInteractions = false;
                collaboration.CanAssignResources = false;
                collaboration.CanAccessFinancialData = false;
                collaboration.CanAccessBusinessInteractions = true;
                collaboration.CanAccessPersonalInteractions = false;
                break;
                
            case CollaboratorRole.Observer:
                // Read-only access (defaults to false)
                collaboration.CanAccessBusinessInteractions = true;
                break;
                
            case CollaboratorRole.Mentor:
                collaboration.CanCreateInteractions = true;
                collaboration.CanEditInteractions = true;
                collaboration.CanAssignResources = false;
                collaboration.CanAccessFinancialData = false;
                collaboration.CanAccessBusinessInteractions = true;
                collaboration.CanAccessPersonalInteractions = false;
                break;
        }
        
        Collaborators.Add(collaboration);
        return collaboration;
    }
    
    /// <summary>
    /// Updates the rating for this contact if it's a service provider.
    /// </summary>
    /// <param name="newRating">The new rating (1-5)</param>
    /// <param name="feedback">Optional feedback about the rating</param>
    /// <exception cref="InvalidOperationException">Thrown if the contact is not a service provider</exception>
    /// <exception cref="ArgumentException">Thrown if the rating is not between 1 and 5</exception>
    public void UpdateRating(decimal newRating, string? feedback = null)
    {
        if (!IsServiceProvider())
            throw new InvalidOperationException("Only service providers can have ratings");
        
        if (newRating < 1 || newRating > 5)
            throw new ArgumentException("Rating must be between 1 and 5");
        
        // Calculate new average rating
        var totalRating = (Rating ?? 0) * ReviewCount + newRating;
        ReviewCount++;
        Rating = totalRating / ReviewCount;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Checks if a user has permission to view this contact.
    /// </summary>
    /// <param name="user">The user to check permissions for</param>
    /// <returns>True if the user has permission to view this contact</returns>
    public bool CanBeViewedBy(User user)
    {
        if (user == null) return false;
        
        // User can view if they are the owner, assigned to, or a collaborator
        return user.Id == OwnerId || 
               user.Id == AssignedTo?.Id || 
               Collaborators.Any(c => c.CollaboratorUserId == user.Id && c.IsActive);
    }
    
    /// <summary>
    /// Checks if a user has permission to edit this contact.
    /// </summary>
    /// <param name="user">The user to check permissions for</param>
    /// <returns>True if the user has permission to edit this contact</returns>
    public bool CanBeEditedBy(User user)
    {
        if (user == null) return false;
        
        // User can edit if they are the owner, assigned to, or have edit permissions as a collaborator
        if (user.Id == OwnerId || user.Id == AssignedTo?.Id)
            return true;
            
        var collaboration = Collaborators.FirstOrDefault(c => c.CollaboratorUserId == user.Id && c.IsActive);
        return collaboration?.CanEditInteractions == true;
    }
    
    /// <summary>
    /// Transfers ownership of this contact to another user.
    /// </summary>
    /// <param name="newOwner">The new owner user</param>
    /// <param name="transferredBy">The user initiating the transfer</param>
    /// <exception cref="InvalidOperationException">Thrown if the transfer is not allowed</exception>
    public void TransferOwnership(User newOwner, User transferredBy)
    {
        if (newOwner == null)
            throw new ArgumentNullException(nameof(newOwner));
            
        if (transferredBy == null)
            throw new ArgumentNullException(nameof(transferredBy));
            
        // Only current owner or admin can transfer ownership
        if (OwnerId != transferredBy.Id && !transferredBy.IsAdmin)
            throw new InvalidOperationException("Only the current owner or an admin can transfer ownership");
        // Update owner and track the change
        var previousOwnerId = OwnerId;
        OwnerId = newOwner.Id;
        Owner = newOwner;
        
        // Add to state history
        StateHistory.Add(new ContactStateHistory
        {
            ContactId = Id,
            FromState = RelationshipState,
            ToState = RelationshipState,
            ChangedByUserId = transferredBy.Id,
            Reason = $"Ownership transferred from {previousOwnerId} to {newOwner.Id}",
            TransitionDate = DateTime.UtcNow
        });
        
        UpdatedAt = DateTime.UtcNow;
    }
}
