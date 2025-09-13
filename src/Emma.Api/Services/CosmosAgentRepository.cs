using System.Threading.Tasks;

namespace Emma.Api.Services
{
    /// <summary>
    /// Minimal stub used by CosmosDbConnectivityTests. In real code, this would wrap a Cosmos client.
    /// </summary>
    public class CosmosAgentRepository
    {
        public Task QueryItemsAsync<T>(string query)
        {
            // This stub simulates a successful call path for connectivity tests.
            // Replace with actual Cosmos DB SDK calls in the real implementation.
            return Task.CompletedTask;
        }
    }
}
