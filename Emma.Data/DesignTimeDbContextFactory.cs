using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;

namespace Emma.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<AppDbContext>();
            var envConn = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql");
            var connectionString = !string.IsNullOrWhiteSpace(envConn)
                ? envConn
                : configuration.GetConnectionString("PostgreSql");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("No PostgreSql connection string found in environment variables or appsettings.json.");

            builder.UseNpgsql(connectionString);

            return new AppDbContext(builder.Options);
        }
    }
}
