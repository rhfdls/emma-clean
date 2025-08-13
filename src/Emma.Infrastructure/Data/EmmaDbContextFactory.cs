using System;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Emma.Core.Interfaces.Repositories;
using Emma.Infrastructure.Data.Repositories;

namespace Emma.Infrastructure.Data
{
    /// <summary>
    /// Factory for creating instances of <see cref="EmmaDbContext"/>.
    /// This is primarily used for design-time operations like migrations.
    /// </summary>
    public class EmmaDbContextFactory : IDesignTimeDbContextFactory<EmmaDbContext>
    {
        /// <summary>
        /// Creates a new instance of <see cref="EmmaDbContext"/>.
        /// </summary>
        public EmmaDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<EmmaDbContext>();
            
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
            }

            optionsBuilder.UseNpgsql(connectionString, options =>
            {
                options.MigrationsAssembly(typeof(EmmaDbContext).Assembly.FullName);
                options.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Enable detailed errors and sensitive data logging in development
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
            
            // Configure logging
            optionsBuilder.LogTo(
                message => Console.WriteLine(message),
                new[] { DbLoggerCategory.Database.Command.Name },
                LogLevel.Information);

            return new EmmaDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a new instance of <see cref="EmmaDbContext"/> for testing purposes.
        /// </summary>
        public static EmmaDbContext CreateInMemoryDbContext(string databaseName = "EmmaTestDb")
        {
            var options = new DbContextOptionsBuilder<EmmaDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            return new EmmaDbContext(options);
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> to register the database context.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Emma database context to the service collection.
        /// </summary>
        public static IServiceCollection AddEmmaDbContext(
            this IServiceCollection services,
            IConfiguration configuration,
            string connectionStringName = "DefaultConnection",
            bool useInMemory = false)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            if (useInMemory)
            {
                services.AddDbContext<EmmaDbContext>(options =>
                    options.UseInMemoryDatabase("EmmaInMemoryDb"));
            }
            else
            {
                services.AddDbContext<EmmaDbContext>((serviceProvider, options) =>
                {
                    var connectionString = configuration.GetConnectionString(connectionStringName);
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException($"Connection string '{connectionStringName}' not found in configuration.");
                    }

                    options.UseNpgsql(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(EmmaDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                    });

                    // Only enable sensitive data logging in development
                    var env = serviceProvider.GetRequiredService<IHostEnvironment>();
                    if (env.IsDevelopment())
                    {
                        options.EnableSensitiveDataLogging();
                    }
                });
            }

            // Register the DbContext for dependency injection
            services.AddScoped<EmmaDbContext>();
            
            // Register repositories
            services.AddScoped<IInteractionRepository, InteractionRepository>();
            services.AddScoped<IContactRepository, ContactRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
