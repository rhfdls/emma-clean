using Emma.Data.Models;
using Emma.Data.Enums;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for Contact data access and management service
    /// </summary>
    public interface IContactService
    {
        /// <summary>
        /// Gets contacts by their relationship state(s) within an organization
        /// </summary>
        Task<IEnumerable<Contact>> GetContactsByRelationshipStateAsync(
            Guid organizationId, 
            IEnumerable<RelationshipState> relationshipStates);

        /// <summary>
        /// Gets a contact by ID
        /// </summary>
        Task<Contact?> GetContactByIdAsync(Guid contactId);

        /// <summary>
        /// Gets all contacts for an organization
        /// </summary>
        Task<IEnumerable<Contact>> GetContactsByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Creates a new contact
        /// </summary>
        Task<Contact> CreateContactAsync(Contact contact);

        /// <summary>
        /// Updates an existing contact
        /// </summary>
        Task<Contact> UpdateContactAsync(Contact contact);

        /// <summary>
        /// Deletes a contact
        /// </summary>
        Task DeleteContactAsync(Guid contactId);

        /// <summary>
        /// Searches contacts by various criteria
        /// </summary>
        Task<IEnumerable<Contact>> SearchContactsAsync(
            Guid organizationId,
            string? searchTerm = null,
            IEnumerable<RelationshipState>? relationshipStates = null,
            IEnumerable<string>? specialties = null,
            IEnumerable<string>? serviceAreas = null,
            decimal? minRating = null,
            bool? isPreferred = null,
            int? maxResults = null);
    }
}
