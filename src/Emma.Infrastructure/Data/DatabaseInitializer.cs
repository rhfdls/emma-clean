using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Emma.Models.Models;

namespace Emma.Infrastructure.Data
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly EmmaDbContext _context;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(EmmaDbContext context, ILogger<DatabaseInitializer> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeDatabaseAsync(bool resetDatabase = false)
        {
            try
            {
                _logger.LogInformation("Initializing database...");

                if (resetDatabase)
                {
                    _logger.LogWarning("Resetting database as requested...");
                    await _context.Database.EnsureDeletedAsync();
                }

                // This will create the database if it doesn't exist and apply all pending migrations
                await _context.Database.EnsureCreatedAsync();

                // Apply any pending SQL scripts or additional schema changes
                await ApplySchemaUpdatesAsync();

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        public async Task SeedDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Seeding database...");

                // Only seed if no organizations exist
                if (!await _context.Organizations.AnyAsync())
                {
                    await SeedOrganizationsAsync();
                    await _context.SaveChangesAsync();
                }

                // Only seed if no users exist
                if (!await _context.Users.AnyAsync())
                {
                    await SeedUsersAsync();
                    await _context.SaveChangesAsync();
                }

                // Only seed if no contacts exist
                if (!await _context.Contacts.AnyAsync())
                {
                    await SeedContactsAsync();
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private async Task ApplySchemaUpdatesAsync()
        {
            try
            {
                // Create PostgreSQL extensions
                await _context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS "uuid-ossp"");
                await _context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector");

                // Add any additional schema updates here
                // Example: await _context.Database.ExecuteSqlRawAsync("ALTER TABLE...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying schema updates");
                throw;
            }
        }

        private async Task SeedOrganizationsAsync()
        {
            // Add default organization
            var defaultOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Default Organization",
                Domain = "example.com",
                TimeZone = "Eastern Standard Time",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Organizations.AddAsync(defaultOrg);
        }

        private async Task SeedUsersAsync()
        {
            var defaultOrg = await _context.Organizations.FirstOrDefaultAsync();
            if (defaultOrg == null)
            {
                throw new InvalidOperationException("No organization found for user seeding");
            }

            // Add default admin user
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                IsActive = true,
                OrganizationId = defaultOrg.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(adminUser);
        }

        private async Task SeedContactsAsync()
        {
            var defaultOrg = await _context.Organizations.FirstOrDefaultAsync();
            if (defaultOrg == null)
            {
                throw new InvalidOperationException("No organization found for contact seeding");
            }

            // Add sample contacts for testing
            var contacts = new[]
            {
                new Contact
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = defaultOrg.Id,
                    FirstName = "John",
                    LastName = "Doe",
                    RelationshipState = RelationshipState.Lead,
                    Emails = new List<EmailAddress> { new() { Address = "john.doe@example.com", Type = "work", IsPrimary = true } },
                    Phones = new List<PhoneNumber> { new() { Number = "+15551234567", Type = "mobile", IsPrimary = true } },
                    Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", PostalCode = "12345", Country = "USA" },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Contact
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = defaultOrg.Id,
                    FirstName = "Jane",
                    LastName = "Smith",
                    RelationshipState = RelationshipState.Client,
                    Emails = new List<EmailAddress> { new() { Address = "jane.smith@example.com", Type = "work", IsPrimary = true } },
                    Phones = new List<PhoneNumber> { new() { Number = "+15557654321", Type = "mobile", IsPrimary = true } },
                    Address = new Address { Street = "456 Oak Ave", City = "Somewhere", State = "NY", PostalCode = "54321", Country = "USA" },
                    IsActiveClient = true,
                    ClientSince = DateTime.UtcNow.AddMonths(-3),
                    CreatedAt = DateTime.UtcNow.AddMonths(-3),
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _context.Contacts.AddRangeAsync(contacts);
        }
    }
}
