using Emma.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for interaction data access operations.
    /// </summary>
    public interface IInteractionRepository
    {
        /// <summary>
        /// Gets a queryable collection of all interactions.
        /// </summary>
        IQueryable<Interaction> GetAll();

        /// <summary>
        /// Gets an interaction by its ID.
        /// </summary>
        /// <param name="id">The ID of the interaction to retrieve.</param>
        /// <returns>The interaction if found; otherwise, null.</returns>
        Task<Interaction?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets multiple interactions by their IDs.
        /// </summary>
        /// <param name="ids">The IDs of the interactions to retrieve.</param>
        /// <returns>A list of interactions matching the provided IDs.</returns>
        Task<List<Interaction>> GetByIdsAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// Gets interactions for a specific contact.
        /// </summary>
        /// <param name="contactId">The ID of the contact.</param>
        /// <param name="maxResults">The maximum number of interactions to return.</param>
        /// <returns>A list of interactions for the specified contact, ordered by most recent.</returns>
        Task<List<Interaction>> GetByContactIdAsync(Guid contactId, int maxResults = 50);

        /// <summary>
        /// Adds a new interaction to the repository.
        /// </summary>
        /// <param name="interaction">The interaction to add.</param>
        Task AddAsync(Interaction interaction);

        /// <summary>
        /// Updates an existing interaction in the repository.
        /// </summary>
        /// <param name="interaction">The interaction to update.</param>
        void Update(Interaction interaction);

        /// <summary>
        /// Removes an interaction from the repository.
        /// </summary>
        /// <param name="interaction">The interaction to remove.</param>
        void Remove(Interaction interaction);

        /// <summary>
        /// Counts the number of interactions matching the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter interactions.</param>
        /// <returns>The number of matching interactions.</returns>
        Task<int> CountAsync(Expression<Func<Interaction, bool>>? predicate = null);

        /// <summary>
        /// Executes the query and returns the results as a list asynchronously.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A list of interactions.</returns>
        Task<List<Interaction>> ToListAsync(IQueryable<Interaction> query);

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task<int> SaveChangesAsync();
    }
}
