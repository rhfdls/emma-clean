using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            string endpoint = "https://localhost:8081";
            string key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            string databaseId = "EmmaDb";
            string containerId = "EmmaContainer";

            Console.WriteLine("Connecting to Cosmos DB...");
            
            using var client = new CosmosClient(endpoint, key);
            
            // Test connection
            var database = await client.CreateDatabaseIfNotExistsAsync(databaseId);
            var container = await database.Database.CreateContainerIfNotExistsAsync(
                id: containerId,
                partitionKeyPath: "/id",
                throughput: 400);

            Console.WriteLine($"Successfully connected to Cosmos DB database: {databaseId}");
            Console.WriteLine($"Created container: {containerId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }
    }
}
