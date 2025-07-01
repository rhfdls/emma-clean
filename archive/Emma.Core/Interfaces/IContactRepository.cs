using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Emma.Models.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for contact data access operations.
    /// </summary>
    public interface IContactRepository
    {
        /// <summary>
        /// Gets a contact by its ID.
        /// </summary>
        /// <param name="id">The ID of the contact to retrieve.</param>
        /// <returns>The contact if found; otherwise, null.</returns>
        Task<Contact?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets multiple contacts by their IDs.
        /// </summary>
        /// <param name="ids">The IDs of the contacts to retrieve.</param>
        /// <returns>A list of contacts matching the provided IDs.</returns>
        Task<List<Contact>> GetByIdsAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// Gets all contacts matching the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A list of contacts that match the condition.</returns>
        Task<List<Contact>> FindAsync(Expression<Func<Contact, bool>> predicate);

        /// <summary>
        /// Adds a new contact to the repository.
        /// </summary>
        /// <param name="contact">The contact to add.</param>
        Task AddAsync(Contact contact);

        /// <summary>
        /// Updates an existing contact in the repository.
        /// </summary>
        /// <param name="contact">The contact to update.</param>
        void Update(Contact contact);

        /// <summary>
        /// Removes a contact from the repository.
        /// </summary>
        /// <param name="contact">The contact to remove.</param>
        void Remove(Contact contact);

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task<int> SaveChangesAsync();
    }
}
