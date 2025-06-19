using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Emma.Infrastructure.Data;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for managing interactions in the database.
    /// </summary>
    public class InteractionRepository : IInteractionRepository
    {
        private readonly EmmaDbContext _context;
        private readonly ILogger<InteractionRepository> _logger;

        public InteractionRepository(EmmaDbContext context, ILogger<InteractionRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public IQueryable<Interaction> GetAll()
        {
            return _context.Interactions
                .Include(i => i.Contact)
                .Include(i => i.AssignedTo)
                .Include(i => i.Participants)
                .Include(i => i.RelatedEntities)
                .Include(i => i.ActionItems)
                .AsQueryable();
        }

        /// <inheritdoc />
        public async Task<Interaction?> GetByIdAsync(Guid id)
        {
            return await _context.Interactions
                .Include(i => i.Contact)
                .Include(i => i.AssignedTo)
                .Include(i => i.Participants)
                .Include(i => i.RelatedEntities)
                .Include(i => i.ActionItems)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <inheritdoc />
        public async Task<List<Interaction>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            return await _context.Interactions
                .Include(i => i.Contact)
                .Include(i => i.AssignedTo)
                .Include(i => i.Participants)
                .Include(i => i.RelatedEntities)
                .Include(i => i.ActionItems)
                .Where(i => idList.Contains(i.Id))
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Interaction>> GetByContactIdAsync(Guid contactId, int maxResults = 50)
        {
            return await _context.Interactions
                .Where(i => i.ContactId == contactId)
                .OrderByDescending(i => i.CreatedAt)
                .Take(maxResults)
                .Include(i => i.Contact)
                .Include(i => i.AssignedTo)
                .Include(i => i.Participants)
                .Include(i => i.RelatedEntities)
                .Include(i => i.ActionItems)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task AddAsync(Interaction interaction)
        {
            if (interaction == null) throw new ArgumentNullException(nameof(interaction));
            
            // Set timestamps if not already set
            if (interaction.CreatedAt == default)
                interaction.CreatedAt = DateTime.UtcNow;
                
            interaction.UpdatedAt = DateTime.UtcNow;

            await _context.Interactions.AddAsync(interaction);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Added interaction {InteractionId} for contact {ContactId}", 
                interaction.Id, interaction.ContactId);
        }

        /// <inheritdoc />
        public void Update(Interaction interaction)
        {
            if (interaction == null) throw new ArgumentNullException(nameof(interaction));
            
            interaction.UpdatedAt = DateTime.UtcNow;
            _context.Entry(interaction).State = EntityState.Modified;
            
            _logger.LogDebug("Updated interaction {InteractionId}", interaction.Id);
        }

        /// <inheritdoc />
        public void Remove(Interaction interaction)
        {
            if (interaction == null) throw new ArgumentNullException(nameof(interaction));
            
            _context.Interactions.Remove(interaction);
            _logger.LogInformation("Removed interaction {InteractionId}", interaction.Id);
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(Expression<Func<Interaction, bool>>? predicate = null)
        {
            return predicate != null 
                ? await _context.Interactions.CountAsync(predicate) 
                : await _context.Interactions.CountAsync();
        }

        /// <inheritdoc />
        public async Task<List<Interaction>> ToListAsync(IQueryable<Interaction> query)
        {
            return await query.ToListAsync();
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
                _logger.LogError(ex, "Concurrency error while saving changes to interactions");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while saving changes to interactions");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to interactions");
                throw;
            }
        }
    }
}
