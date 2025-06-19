using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Emma.Models.Interfaces;
using Emma.Data;

namespace Emma.IntegrationTests.Fixtures;

public class TestDatabaseFixture : IDisposable
{
    public AppDbContext DbContext { get; private set; }
    private readonly IServiceProvider _serviceProvider;

    public TestDatabaseFixture()
    {
        // Set up the test database
        var services = new ServiceCollection();
        
        // Configure in-memory database
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase("EmmaTestDb_" + Guid.NewGuid());
            options.EnableSensitiveDataLogging();
        });

        // Add other services needed for testing
        // services.AddScoped<ISomeService, SomeService>();

        _serviceProvider = services.BuildServiceProvider();
        
        // Create database and apply migrations
        DbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        DbContext.Database.EnsureCreated();
        
        // Seed test data
        TestDataSeeder.SeedTestData(DbContext).Wait();
    }

    public void Dispose()
    {
        // Clean up the database after tests
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
