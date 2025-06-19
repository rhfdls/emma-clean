using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services
{
    /// <summary>
    /// Service for managing contacts with strict User/Agent separation.
    /// Enforces business rules, access control, and audit logging.
    /// </summary>
    public class ContactService : IContactService
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<ContactService> _logger;
        private readonly IContactAccessService _contactAccessService;

        public ContactService(
            IAppDbContext context, 
            ILogger<ContactService> logger,
            IContactAccessService contactAccessService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contactAccessService = contactAccessService ?? throw new ArgumentNullException(nameof(contactAccessService));
        }

        #region CRUD Operations

        /// <summary>
        /// Gets a contact by ID with proper access control
        /// </summary>
        public async Task<Contact> GetContactByIdAsync(Guid contactId, Guid requestingUserId, bool includeDeleted = false)
        {
            // Check if user has access to this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, requestingUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to access contact {ContactId} without permission", 
                    requestingUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to access this contact");
            }

            var query = _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Include(c => c.Collaborators)
                    .ThenInclude(cc => cc.CollaboratorUser)
                .AsQueryable();

            if (includeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }

            var contact = await query.FirstOrDefaultAsync(c => c.Id == contactId);
            if (contact == null)
            {
                _logger.LogWarning("Contact {ContactId} not found", contactId);
                throw new KeyNotFoundException($"Contact with ID {contactId} not found");
            }

            // Log the access
            await _contactAccessService.LogContactAccessAsync(
                contactId, 
                requestingUserId, 
                true, 
                "Contact details retrieved");

            return contact;
        }

        /// <summary>
        /// Gets contacts based on filter criteria with proper access control
        /// </summary>
        public async Task<IEnumerable<Contact>> GetContactsAsync(ContactFilter filter, Guid requestingUserId)
        {
            var query = _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .AsQueryable();

            // Apply filters
            if (filter.OrganizationId.HasValue)
            {
                query = query.Where(c => c.OrganizationId == filter.OrganizationId);
            }

            if (filter.State.HasValue)
            {
                query = query.Where(c => c.RelationshipState == filter.State.Value);
            }

            if (filter.AssignedToUserId.HasValue)
            {
                query = query.Where(c => c.AssignedToUserId == filter.AssignedToUserId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLowerInvariant();
                query = query.Where(c => 
                    c.FirstName.ToLower().Contains(term) ||
                    c.LastName.ToLower().Contains(term) ||
                    c.CompanyName.ToLower().Contains(term) ||
                    c.Emails.Any(e => e.Address.ToLower().Contains(term)) ||
                    c.Phones.Any(p => p.Number.Contains(term)));
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDescending 
                    ? query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName)
                    : query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName),
                "created" => filter.SortDescending
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt),
                _ => filter.SortDescending
                    ? query.OrderByDescending(c => c.UpdatedAt)
                    : query.OrderBy(c => c.UpdatedAt)
            };

            // Apply pagination
            if (filter.PageNumber > 0 && filter.PageSize > 0)
            {
                query = query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize);
            }

            var contacts = await query.ToListAsync();
            
            // Filter out contacts the user doesn't have access to
            var accessibleContacts = new List<Contact>();
            foreach (var contact in contacts)
            {
                if (await _contactAccessService.CanAccessContactAsync(contact.Id, requestingUserId))
                {
                    accessibleContacts.Add(contact);
                }
            }

            _logger.LogInformation("Retrieved {Count} contacts for user {UserId}", 
                accessibleContacts.Count, requestingUserId);

            return accessibleContacts;
        }

        /// <summary>
        /// Creates a new contact with audit trail
        /// </summary>
        public async Task<Contact> CreateContactAsync(Contact contact, Guid createdByUserId)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            
            // Set audit fields
            contact.CreatedAt = DateTime.UtcNow;
            contact.UpdatedAt = DateTime.UtcNow;
            contact.CreatedByUserId = createdByUserId;
            contact.UpdatedByUserId = createdByUserId;

            // Set default relationship state if not provided
            if (contact.RelationshipState == default)
            {
                contact.RelationshipState = RelationshipState.Lead;
            }

            // Ensure organization is set (required)
            if (contact.OrganizationId == Guid.Empty)
            {
                throw new ArgumentException("Organization ID is required when creating a contact");
            }

            // Add to database
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            // Log the creation
            _logger.LogInformation("User {UserId} created contact {ContactId} in organization {OrganizationId}",
                createdByUserId, contact.Id, contact.OrganizationId);

            return contact;
        }

        /// <summary>
        /// Updates an existing contact with audit trail
        /// </summary>
        public async Task<Contact> UpdateContactAsync(Contact contact, Guid updatedByUserId)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            
            // Check if user has permission to update this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contact.Id, updatedByUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to update contact {ContactId} without permission", 
                    updatedByUserId, contact.Id);
                throw new UnauthorizedAccessException("You do not have permission to update this contact");
            }

            // Get existing contact to track changes
            var existingContact = await _context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contact.Id);

            if (existingContact == null)
            {
                throw new KeyNotFoundException($"Contact with ID {contact.Id} not found");
            }

            // Update audit fields
            contact.UpdatedAt = DateTime.UtcNow;
            contact.UpdatedByUserId = updatedByUserId;

            // Update the contact
            _context.Contacts.Update(contact);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated contact {ContactId}", 
                updatedByUserId, contact.Id);

            return contact;
        }

        /// <summary>
        /// Soft-deletes a contact with audit trail
        /// </summary>
        public async Task<bool> DeleteContactAsync(Guid contactId, Guid deletedByUserId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A reason is required when deleting a contact", nameof(reason));
            }

            // Check if user has permission to delete this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, deletedByUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to delete contact {ContactId} without permission", 
                    deletedByUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to delete this contact");
            }

            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null)
            {
                _logger.LogWarning("Attempted to delete non-existent contact {ContactId}", contactId);
                return false;
            }

            // Soft delete
            contact.IsDeleted = true;
            contact.DeletedAt = DateTime.UtcNow;
            contact.DeletedByUserId = deletedByUserId;
            contact.DeleteReason = reason;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} soft-deleted contact {ContactId}. Reason: {Reason}", 
                deletedByUserId, contactId, reason);

            return true;
        }

        /// <summary>
        /// Restores a soft-deleted contact
        /// </summary>
        public async Task<bool> RestoreContactAsync(Guid contactId, Guid restoredByUserId)
        {
            var contact = await _context.Contacts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == contactId && c.IsDeleted);

            if (contact == null)
            {
                _logger.LogWarning("Attempted to restore non-existent or not-deleted contact {ContactId}", contactId);
                return false;
            }

            // Check if user has permission to restore this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, restoredByUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to restore contact {ContactId} without permission", 
                    restoredByUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to restore this contact");
            }

            // Restore the contact
            contact.IsDeleted = false;
            contact.DeletedAt = null;
            contact.DeletedByUserId = null;
            contact.DeleteReason = null;
            contact.UpdatedAt = DateTime.UtcNow;
            contact.UpdatedByUserId = restoredByUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} restored contact {ContactId}", 
                restoredByUserId, contactId);

            return true;
        }

        #endregion

        #region Assignment & Ownership

        /// <summary>
        /// Assigns a contact to a user
        /// </summary>
        public async Task<bool> AssignContactToUserAsync(Guid contactId, Guid targetUserId, Guid assignedByUserId, string? notes = null)
        {
            // Check if user has permission to assign this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, assignedByUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to assign contact {ContactId} without permission", 
                    assignedByUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to assign this contact");
            }

            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null)
            {
                _logger.LogWarning("Attempted to assign non-existent contact {ContactId}", contactId);
                return false;
            }

            // Check if target user exists
            var targetUser = await _context.Users.FindAsync(targetUserId);
            if (targetUser == null)
            {
                _logger.LogWarning("Attempted to assign contact {ContactId} to non-existent user {UserId}", 
                    contactId, targetUserId);
                throw new ArgumentException("Target user not found", nameof(targetUserId));
            }

            // Update assignment
            var previousOwnerId = contact.AssignedToUserId;
            contact.AssignedToUserId = targetUserId;
            contact.UpdatedAt = DateTime.UtcNow;
            contact.UpdatedByUserId = assignedByUserId;

            // Log the assignment
            var assignment = new ContactAssignment
            {
                ContactId = contactId,
                AssignedToUserId = targetUserId,
                AssignedByUserId = assignedByUserId,
                AssignedAt = DateTime.UtcNow,
                Notes = notes,
                PreviousOwnerId = previousOwnerId
            };
            _context.ContactAssignments.Add(assignment);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {AssignedByUserId} assigned contact {ContactId} to user {TargetUserId}", 
                assignedByUserId, contactId, targetUserId);

            return true;
        }

        /// <summary>
        /// Transfers ownership of a contact to another user
        /// </summary>
        public async Task<bool> TransferContactOwnershipAsync(
            Guid contactId, 
            Guid newOwnerUserId, 
            Guid transferredByUserId, 
            string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A reason is required when transferring contact ownership", nameof(reason));
            }

            // Check if user has permission to transfer ownership of this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, transferredByUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to transfer ownership of contact {ContactId} without permission", 
                    transferredByUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to transfer ownership of this contact");
            }

            var contact = await _context.Contacts
                .Include(c => c.Collaborators)
                .FirstOrDefaultAsync(c => c.Id == contactId);

            if (contact == null)
            {
                _logger.LogWarning("Attempted to transfer ownership of non-existent contact {ContactId}", contactId);
                return false;
            }


            // Check if new owner exists
            var newOwner = await _context.Users.FindAsync(newOwnerUserId);
            if (newOwner == null)
            {
                _logger.LogWarning("Attempted to transfer contact {ContactId} to non-existent user {UserId}", 
                    contactId, newOwnerUserId);
                throw new ArgumentException("New owner not found", nameof(newOwnerUserId));
            }

            // Log the previous owner
            var previousOwnerId = contact.OwnerUserId;
            
            // Update ownership
            contact.OwnerUserId = newOwnerUserId;
            contact.UpdatedAt = DateTime.UtcNow;
            contact.UpdatedByUserId = transferredByUserId;

            // Remove new owner from collaborators if they were one
            var existingCollaboration = contact.Collaborators
                .FirstOrDefault(c => c.CollaboratorUserId == newOwnerUserId);
                
            if (existingCollaboration != null)
            {
                contact.Collaborators.Remove(existingCollaboration);
            }

            // Log the ownership transfer
            var transferLog = new ContactOwnershipTransfer
            {
                ContactId = contactId,
                PreviousOwnerId = previousOwnerId,
                NewOwnerId = newOwnerUserId,
                TransferredByUserId = transferredByUserId,
                TransferredAt = DateTime.UtcNow,
                Reason = reason
            };
            _context.ContactOwnershipTransfers.Add(transferLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {TransferredByUserId} transferred ownership of contact {ContactId} from {PreviousOwnerId} to {NewOwnerId}", 
                transferredByUserId, contactId, previousOwnerId, newOwnerUserId);

            return true;
        }

        /// <summary>
        /// Checks if a user is the owner of a contact
        /// </summary>
        public async Task<bool> IsUserContactOwnerAsync(Guid contactId, Guid userId)
        {
            var contact = await _context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contactId);

            if (contact == null)
            {
                _logger.LogWarning("Attempted to check ownership for non-existent contact {ContactId}", contactId);
                throw new KeyNotFoundException($"Contact with ID {contactId} not found");
            }

            return contact.OwnerUserId == userId || contact.AssignedToUserId == userId;
        }

        #endregion

        #region Collaboration

        /// <summary>
        /// Adds a collaborator to a contact with the specified access level
        /// </summary>
        public async Task<bool> AddCollaboratorAsync(
            Guid contactId, 
            Guid collaboratorUserId, 
            Guid addedByUserId, 
            ContactCollaborationType collaborationType, 
            string? notes = null)
        {
            // Check if user has permission to add collaborators to this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, addedByUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to add collaborator to contact {ContactId} without permission", 
                    addedByUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to add collaborators to this contact");
            }

            // Check if contact exists
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null)
            {
                _logger.LogWarning("Attempted to add collaborator to non-existent contact {ContactId}", contactId);
                return false;
            }

            // Check if collaborator user exists
            var collaborator = await _context.Users.FindAsync(collaboratorUserId);
            if (collaborator == null)
            {
                _logger.LogWarning("Attempted to add non-existent user {UserId} as collaborator to contact {ContactId}", 
                    collaboratorUserId, contactId);
                throw new ArgumentException("Collaborator user not found", nameof(collaboratorUserId));
            }

            // Check if user is trying to add themselves
            if (collaboratorUserId == addedByUserId)
            {
                _logger.LogWarning("User {UserId} attempted to add themselves as a collaborator to contact {ContactId}", 
                    addedByUserId, contactId);
                throw new InvalidOperationException("You cannot add yourself as a collaborator");
            }

            // Check if collaborator is already added
            var existingCollaboration = await _context.ContactCollaborators
                .FirstOrDefaultAsync(cc => cc.ContactId == contactId && cc.CollaboratorUserId == collaboratorUserId);

            if (existingCollaboration != null)
            {
                if (existingCollaboration.IsActive)
                {
                    _logger.LogWarning("User {UserId} is already a collaborator on contact {ContactId}", 
                        collaboratorUserId, contactId);
                    return false;
                }
                
                // Reactivate existing collaboration
                existingCollaboration.IsActive = true;
                existingCollaboration.CollaborationType = collaborationType;
                existingCollaboration.UpdatedAt = DateTime.UtcNow;
                existingCollaboration.UpdatedByUserId = addedByUserId;
                existingCollaboration.Notes = notes;
            }
            else
            {
                // Create new collaboration
                var collaboration = new ContactCollaborator
                {
                    ContactId = contactId,
                    CollaboratorUserId = collaboratorUserId,
                    CollaborationType = collaborationType,
                    AddedByUserId = addedByUserId,
                    AddedAt = DateTime.UtcNow,
                    IsActive = true,
                    Notes = notes
                };
                _context.ContactCollaborators.Add(collaboration);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {AddedByUserId} added user {CollaboratorUserId} as collaborator to contact {ContactId} with access level {AccessLevel}", 
                addedByUserId, collaboratorUserId, contactId, collaborationType);

            return true;
        }

        /// <summary>
        /// Updates a collaborator's access level on a contact
        /// </summary>
        public async Task<bool> UpdateCollaboratorAsync(
            Guid contactId, 
            Guid collaboratorUserId, 
            Guid updatedByUserId, 
            ContactCollaborationType newCollaborationType, 
            string? notes = null)
        {
            // Check if user has permission to update collaborators on this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, updatedByUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to update collaborator on contact {ContactId} without permission", 
                    updatedByUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to update collaborators on this contact");
            }

            // Find the collaboration
            var collaboration = await _context.ContactCollaborators
                .FirstOrDefaultAsync(cc => cc.ContactId == contactId && 
                                         cc.CollaboratorUserId == collaboratorUserId && 
                                         cc.IsActive);

            if (collaboration == null)
            {
                _logger.LogWarning("No active collaboration found for user {UserId} on contact {ContactId}", 
                    collaboratorUserId, contactId);
                return false;
            }

            // Update the collaboration
            var previousAccessLevel = collaboration.CollaborationType;
            collaboration.CollaborationType = newCollaborationType;
            collaboration.UpdatedAt = DateTime.UtcNow;
            collaboration.UpdatedByUserId = updatedByUserId;
            collaboration.Notes = notes ?? collaboration.Notes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UpdatedByUserId} updated collaborator {CollaboratorUserId} access level on contact {ContactId} from {PreviousAccessLevel} to {NewAccessLevel}", 
                updatedByUserId, collaboratorUserId, contactId, previousAccessLevel, newCollaborationType);

            return true;
        }

        /// <summary>
        /// Removes a collaborator from a contact
        /// </summary>
        public async Task<bool> RemoveCollaboratorAsync(Guid contactId, Guid collaboratorUserId, Guid removedByUserId, string? reason = null)
        {
            // Check if user has permission to remove collaborators from this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, removedByUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to remove collaborator from contact {ContactId} without permission", 
                    removedByUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to remove collaborators from this contact");
            }
            // Find the collaboration
            var collaboration = await _context.ContactCollaborators
                .FirstOrDefaultAsync(cc => cc.ContactId == contactId && 
                                         cc.CollaboratorUserId == collaboratorUserId && 
                                         cc.IsActive);

            if (collaboration == null)
            {
                _logger.LogWarning("No active collaboration found for user {UserId} on contact {ContactId}", 
                    collaboratorUserId, contactId);
                return false;
            }
            // Soft delete the collaboration
            collaboration.IsActive = false;
            collaboration.UpdatedAt = DateTime.UtcNow;
            collaboration.UpdatedByUserId = removedByUserId;
            collaboration.RemovedAt = DateTime.UtcNow;
            collaboration.RemovedByUserId = removedByUserId;
            collaboration.RemovalReason = reason;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {RemovedByUserId} removed collaborator {CollaboratorUserId} from contact {ContactId}", 
                removedByUserId, collaboratorUserId, contactId);

            return true;
        }

        /// <summary>
        /// Gets all active collaborators for a contact
        /// </summary>
        public async Task<IEnumerable<ContactCollaborator>> GetCollaboratorsAsync(Guid contactId, Guid requestingUserId)
        {
            // Check if user has access to this contact
            var hasAccess = await _contactAccessService.CanAccessContactAsync(contactId, requestingUserId);
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} attempted to view collaborators for contact {ContactId} without permission", 
                    requestingUserId, contactId);
                throw new UnauthorizedAccessException("You do not have permission to view collaborators for this contact");
            }
            var collaborators = await _context.ContactCollaborators
                .Include(cc => cc.CollaboratorUser)
                .Where(cc => cc.ContactId == contactId && cc.IsActive)
                .ToListAsync();

            return collaborators;
        }

        /// <summary>
        /// Gets all contacts where the specified user is a collaborator
        /// </summary>
        public async Task<IEnumerable<Contact>> GetCollaborationsForUserAsync(Guid userId, Guid requestingUserId, bool includeInactive = false)
        {
            // Users can only view their own collaborations unless they're an admin
            if (userId != requestingUserId)
            {
                var isAdmin = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == requestingUserId && ur.Role.Name == "Admin");
                
                if (!isAdmin)
                {
                    _logger.LogWarning("User {RequestingUserId} attempted to view collaborations for user {UserId} without permission", 
                        requestingUserId, userId);
                    throw new UnauthorizedAccessException("You do not have permission to view these collaborations");
                }
            }
            var query = _context.ContactCollaborators
                .Where(cc => cc.CollaboratorUserId == userId)
                .Include(cc => cc.Contact)
                    .ThenInclude(c => c.Emails)
                .Include(cc => cc.Contact)
                    .ThenInclude(c => c.Phones)
                .Select(cc => cc.Contact);

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query.ToListAsync();
        }
        #endregion

        #region Search & Filtering

        /// <summary>
        /// Searches contacts with access control
        /// </summary>
        public async Task<IEnumerable<Contact>> SearchContactsAsync(
            string searchTerm, 
            Guid requestingUserId, 
            int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));
            }

            var term = searchTerm.Trim().ToLowerInvariant();
            
            var query = _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Where(c => 
                    c.FirstName.ToLower().Contains(term) ||
                    c.LastName.ToLower().Contains(term) ||
                    c.CompanyName.ToLower().Contains(term) ||
                    c.Emails.Any(e => e.Address.ToLower().Contains(term)) ||
                    c.Phones.Any(p => p.Number.Contains(term)))
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Take(maxResults);

            var contacts = await query.ToListAsync();
            
            // Filter out contacts the user doesn't have access to
            var accessibleContacts = new List<Contact>();
            foreach (var contact in contacts)
            {
                if (await _contactAccessService.CanAccessContactAsync(contact.Id, requestingUserId))
                {
                    accessibleContacts.Add(contact);
                }
            }

            _logger.LogInformation("Found {Count} contacts matching search term '{SearchTerm}' for user {UserId}", 
                accessibleContacts.Count, searchTerm, requestingUserId);

            return accessibleContacts;
        }

        /// <summary>
        /// Gets contacts by organization with access control
        /// </summary>
        public async Task<IEnumerable<Contact>> GetContactsByOrganizationAsync(
            Guid organizationId, 
            Guid requestingUserId, 
            bool includeInactive = false)
        {
            var query = _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Where(c => c.OrganizationId == organizationId);

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }


            var contacts = await query.ToListAsync();
            
            // Filter out contacts the user doesn't have access to
            var accessibleContacts = new List<Contact>();
            foreach (var contact in contacts)
            {
                if (await _contactAccessService.CanAccessContactAsync(contact.Id, requestingUserId))
                {
                    accessibleContacts.Add(contact);
                }
            }

            _logger.LogInformation("Retrieved {Count} contacts for organization {OrganizationId} for user {UserId}", 
                accessibleContacts.Count, organizationId, requestingUserId);

            return accessibleContacts;
        }

        /// <summary>
        /// Gets contacts assigned to a specific user
        /// </summary>
        public async Task<IEnumerable<Contact>> GetContactsByUserAsync(
            Guid userId, 
            Guid requestingUserId, 
            bool includeCollaborations = true)
        {
            // If the requesting user is not the same as the target user, verify permissions
            if (userId != requestingUserId)
            {
                var isAdmin = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == requestingUserId && ur.Role.Name == "Admin");
                
                if (!isAdmin)
                {
                    _logger.LogWarning("User {RequestingUserId} attempted to access contacts for user {UserId} without permission", 
                        requestingUserId, userId);
                    throw new UnauthorizedAccessException("You do not have permission to view these contacts");
                }
            }


            var query = _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Where(c => c.AssignedToUserId == userId || c.OwnerUserId == userId);

            if (includeCollaborations)
            {
                query = query.Union(
                    _context.ContactCollaborators
                        .Where(cc => cc.CollaboratorUserId == userId && cc.IsActive)
                        .Select(cc => cc.Contact)
                        .Include(c => c.Emails)
                        .Include(c => c.Phones)
                );
            }


            var contacts = await query.ToListAsync();
            
            // Filter out contacts the user doesn't have access to
            var accessibleContacts = new List<Contact>();
            foreach (var contact in contacts)
            {
                if (await _contactAccessService.CanAccessContactAsync(contact.Id, requestingUserId))
                {
                    accessibleContacts.Add(contact);
                }
            }

            _logger.LogInformation("Retrieved {Count} contacts for user {UserId}", 
                accessibleContacts.Count, userId);

            return accessibleContacts;
        }

        #endregion
    }
}
