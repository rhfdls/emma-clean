using Emma.Models.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces.Repositories
{
    public interface IInteractionRepository
    {
        Task<Interaction?> GetByIdAsync(Guid id);
        Task<List<Interaction>> GetByContactIdAsync(Guid contactId, int maxResults = 50);
        Task AddAsync(Interaction interaction);
        Task<int> SaveChangesAsync();
    }
}
