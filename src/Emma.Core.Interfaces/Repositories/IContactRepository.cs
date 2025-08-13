using Emma.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces.Repositories
{
    public interface IContactRepository
    {
        Task<Contact?> GetByIdAsync(Guid id);
        Task<List<Contact>> GetByIdsAsync(IEnumerable<Guid> ids);
        Task<List<Contact>> FindAsync(Expression<Func<Contact, bool>> predicate);
        Task AddAsync(Contact contact);
        void Update(Contact contact);
        void Remove(Contact contact);
        Task<int> SaveChangesAsync();
    }
}
