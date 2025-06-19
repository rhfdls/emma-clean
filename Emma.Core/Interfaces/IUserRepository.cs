using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Emma.Models.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for user data access operations.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by their ID.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        Task<User?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Gets a user by their external provider ID.
        /// </summary>
        /// <param name="provider">The authentication provider name.</param>
        /// <param name="providerId">The user's ID from the external provider.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        Task<User?> GetByExternalIdAsync(string provider, string providerId);

        /// <summary>
        /// Gets multiple users by their IDs.
        /// </summary>
        /// <param name="ids">The IDs of the users to retrieve.</param>
        /// <returns>A list of users matching the provided IDs.</returns>
        Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// Gets all users matching the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A list of users that match the condition.</returns>
        Task<List<User>> FindAsync(Expression<Func<User, bool>> predicate);

        /// <summary>
        /// Adds a new user to the repository.
        /// </summary>
        /// <param name="user">The user to add.</param>
        Task AddAsync(User user);

        /// <summary>
        /// Updates an existing user in the repository.
        /// </summary>
        /// <param name="user">The user to update.</param>
        void Update(User user);

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task<int> SaveChangesAsync();
    }
}
