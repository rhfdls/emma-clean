using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace EmmaCosmosDbTest
{
    public class Program
    {
        // CosmosDB connection parameters
        private static readonly string EndpointUrl = "https://emma-cosmos.documents.azure.com:443/";
        private static readonly string PrimaryKey = "LgUJy8nQWgJ1Cl3SQB0CNLPhNcpS70tAMRsdeIXpZBlncpveoLaZX9RRtjpJl6ETXsRZrHFQ2J5kACDbrjmIoA==";
        private static readonly string DatabaseId = "emma-agent";
        private static readonly string ContainerId = "messages";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("  Emma AI Platform - CosmosDB Connection Test");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Endpoint: {EndpointUrl}");
            Console.WriteLine($"Database: {DatabaseId}");
            Console.WriteLine($"Container: {ContainerId}");
            Console.WriteLine();

            try
            {
                // Initialize the CosmosClient
                Console.WriteLine("Connecting to CosmosDB...");
                var cosmosClient = new CosmosClient(EndpointUrl, PrimaryKey);
                
                // Verify database access
                Console.WriteLine("Checking database access...");
                var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
                Console.WriteLine($"Successfully connected to database: {database.Database.Id}");
                
                // Verify container access
                Console.WriteLine("Checking container access...");
                var container = await database.Database.CreateContainerIfNotExistsAsync(
                    id: ContainerId,
                    partitionKeyPath: "/id"
                );
                Console.WriteLine($"Successfully accessed container: {container.Container.Id}");
                
                // Create a test document
                Console.WriteLine();
                Console.WriteLine("Creating test document...");
                var testId = Guid.NewGuid().ToString();
                var testDocument = new
                {
                    id = testId,
                    contactId = "test-contact-123",
                    type = "rolling_summary",
                    version = "1.0",
                    createdAt = DateTime.UtcNow,
                    summary = "This is a test rolling summary for the Emma AI Platform context models.",
                    metadata = new
                    {
                        documentCount = 5,
                        lastInteractionDate = DateTime.UtcNow.AddDays(-1)
                    }
                };
                
                var createResponse = await container.Container.CreateItemAsync(
                    testDocument, 
                    new PartitionKey(testId)
                );
                Console.WriteLine($"Successfully created test document with id: {testId}");
                Console.WriteLine($"Request charge: {createResponse.RequestCharge} RUs");
                
                // Read the test document back
                Console.WriteLine();
                Console.WriteLine("Reading test document...");
                var readResponse = await container.Container.ReadItemAsync<dynamic>(
                    testId, 
                    new PartitionKey(testId)
                );
                Console.WriteLine($"Successfully read test document");
                Console.WriteLine($"Request charge: {readResponse.RequestCharge} RUs");
                
                // Clean up the test document
                Console.WriteLine();
                Console.WriteLine("Deleting test document...");
                var deleteResponse = await container.Container.DeleteItemAsync<dynamic>(
                    testId, 
                    new PartitionKey(testId)
                );
                Console.WriteLine($"Successfully deleted test document");
                
                Console.WriteLine();
                Console.WriteLine("✅ CONNECTION SUCCESSFUL!");
                Console.WriteLine("The Emma AI Platform can successfully connect to CosmosDB");
                Console.WriteLine("Ready to implement context models (rolling summaries, embeddings, state)");
            }
            catch (CosmosException cosmosEx)
            {
                Console.WriteLine();
                Console.WriteLine($"❌ COSMOS DB ERROR: {cosmosEx.StatusCode}");
                Console.WriteLine(cosmosEx.Message);
                Console.WriteLine();
                Console.WriteLine("Troubleshooting tips:");
                Console.WriteLine("1. Verify the connection string/key in .env is correct");
                Console.WriteLine("2. Check if your IP is allowed in the CosmosDB firewall");
                Console.WriteLine("3. Verify the database and container exist");
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
}
