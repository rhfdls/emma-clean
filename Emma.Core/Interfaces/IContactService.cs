using Emma.Models.Models;
using Emma.Models.Enums;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Service for managing contacts with strict User/Agent separation.
    /// Enforces business rules, access control, and audit logging.
    /// </summary>
    public interface IContactService
    {
        // ===== CRUD Operations =====
        
        /// <summary>
        /// Gets a contact by ID with proper access control
        /// </summary>
        /// <param name="contactId">ID of the contact to retrieve</param>
        /// <param name="requestingUserId">ID of the user making the request (for access control)</param>
        /// <param name="includeDeleted">Whether to include soft-deleted contacts</param>
        /// <returns>The contact if found and accessible, null if not found, throws if access denied</returns>
        Task<Contact> GetContactByIdAsync(Guid contactId, Guid requestingUserId, bool includeDeleted = false);
        
        /// <summary>
        /// Gets contacts based on filter criteria with proper access control
        /// </summary>
        Task<IEnumerable<Contact>> GetContactsAsync(ContactFilter filter, Guid requestingUserId);
        
        /// <summary>
        /// Creates a new contact with audit trail
        /// </summary>
        Task<Contact> CreateContactAsync(Contact contact, Guid createdByUserId);
        
        /// <summary>
        /// Updates an existing contact with audit trail
        /// </summary>
        Task<Contact> UpdateContactAsync(Contact contact, Guid updatedByUserId);
        
        /// <summary>
        /// Soft-deletes a contact with audit trail
        /// </summary>
        Task<bool> DeleteContactAsync(Guid contactId, Guid deletedByUserId, string reason);
        
        /// <summary>
        /// Restores a soft-deleted contact
        /// </summary>
        Task<bool> RestoreContactAsync(Guid contactId, Guid restoredByUserId);

        // ===== Assignment & Ownership =====
        
        /// <summary>
        /// Assigns a contact to a user
        /// </summary>
        Task<bool> AssignContactToUserAsync(Guid contactId, Guid targetUserId, Guid assignedByUserId, string? notes = null);
        
        /// <summary>
        /// Transfers ownership of a contact to another user
        /// </summary>
        Task<bool> TransferContactOwnershipAsync(Guid contactId, Guid newOwnerUserId, Guid transferredByUserId, string reason);
        
        /// <summary>
        /// Checks if a user is the owner of a contact
        /// </summary>
        Task<bool> IsUserContactOwnerAsync(Guid contactId, Guid userId);

        // ===== Collaboration =====
        
        /// <summary>
        /// Adds a collaborator to a contact
        /// </summary>
        Task<bool> AddCollaboratorAsync(Guid contactId, Guid collaboratorUserId, Guid addedByUserId, 
            ContactCollaborationType collaborationType, DateTime? expiresAt = null, string? notes = null);
            
        /// <summary>
        /// Removes a collaborator from a contact
        /// </summary>
        Task<bool> RemoveCollaboratorAsync(Guid contactId, Guid collaboratorUserId, Guid removedByUserId, string reason);
        
        /// <summary>
        /// Updates collaboration settings
        /// </summary>
        Task<bool> UpdateCollaborationAsync(Guid contactId, Guid collaboratorUserId, Guid updatedByUserId, 
            bool? isActive = null, DateTime? expiresAt = null, string? notes = null);
            
        /// <summary>
        /// Gets all collaborators for a contact
        /// </summary>
        Task<IEnumerable<ContactCollaborator>> GetContactCollaboratorsAsync(Guid contactId, Guid requestingUserId);

        // ===== State Management =====
        
        /// <summary>
        /// Updates the relationship state of a contact with audit trail
        /// </summary>
        Task<bool> UpdateContactStateAsync(Guid contactId, RelationshipState newState, Guid updatedByUserId, string? reason = null);
        
        /// <summary>
        /// Gets the state history of a contact
        /// </summary>
        Task<IEnumerable<ContactStateHistory>> GetContactStateHistoryAsync(Guid contactId, Guid requestingUserId);

        // ===== Search & Filtering =====
        
        /// <summary>
        /// Searches contacts with access control
        /// </summary>
        Task<IEnumerable<Contact>> SearchContactsAsync(string searchTerm, Guid requestingUserId, int maxResults = 50);
        
        /// <summary>
        /// Gets contacts by organization with access control
        /// </summary>
        Task<IEnumerable<Contact>> GetContactsByOrganizationAsync(Guid organizationId, Guid requestingUserId, bool includeInactive = false);
        
        /// <summary>
        /// Gets contacts assigned to a specific user
        /// </summary>
        Task<IEnumerable<Contact>> GetContactsByUserAsync(Guid userId, Guid requestingUserId, bool includeCollaborations = true);

        // ===== Advanced Features =====
        
        /// <summary>
        /// Merges two contacts (primary absorbs secondary)
        /// </summary>
        Task<bool> MergeContactsAsync(Guid primaryContactId, Guid secondaryContactId, Guid mergedByUserId, string reason);
        
        /// <summary>
        /// Adds a tag to a contact
        /// </summary>
        Task<bool> TagContactAsync(Guid contactId, string tag, Guid taggedByUserId, string? notes = null);
        
        /// <summary>
        /// Removes a tag from a contact
        /// </summary>
        Task<bool> RemoveTagAsync(Guid contactId, string tag, Guid removedByUserId, string reason);
    }

    /// <summary>
    /// Filter criteria for contact queries
    /// </summary>
    public record ContactFilter
    {
        public string? SearchTerm { get; init; }
        public RelationshipState? State { get; init; }
        public Guid? OrganizationId { get; init; }
        public Guid? AssignedToUserId { get; init; }
        public bool IncludeInactive { get; init; }
        public bool IncludeDeleted { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 50;
        public string? SortBy { get; init; }
        public bool SortDescending { get; init; }
    }
}
