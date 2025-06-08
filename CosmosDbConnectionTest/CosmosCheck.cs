using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

class CosmosCheck
{
    // CosmosDB connection parameters from .env
    private static readonly string EndpointUrl = "https://emma-cosmos.documents.azure.com:443/";
    private static readonly string PrimaryKey = "LgUJy8nQWgJ1Cl3SQB0CNLPhNcpS70tAMRsdeIXpZBlncpveoLaZX9RRtjpJl6ETXsRZrHFQ2J5kACDbrjmIoA==";
    private static readonly string DatabaseId = "emma-agent";
    private static readonly string ContainerId = "messages";

    static async Task Main()
    {
        Console.WriteLine("====================================================");
        Console.WriteLine("  Emma AI Platform - CosmosDB Connection Validator");
        Console.WriteLine("====================================================");
        Console.WriteLine($"Endpoint: {EndpointUrl}");
        Console.WriteLine($"Database: {DatabaseId}");
        Console.WriteLine($"Container: {ContainerId}");
        Console.WriteLine();

        try
        {
            Console.WriteLine("Connecting to CosmosDB...");
            using var cosmosClient = new CosmosClient(EndpointUrl, PrimaryKey);
            
            // Try to read the database
            Console.WriteLine("Checking database access...");
            DatabaseResponse dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
            Console.WriteLine($"✓ Successfully connected to database: {dbResponse.Database.Id}");
            
            // Try to read the container
            Console.WriteLine("Checking container access...");
            ContainerResponse containerResponse = await dbResponse.Database.CreateContainerIfNotExistsAsync(
                ContainerId, "/id");
            Console.WriteLine($"✓ Successfully accessed container: {containerResponse.Container.Id}");
            
            Console.WriteLine();
            Console.WriteLine("✅ CONNECTION SUCCESSFUL!");
            Console.WriteLine("The Emma AI Platform can use CosmosDB for context models.");
        }
        catch (CosmosException cosmosEx)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ COSMOS DB ERROR: {cosmosEx.StatusCode}");
            Console.WriteLine(cosmosEx.Message);
            Console.WriteLine();
            Console.WriteLine("Troubleshooting tips:");
            Console.WriteLine("1. Verify connection string in .env is correct");
            Console.WriteLine("2. Check if your IP is allowed in CosmosDB firewall");
            Console.WriteLine("3. Verify the database/container exists or you have permissions to create them");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ ERROR: {ex.GetType().Name}");
            Console.WriteLine(ex.Message);
        }
        
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
