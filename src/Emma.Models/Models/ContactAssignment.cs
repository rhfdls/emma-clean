using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Emma.Models.Enums; // Only one instance should remain at the top of the file

namespace Emma.Models.Models;

/// <summary>
/// Represents the assignment of a service provider contact to a client contact.
/// This model enables tracking of service provider assignments while maintaining
/// proper User/Agent separation and audit trails.
/// </summary>
[Table("ContactAssignments")]
public class ContactAssignment
{
    /// <summary>
    /// Unique identifier for this assignment.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // ===== CORE RELATIONSHIPS =====
    
    /// <summary>
    /// The contact receiving the service (the client).
    /// This is a required field and represents the contact being served.
    /// </summary>
    [Required(ErrorMessage = "ContactId is required")]
    public Guid ContactId { get; set; }
    
    /// <summary>
    /// Navigation property to the client contact receiving the service.
    /// </summary>
    [ForeignKey(nameof(ContactId))]
    public Contact ClientContact { get; set; } = null!;
    
    /// <summary>
    /// The service provider contact being assigned.
    /// This is a required field and represents the contact providing the service.
    /// </summary>
    [Required(ErrorMessage = "ServiceContactId is required")]
    public Guid ServiceContactId { get; set; }
    
    /// <summary>
    /// Navigation property to the service provider contact.
    /// </summary>
    [ForeignKey(nameof(ServiceContactId))]
    public Contact ServiceContact { get; set; } = null!;
    
    /// <summary>
    /// The user who made this assignment.
    /// This is a required field and represents the human user who created the assignment.
    /// </summary>
    [Required(ErrorMessage = "AssignedByUserId is required")]
    public Guid AssignedByUserId { get; set; }
    
    /// <summary>
    /// Navigation property to the user who made the assignment.
    /// </summary>
    [ForeignKey(nameof(AssignedByUserId))]
    public User AssignedByUser { get; set; } = null!;
    
