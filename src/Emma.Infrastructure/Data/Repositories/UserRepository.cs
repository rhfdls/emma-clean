using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Emma.Infrastructure.Data;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emma.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for managing users in the database.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly EmmaDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(EmmaDbContext context, ILogger<UserRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <inheritdoc />
        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));

            return await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        /// <inheritdoc />
        public async Task<User?> GetByExternalIdAsync(string provider, string providerId)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider cannot be null or whitespace.", nameof(provider));
                
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("Provider ID cannot be null or whitespace.", nameof(providerId));

            return await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.IdentityProvider == provider && u.IdentityProviderId == providerId);
        }

        /// <inheritdoc />
        public async Task<List<User>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            return await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Roles)
                .Where(u => idList.Contains(u.Id))
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<User>> FindAsync(Expression<Func<User, bool>> predicate)
        {
            return await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Roles)
                .Where(predicate)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task AddAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            // Set timestamps if not already set
            if (user.CreatedAt == default)
                user.CreatedAt = DateTime.UtcNow;
                
            user.UpdatedAt = DateTime.UtcNow;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Added user {UserId} ({Email})", user.Id, user.Email);
        }

        /// <inheritdoc />
        public void Update(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            user.UpdatedAt = DateTime.UtcNow;
            _context.Entry(user).State = EntityState.Modified;
            
            _logger.LogDebug("Updated user {UserId}", user.Id);
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while saving changes to users");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while saving changes to users");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to users");
                throw;
            }
        }
    }
}
