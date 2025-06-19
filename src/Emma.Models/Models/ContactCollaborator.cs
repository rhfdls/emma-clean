using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Defines the level of access and permissions for a user collaborating on a contact.
/// This enables team-based contact management while maintaining appropriate access controls.
/// </summary>
public enum CollaboratorRole
{
    /// <summary>
    /// Can handle all business interactions in primary user's absence.
    /// Has full access to business-related data but respects personal data privacy.
    /// </summary>
    BackupUser,
    
    /// <summary>
    /// Has expertise in specific areas (e.g., luxury, commercial properties).
    /// Can interact with relevant contacts and update information.
    /// </summary>
    Specialist,
    
    /// <summary>
    /// Senior user providing guidance to junior team members.
    /// Can view and edit interactions they're involved in.
    /// </summary>
    Mentor,
    
    /// <summary>
    /// Administrative support with limited access.
    /// Can perform assigned tasks but cannot access sensitive information.
    /// </summary>
    Assistant,
    
    /// <summary>
    /// Team leader with oversight access.
    /// Can view all team interactions and manage team resources.
    /// </summary>
    TeamLead,
    
    /// <summary>
    /// Read-only access for training or oversight purposes.
    /// Cannot make any changes to contact data.
    /// </summary>
    Observer
}

/// <summary>
/// Represents a collaboration relationship between a user and a contact they have access to.
/// Defines the scope of access and permissions for the collaboration.
/// </summary>
[Table("ContactCollaborators")]
public class ContactCollaborator
{
    /// <summary>
    /// Unique identifier for this collaboration record.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Date and time when this collaboration record was created.
    /// </summary>
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Date and time when this collaboration record was last updated.
    /// </summary>
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// The client contact that the collaborator has access to.
    /// This is a required field and cannot be null.
    /// </summary>
    [Required(ErrorMessage = "ContactId is required")]
    public Guid ContactId { get; set; }
    
    /// <summary>
    /// Navigation property to the contact being collaborated on.
    /// </summary>
    [ForeignKey(nameof(ContactId))]
    public Contact Contact { get; set; } = null!;
    
    /// <summary>
    /// The user who is collaborating on this contact.
    /// This must be a valid user ID within the organization.
    /// </summary>
    [Required(ErrorMessage = "CollaboratorUserId is required")]
    public Guid CollaboratorUserId { get; set; }
    
    /// <summary>
    /// Navigation property to the user who is collaborating.
    /// </summary>
    [ForeignKey(nameof(CollaboratorUserId))]
    public User CollaboratorUser { get; set; } = null!;
    
    /// <summary>
    /// The user who granted this collaboration access.
    /// This is typically an admin or the contact owner.
    /// </summary>
    [Required(ErrorMessage = "GrantedByUserId is required")]
    public Guid GrantedByUserId { get; set; }
    
    /// <summary>
    /// Navigation property to the user who granted access.
    /// </summary>
    [ForeignKey(nameof(GrantedByUserId))]
    public User GrantedByUser { get; set; } = null!;
    
