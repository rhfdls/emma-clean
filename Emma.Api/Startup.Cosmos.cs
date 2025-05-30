using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;

namespace Emma.Api
{
    public static class CosmosStartupExtensions
    {
        public static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration)
        {
            // Debug output for environment variables (direct from Environment)
            Console.WriteLine("[DEBUG] COSMOSDB__ACCOUNTENDPOINT: " + Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT"));
            Console.WriteLine("[DEBUG] COSMOSDB__ACCOUNTKEY: " + Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY"));
            Console.WriteLine("[DEBUG] COSMOSDB__DATABASENAME: " + Environment.GetEnvironmentVariable("COSMOSDB__DATABASENAME"));
            Console.WriteLine("[DEBUG] COSMOSDB__CONTAINERNAME: " + Environment.GetEnvironmentVariable("COSMOSDB__CONTAINERNAME"));

            var endpoint = Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT");
            var key = Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY");
            var databaseName = Environment.GetEnvironmentVariable("COSMOSDB__DATABASENAME");
            var containerName = Environment.GetEnvironmentVariable("COSMOSDB__CONTAINERNAME");

            services.AddSingleton(s => new CosmosClient(endpoint, key));
            services.AddSingleton(s =>
            {
                var client = s.GetRequiredService<CosmosClient>();
                var logger = s.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Services.CosmosAgentRepository>>();
                return new Services.CosmosAgentRepository(client, databaseName, containerName, logger);
            });
            return services;
        }
    }
}
