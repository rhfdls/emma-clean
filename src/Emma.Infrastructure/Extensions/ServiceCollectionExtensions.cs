using System;
using Emma.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEmmaDatabase(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment = false)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string 'DefaultConnection' not found.");
            }

            // Configure DbContext with retry policy
            services.AddDbContext<EmmaDbContext>((sp, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                        
                        // Enable vector search
                        npgsqlOptions.UseVector();
                    });

                if (isDevelopment)
                {
                    options.EnableSensitiveDataLogging();
                    options.LogTo(
                        message => sp.GetRequiredService<ILogger<EmmaDbContext>>().LogInformation(message),
                        LogLevel.Information);
                }
            });

            // Register repositories
            services.AddScoped<IContactRepository, ContactRepository>();
            services.AddScoped<IInteractionRepository, InteractionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // Register database initializer
            services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

            return services;
        }

        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, bool resetDatabase = false)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            
            try
            {
                var initializer = services.GetRequiredService<IDatabaseInitializer>();
                await initializer.InitializeDatabaseAsync(resetDatabase);
                await initializer.SeedDatabaseAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
    }
}
