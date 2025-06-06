namespace Emma.Data.Models;

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
/// Tracks state transition history for analytics and audit purposes.
/// </summary>
public class ContactStateHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContactId { get; set; }
    public RelationshipState FromState { get; set; }
    public RelationshipState ToState { get; set; }
    public DateTime TransitionDate { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public Guid? ChangedByUserId { get; set; }
}

public class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<EmailAddress> Emails { get; set; } = new();
    public List<PhoneNumber> Phones { get; set; } = new();
    public Address? Address { get; set; }
    
    /// <summary>
    /// Current relationship state - can evolve over time as relationships change.
    /// </summary>
    public RelationshipState RelationshipState { get; set; } = RelationshipState.Lead;
    
    /// <summary>
    /// Boolean flag for compliance and legal triggers when someone becomes an active client.
    /// Set to true when client signs representation agreement or enters active transaction.
    /// </summary>
    public bool IsActiveClient { get; set; } = false;
    
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
    public List<string> Specialties { get; set; } = new();
    
    /// <summary>
    /// Geographic service areas (e.g., "Downtown", "North County", "San Diego County").
    /// </summary>
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
    /// For agent contacts - links to their Agent record if they're part of the organization.
    /// </summary>
    public Guid? AgentId { get; set; }
    
    /// <summary>
    /// Segmentation tags only (e.g., VIP, Buyer, Region). DO NOT use for privacy/business logic (CRM, PERSONAL, PRIVATE, etc.).
    /// All privacy/business logic must be enforced via Interaction.Tags.
    /// </summary>
    [Emma.Data.Validation.NoPrivacyBusinessTags]
    public List<string> Tags { get; set; } = new();
    public string? LeadSource { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string>? CustomFields { get; set; }
    
    // Navigation properties
    public Agent? Owner { get; set; }
    public Agent? Agent { get; set; }  // For agent contacts
    public List<Interaction> Interactions { get; set; } = new();
    public List<ContactAssignment> AssignedResources { get; set; } = new();  // Resources assigned to this contact
    public List<ContactAssignment> ResourceAssignments { get; set; } = new();  // When this contact is assigned as a resource
    public List<ContactStateHistory> StateHistory { get; set; } = new();
    public List<ContactCollaborator> Collaborators { get; set; } = new();  // Team members with access
    public List<ContactCollaborator> CollaboratingOn { get; set; } = new();  // Contacts this person collaborates on
    
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
        UpdatedAt = DateTime.UtcNow;
        
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
    public ContactAssignment AssignToClient(Contact clientContact, Agent assigningAgent, string purpose, string? clientRequest = null)
    {
        if (!CanBeAssignedAsResource())
            throw new InvalidOperationException("Contact cannot be assigned as a resource");
        
        if (!clientContact.IsBusinessLead() && !clientContact.IsClient())
            throw new InvalidOperationException("Can only assign resources to business leads or clients");
        
        var assignment = new ContactAssignment
        {
            ClientContactId = clientContact.Id,
            ServiceContactId = Id,
            AssignedByAgentId = assigningAgent.Id,
            OrganizationId = assigningAgent.OrganizationId ?? Guid.Empty,
            Purpose = purpose,
            ClientRequest = clientRequest
        };
        
        ResourceAssignments.Add(assignment);
        clientContact.AssignedResources.Add(assignment);
        
        return assignment;
    }
    
    /// <summary>
    /// Grants collaboration access to another agent for this contact.
    /// </summary>
    public ContactCollaborator GrantCollaborationAccess(Agent collaboratorAgent, Agent grantingAgent, CollaboratorRole role, string? reason = null)
    {
        if (!IsBusinessLead() && !IsClient())
            throw new InvalidOperationException("Collaboration access can only be granted for business leads or clients");
        
        var collaboration = new ContactCollaborator
        {
            ContactId = Id,
            CollaboratorAgentId = collaboratorAgent.Id,
            GrantedByAgentId = grantingAgent.Id,
            OrganizationId = grantingAgent.OrganizationId ?? Guid.Empty,
            Role = role,
            Reason = reason
        };
        
        // Set default permissions based on role
        switch (role)
        {
            case CollaboratorRole.BackupAgent:
                collaboration.CanCreateInteractions = true;
                collaboration.CanEditInteractions = true;
                collaboration.CanAssignResources = true;
                collaboration.CanAccessFinancialData = true;
                break;
            case CollaboratorRole.Specialist:
                collaboration.CanCreateInteractions = true;
                collaboration.CanAssignResources = true;
                break;
            case CollaboratorRole.TeamLead:
                collaboration.CanAccessFinancialData = true;
                break;
            case CollaboratorRole.Assistant:
                collaboration.CanCreateInteractions = true;
                break;
            case CollaboratorRole.Observer:
                // Read-only access (defaults)
                break;
        }
        
        Collaborators.Add(collaboration);
        return collaboration;
    }
    
    /// <summary>
    /// Updates service provider rating based on client feedback.
    /// </summary>
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
}

public class EmailAddress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The email address - must be unique across all contacts in the system
    /// </summary>
    public string Address { get; set; } = string.Empty;
    
    public string Type { get; set; } = "primary"; // primary|work|personal|other
    public bool Verified { get; set; } = false;
    
    /// <summary>
    /// Foreign key to the Contact that owns this email address
    /// </summary>
    public Guid ContactId { get; set; }
    
    /// <summary>
    /// Navigation property to the Contact
    /// </summary>
    public Contact Contact { get; set; } = null!;
}

public class PhoneNumber
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = "mobile"; // mobile|work|home|other
    public bool Verified { get; set; } = false;
}

public class Address
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
