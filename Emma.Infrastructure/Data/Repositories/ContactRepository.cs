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
    /// Repository implementation for managing contacts in the database.
    /// </summary>
    public class ContactRepository : IContactRepository
    {
        private readonly EmmaDbContext _context;
        private readonly ILogger<ContactRepository> _logger;

        public ContactRepository(EmmaDbContext context, ILogger<ContactRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Contact?> GetByIdAsync(Guid id)
        {
            return await _context.Contacts
                .Include(c => c.Organization)
                .Include(c => c.Interactions)
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <inheritdoc />
        public async Task<List<Contact>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            return await _context.Contacts
                .Include(c => c.Organization)
                .Where(c => idList.Contains(c.Id))
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Contact>> FindAsync(Expression<Func<Contact, bool>> predicate)
        {
            return await _context.Contacts
                .Include(c => c.Organization)
                .Where(predicate)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task AddAsync(Contact contact)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            
            // Set timestamps if not already set
            if (contact.CreatedAt == default)
                contact.CreatedAt = DateTime.UtcNow;
                
            contact.UpdatedAt = DateTime.UtcNow;

            await _context.Contacts.AddAsync(contact);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Added contact {ContactId} ({Email})", contact.Id, contact.Email);
        }

        /// <inheritdoc />
        public void Update(Contact contact)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            
            contact.UpdatedAt = DateTime.UtcNow;
            _context.Entry(contact).State = EntityState.Modified;
            
            _logger.LogDebug("Updated contact {ContactId}", contact.Id);
        }

        /// <inheritdoc />
        public void Remove(Contact contact)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            
            _context.Contacts.Remove(contact);
            _logger.LogInformation("Removed contact {ContactId} ({Email})", contact.Id, contact.Email);
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
                _logger.LogError(ex, "Concurrency error while saving changes to contacts");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while saving changes to contacts");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to contacts");
                throw;
            }
        }
    }
}
