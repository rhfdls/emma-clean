using System.Threading.Tasks;

namespace Emma.Infrastructure.Data
{
    public interface IDatabaseInitializer
    {
        /// <summary>
        /// Initializes the database by applying all pending migrations and seeding initial data.
        /// </summary>
        Task InitializeDatabaseAsync(bool resetDatabase = false);

        /// <summary>
        /// Seeds the database with initial data.
        /// </summary>
        Task SeedDatabaseAsync();
    }
}
