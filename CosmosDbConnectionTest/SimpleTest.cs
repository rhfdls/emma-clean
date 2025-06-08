using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace CosmosDbConnectionTest
{
    public class SimpleTest
    {
        public static async Task RunTest()
        {
            // Load connection details directly
            string EndpointUrl = "https://emma-cosmos.documents.azure.com:443/";
            string PrimaryKey = "LgUJy8nQWgJ1Cl3SQB0CNLPhNcpS70tAMRsdeIXpZBlncpveoLaZX9RRtjpJl6ETXsRZrHFQ2J5kACDbrjmIoA==";
            string DatabaseId = "emma-agent";
            string ContainerId = "messages";

            Console.WriteLine("=== Emma AI Platform - CosmosDB Connection Test ===");
            Console.WriteLine($"Connecting to: {EndpointUrl}");
            Console.WriteLine($"Database: {DatabaseId}");
            Console.WriteLine($"Container: {ContainerId}");

            try
            {
                // Initialize CosmosClient
                using CosmosClient cosmosClient = new CosmosClient(EndpointUrl, PrimaryKey);
                Console.WriteLine("✓ Successfully initialized CosmosClient");
                
                // Verify database access
                Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
                Console.WriteLine($"✓ Successfully connected to database: {database.Id}");
                
                // Verify container access
                Container container = await database.CreateContainerIfNotExistsAsync(ContainerId, "/id");
                Console.WriteLine($"✓ Successfully connected to container: {container.Id}");
                
                Console.WriteLine("\n✅ CosmosDB CONNECTION SUCCESSFUL!");
                Console.WriteLine("The Emma AI Platform can connect to CosmosDB for context models.");
            }
            catch (CosmosException cosmosEx)
            {
                Console.WriteLine($"\n❌ CosmosDB ERROR: {cosmosEx.StatusCode} - {cosmosEx.Message}");
                Console.WriteLine("\nTroubleshooting tips:");
                Console.WriteLine("1. Verify the connection string and key in .env are correct");
                Console.WriteLine("2. Check if your IP is allowed in the CosmosDB firewall");
                Console.WriteLine("3. Confirm the database and container exist");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERROR: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }
}