    /// <summary>
    /// The organization that this collaboration belongs to.
    /// This ensures proper multi-tenant isolation.
    /// </summary>
    [Required(ErrorMessage = "OrganizationId is required")]
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Navigation property to the organization.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; } = null!;
    
    /// <summary>
    /// Role defining the type and scope of collaboration.
    /// This determines the default permissions for the collaborator.
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    public CollaboratorRole Role { get; set; } = CollaboratorRole.Assistant;
    
    /// <summary>
    /// Can access business interactions (CRM tagged interactions).
    /// </summary>
    public bool CanAccessBusinessInteractions { get; set; } = true;
    
    /// <summary>
    /// Can access personal interactions (PERSONAL tagged interactions).
    /// Usually false to respect privacy.
    /// </summary>
    public bool CanAccessPersonalInteractions { get; set; } = false;
    
    /// <summary>
    /// Can create new interactions on behalf of the primary agent.
    /// </summary>
    public bool CanCreateInteractions { get; set; } = false;
    
    /// <summary>
    /// Can modify existing interactions.
    /// </summary>
    public bool CanEditInteractions { get; set; } = false;
    
    /// <summary>
    /// Can assign resources to this contact.
    /// </summary>
    public bool CanAssignResources { get; set; } = false;
    
    /// <summary>
    /// Can view contact's financial/transaction details.
    /// </summary>
    public bool CanAccessFinancialData { get; set; } = false;
    
    /// <summary>
    /// Can edit contact details and properties.
    /// </summary>
    public bool CanEditContactDetails { get; set; } = false;
    
    /// <summary>
    /// Can manage other collaborators for this contact.
    /// </summary>
    public bool CanManageCollaborators { get; set; } = false;
    
    /// <summary>
    /// Can view audit logs for this contact.
    /// </summary>
    public bool CanViewAuditLogs { get; set; } = false;
    
    /// <summary>
    /// Optional reason for granting collaboration access.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    /// <summary>
    /// When collaboration access expires (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether this collaboration is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Determines if this collaboration is currently active and not expired.
    /// </summary>
    public bool IsValid => IsActive && (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);
    
    /// <summary>
    /// Validates that this collaboration record is properly configured.
    /// </summary>
    /// <returns>A tuple indicating validity and any error message.</returns>
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (ContactId == Guid.Empty)
            return (false, "ContactId is required");
            
        if (CollaboratorUserId == Guid.Empty)
            return (false, "CollaboratorUserId is required");
            
        if (GrantedByUserId == Guid.Empty)
            return (false, "GrantedByUserId is required");
            
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");
            
        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
            return (false, "Collaboration has expired");
            
        return (true, null);
    }
    
    /// <summary>
    /// Checks if the collaborator can access a specific interaction based on their permissions.
    /// </summary>
    /// <param name="interaction">The interaction to check access for</param>
    /// <returns>True if access is allowed, false otherwise</returns>
    public bool CanAccessInteraction(Interaction interaction)
    {
        if (interaction == null)
            throw new ArgumentNullException(nameof(interaction));
            
        // Check if collaboration is active and not expired
        if (!IsValid)
            return false;
        
        // Check interaction privacy tags
        var hasPersonalTag = interaction.Tags?.Contains("PERSONAL", StringComparer.OrdinalIgnoreCase) == true;
        var hasBusinessTag = interaction.Tags?.Contains("CRM", StringComparer.OrdinalIgnoreCase) == true;
        
        // Apply privacy rules based on tags
        if (hasPersonalTag && !CanAccessPersonalInteractions)
            return false;
            
        if (hasBusinessTag && !CanAccessBusinessInteractions)
            return false;
        
        // Handle untagged interactions (default to business rules)
        if (!hasPersonalTag && !hasBusinessTag && !CanAccessBusinessInteractions)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Checks if the collaborator has a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check</param>
    /// <returns>True if the permission is granted</returns>
    public bool HasPermission(Func<ContactCollaborator, bool> permission)
    {
        return IsValid && permission(this);
    }
    
    /// <summary>
    /// Updates the permissions based on the current role.
    /// This can be overridden by setting individual permission flags.
    /// </summary>
    public void UpdatePermissionsFromRole()
    {
        // Reset all permissions to defaults for the role
        switch (Role)
        {
            case CollaboratorRole.BackupUser:
                CanAccessBusinessInteractions = true;
                CanAccessPersonalInteractions = false;
                CanCreateInteractions = true;
                CanEditInteractions = true;
                CanAssignResources = true;
                CanAccessFinancialData = true;
                CanEditContactDetails = true;
                CanManageCollaborators = false;
                CanViewAuditLogs = true;
                break;
                
            case CollaboratorRole.Specialist:
                CanAccessBusinessInteractions = true;
                CanAccessPersonalInteractions = false;
                CanCreateInteractions = true;
                CanEditInteractions = false;
                CanAssignResources = true;
                CanAccessFinancialData = false;
                CanEditContactDetails = false;
                CanManageCollaborators = false;
                CanViewAuditLogs = false;
                break;
                
            case CollaboratorRole.TeamLead:
                CanAccessBusinessInteractions = true;
                CanAccessPersonalInteractions = true;
                CanCreateInteractions = true;
                CanEditInteractions = true;
                CanAssignResources = true;
                CanAccessFinancialData = true;
                CanEditContactDetails = true;
                CanManageCollaborators = true;
                CanViewAuditLogs = true;
                break;
                
            case CollaboratorRole.Mentor:
                CanAccessBusinessInteractions = true;
                CanAccessPersonalInteractions = false;
                CanCreateInteractions = true;
                CanEditInteractions = true;
                CanAssignResources = false;
                CanAccessFinancialData = false;
                CanEditContactDetails = false;
                CanManageCollaborators = false;
                CanViewAuditLogs = true;
                break;
                
            case CollaboratorRole.Assistant:
                CanAccessBusinessInteractions = true;
                CanAccessPersonalInteractions = false;
                CanCreateInteractions = true;
                CanEditInteractions = false;
                CanAssignResources = false;
                CanAccessFinancialData = false;
                CanEditContactDetails = false;
                CanManageCollaborators = false;
                CanViewAuditLogs = false;
                break;
                
            case CollaboratorRole.Observer:
                // Read-only access
                CanAccessBusinessInteractions = true;
                CanAccessPersonalInteractions = false;
                CanCreateInteractions = false;
                CanEditInteractions = false;
                CanAssignResources = false;
                CanAccessFinancialData = false;
                CanEditContactDetails = false;
                CanManageCollaborators = false;
                CanViewAuditLogs = true;
                break;
        }
    }
    
    /// <summary>
    /// Creates a copy of this collaboration with the same permissions but for a different user.
    /// </summary>
    /// <param name="newCollaborator">The user to grant the same access to</param>
    /// <param name="grantedBy">The user who is granting this access</param>
    /// <returns>A new ContactCollaborator instance</returns>
    public ContactCollaborator CloneForUser(User newCollaborator, User grantedBy)
    {
        if (newCollaborator == null)
            throw new ArgumentNullException(nameof(newCollaborator));
            
        if (grantedBy == null)
            throw new ArgumentNullException(nameof(grantedBy));
            
        return new ContactCollaborator
        {
            ContactId = this.ContactId,
            Contact = this.Contact,
            CollaboratorUserId = newCollaborator.Id,
            CollaboratorUser = newCollaborator,
            GrantedByUserId = grantedBy.Id,
            GrantedByUser = grantedBy,
            OrganizationId = this.OrganizationId,
            Organization = this.Organization,
            Role = this.Role,
            
            // Copy all permission flags
            CanAccessBusinessInteractions = this.CanAccessBusinessInteractions,
            CanAccessPersonalInteractions = this.CanAccessPersonalInteractions,
            CanCreateInteractions = this.CanCreateInteractions,
            CanEditInteractions = this.CanEditInteractions,
            CanAssignResources = this.CanAssignResources,
            CanAccessFinancialData = this.CanAccessFinancialData,
            CanEditContactDetails = this.CanEditContactDetails,
            CanManageCollaborators = this.CanManageCollaborators,
            CanViewAuditLogs = this.CanViewAuditLogs,
            
            // Copy metadata
            Reason = $"Cloned from collaboration with {this.CollaboratorUser.Email}",
            ExpiresAt = this.ExpiresAt,
            IsActive = this.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
