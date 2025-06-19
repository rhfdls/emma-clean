using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Emma.Core.Services;
using Microsoft.Extensions.Options;
using Emma.Models.Interfaces;
using Emma.Data;

namespace Emma.IntegrationTests.Fixtures
{
    public class TimeSimulationTestFixture : WebApplicationFactory<Program>
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IAppDbContext DbContext { get; private set; }
        public TimeSimulatorService TimeSimulator { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Configure in-memory database for testing
                services.AddDbContext<IAppDbContext, AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TimeSimulationTests");
                });

                // Configure test time simulation options
                services.Configure<TimeSimulationOptions>(options =>
                {
                    options.DefaultTimeScale = 1.0;
                    options.StartPaused = true;
                    options.MinTimeScale = 0.1;
                    options.MaxTimeScale = 1000.0;
                });

                // Build the service provider
                var sp = services.BuildServiceProvider();
                
                // Initialize database with test data
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<AppDbContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<TimeSimulationTestFixture>>();

                    // Ensure the database is created
                    db.Database.EnsureCreated();

                    try
                    {
                        // Seed the database with test data
                        TestDataSeeder.SeedTestData(db);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred seeding the test database. Error: {Message}", ex.Message);
                    }
                }
            });
        }


        public async Task InitializeAsync()
        {
            var scope = Services.CreateScope();
            ServiceProvider = scope.ServiceProvider;
            
            // Get required services
            DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
            TimeSimulator = ServiceProvider.GetRequiredService<TimeSimulatorService>();
            
            // Ensure database is created and seeded
            await DbContext.Database.EnsureCreatedAsync();
            
            // Start the time simulator if not already started
            if (TimeSimulator is IHostedService hostedService)
            {
                await hostedService.StartAsync(CancellationToken.None);
            }
        }

        public new async Task DisposeAsync()
        {
            // Stop the time simulator
            if (TimeSimulator is IHostedService hostedService)
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
            
            // Clean up the database
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.DisposeAsync();
            
            // Dispose the service provider if it implements IDisposable
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