    /// <summary>
    /// The organization that owns this assignment.
    /// This is a required field and ensures proper multi-tenant isolation.
    /// </summary>
    [Required(ErrorMessage = "OrganizationId is required")]
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Navigation property to the organization that owns this assignment.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; } = null!;
    
    // ===== ASSIGNMENT DETAILS =====
    
    /// <summary>
    /// Purpose of the assignment (e.g., "Home inspection", "Loan pre-approval").
    /// This is a required field that describes why this assignment was created.
    /// </summary>
    [Required(ErrorMessage = "Purpose is required")]
    [MaxLength(500, ErrorMessage = "Purpose cannot exceed 500 characters")]
    public string Purpose { get; set; } = string.Empty;
    
    /// <summary>
    /// The current status of this assignment.
    /// Defaults to Active when a new assignment is created.
    /// </summary>
    [Required]
    public ContactAssignmentStatus Status { get; set; } = ContactAssignmentStatus.Active;
    
    /// <summary>
    /// The priority level of this assignment.
    /// Helps in sorting and prioritizing work.
    /// </summary>
    [Required]
    public Priority Priority { get; set; } = Priority.Normal;
    
    /// <summary>
    /// Optional reference to the interaction that triggered this assignment.
    /// Useful for tracking the source of the assignment.
    /// </summary>
    public Guid? InteractionId { get; set; }
    
    /// <summary>
    /// Navigation property to the interaction that triggered this assignment.
    /// </summary>
    [ForeignKey(nameof(InteractionId))]
    public Interaction? Interaction { get; set; }
    
    /// <summary>
    /// Client's specific request or requirements for this assignment.
    /// Limited to 1000 characters to keep the database efficient.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Client request cannot exceed 1000 characters")]
    public string? ClientRequest { get; set; }
    
    // ===== TIMESTAMPS =====
    
    /// <summary>
    /// Date and time when the assignment was made.
    /// Automatically set to the current UTC time when the assignment is created.
    /// </summary>
    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Date and time when the assignment was marked as completed.
    /// Null until the assignment is completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Date and time for a follow-up on this assignment.
    /// Optional and can be set based on business rules or user input.
    /// </summary>
    public DateTime? FollowUpAt { get; set; }
    
    // ===== OUTCOME TRACKING =====
    
    /// <summary>
    /// Indicates whether the client actually used the service provider.
    /// This is typically updated after the service is delivered.
    /// </summary>
    public bool WasUsed { get; set; } = false;
    
    /// <summary>
    /// Client's rating of the service provider on a scale of 1-5 stars.
    /// Null until the client provides a rating.
    /// </summary>
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public decimal? ClientRating { get; set; }
    
    /// <summary>
    /// Client's feedback about the service provider.
    /// Limited to 1000 characters to maintain database efficiency.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Client feedback cannot exceed 1000 characters")]
    public string? ClientFeedback { get; set; }
    
    /// <summary>
    /// Notes about the outcome of the assignment.
    /// Used for internal tracking and reporting.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Outcome notes cannot exceed 1000 characters")]
    public string? OutcomeNotes { get; set; }
    
    /// <summary>
    /// Internal notes for the agent/team.
    /// Not visible to the client or service provider.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Internal notes cannot exceed 1000 characters")]
    public string? InternalNotes { get; set; }

    // ===== COMPLIANCE =====
    
    /// <summary>
    /// Whether referral disclosure has been provided to the client.
    /// Required for compliance in many jurisdictions.
    /// </summary>
    public bool ReferralDisclosureProvided { get; set; } = false;

    /// <summary>
    /// Date when referral disclosure was provided to the client.
    /// Automatically set when ReferralDisclosureProvided is set to true.
    /// </summary>
    public DateTime? ReferralDisclosureDate { get; set; }

    /// <summary>
    /// Whether liability disclaimer has been acknowledged by the client.
    /// Important for legal protection.
    /// </summary>
    public bool LiabilityDisclaimerAcknowledged { get; set; } = false;

    /// <summary>
    /// Date when liability disclaimer was acknowledged by the client.
    /// Automatically set when LiabilityDisclaimerAcknowledged is set to true.
    /// </summary>
    public DateTime? LiabilityDisclaimerDate { get; set; }

    // ===== METADATA =====
    
    /// <summary>
    /// Date and time when this record was created.
    /// Automatically set to the current UTC time when the record is created.
    /// </summary>
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Date and time when this record was last updated.
    /// Automatically updated whenever the record is modified.
    /// </summary>
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Custom fields for storing additional data.
    /// Useful for extensibility without schema changes.
    /// </summary>
    public Dictionary<string, string>? CustomFields { get; set; }
    
    // ===== HELPER METHODS =====
    
    /// <summary>
    /// Marks the assignment as completed with optional notes and rating.
    /// </summary>
    /// <param name="wasUsed">Whether the client used the service provider</param>
    /// <param name="rating">Optional client rating (1-5)</param>
    /// <param name="feedback">Optional client feedback</param>
    /// <param name="outcomeNotes">Optional outcome notes</param>
    public void MarkAsCompleted(bool wasUsed, decimal? rating = null, string? feedback = null, string? outcomeNotes = null)
    {
        Status = ContactAssignmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        WasUsed = wasUsed;
        
        if (rating.HasValue)
            ClientRating = Math.Clamp(rating.Value, 1, 5);
            
        if (!string.IsNullOrWhiteSpace(feedback))
            ClientFeedback = feedback;
            
        if (!string.IsNullOrWhiteSpace(outcomeNotes))
            OutcomeNotes = outcomeNotes;
            
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Records that referral disclosure was provided to the client.
    /// </summary>
    public void RecordDisclosureProvided()
    {
        ReferralDisclosureProvided = true;
        ReferralDisclosureDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Records that the liability disclaimer was acknowledged by the client.
    /// </summary>
    public void RecordLiabilityAcknowledgment()
    {
        LiabilityDisclaimerAcknowledged = true;
        LiabilityDisclaimerDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Validates that the assignment is properly configured.
    /// </summary>
    /// <returns>A tuple indicating validity and any error message.</returns>
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (ContactId == Guid.Empty)
            return (false, "ContactId is required");
            
        if (ServiceContactId == Guid.Empty)
            return (false, "ServiceContactId is required");
            
        if (AssignedByUserId == Guid.Empty)
            return (false, "AssignedByUserId is required");
            
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");
            
        if (string.IsNullOrWhiteSpace(Purpose))
            return (false, "Purpose is required");
            
        if (ClientRating.HasValue && (ClientRating < 1 || ClientRating > 5))
            return (false, "ClientRating must be between 1 and 5");
            
        return (true, null);
    }
    
    /// <summary>
    /// Creates a new assignment based on this one, useful for recurring services.
    /// </summary>
    /// <param name="assignedBy">The user creating the new assignment</param>
    /// <param name="newPurpose">Optional new purpose, uses current purpose if not specified</param>
    /// <returns>A new ContactAssignment with similar settings</returns>
    public ContactAssignment CreateFollowUpAssignment(User assignedBy, string? newPurpose = null)
    {
        if (assignedBy == null)
            throw new ArgumentNullException(nameof(assignedBy));
            
        return new ContactAssignment
        {
            ContactId = this.ContactId,
            ServiceContactId = this.ServiceContactId,
            AssignedByUserId = assignedBy.Id,
            AssignedByUser = assignedBy,
            OrganizationId = this.OrganizationId,
            Organization = this.Organization,
            Purpose = newPurpose ?? this.Purpose,
            Status = ContactAssignmentStatus.Active,
            Priority = this.Priority,
            ClientRequest = this.ClientRequest,
            AssignedAt = DateTime.UtcNow,
            FollowUpAt = this.FollowUpAt,
            CustomFields = this.CustomFields != null 
                ? new Dictionary<string, string>(this.CustomFields) 
                : null
        };
    }
}
