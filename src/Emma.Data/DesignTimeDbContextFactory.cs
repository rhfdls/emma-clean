using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;
using DotNetEnv;

namespace Emma.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Load environment variables from .env file
            Env.Load("../.env");
            
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<AppDbContext>();
            var envConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql");
            var connectionString = !string.IsNullOrWhiteSpace(envConn)
                ? envConn
                : configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("PostgreSql");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("No PostgreSql connection string found in environment variables or appsettings.json.");

            builder.UseNpgsql(connectionString);

            return new AppDbContext(builder.Options);
        }
    }
}
