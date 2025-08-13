using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System;
using System.IO;

namespace Emma.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Try to load configuration from the current directory.
            // If not found, also probe the API project's directory so EF CLI works from solution root.
            var currentDir = Directory.GetCurrentDirectory();
            var apiDir = Path.Combine(currentDir, "src", "Emma.Api");

            IConfigurationRoot BuildConfig(string basePath)
            {
                return new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();
            }

            IConfiguration config = BuildConfig(currentDir);

            // If no connection string resolved, try API directory as fallback base path
            string? connStr = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connStr) && Directory.Exists(apiDir))
            {
                config = BuildConfig(apiDir);
                connStr = config.GetConnectionString("DefaultConnection");
            }

            // Final fallback: environment variable convention ConnectionStrings__DefaultConnection
            connStr ??= Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new InvalidOperationException(
                    "AppDbContextFactory could not resolve a database connection string. " +
                    "Ensure appsettings.json is present (in current or src/Emma.Api directory) or set ConnectionStrings__DefaultConnection environment variable.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connStr);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
