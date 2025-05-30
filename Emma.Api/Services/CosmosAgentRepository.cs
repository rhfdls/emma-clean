using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Services
{
    public class CosmosAgentRepository
    {
        private readonly Container _container;
        private readonly ILogger<CosmosAgentRepository> _logger;

        public CosmosAgentRepository(CosmosClient cosmosClient, string databaseName, string containerName, ILogger<CosmosAgentRepository> logger)
        {
            _container = cosmosClient.GetContainer(databaseName, containerName);
            _logger = logger;
        }

        public async Task<T> GetItemAsync<T>(string id, string partitionKey) where T : class
        {
            try
            {
                ItemResponse<T> response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Item not found in Cosmos DB. Id: {id}, PartitionKey: {partitionKey}");
                return null;
            }
        }

        public async Task<T> UpsertItemAsync<T>(T item, string partitionKey) where T : class
        {
            var response = await _container.UpsertItemAsync(item, new PartitionKey(partitionKey));
            return response.Resource;
        }

        public async Task DeleteItemAsync(string id, string partitionKey)
        {
            await _container.DeleteItemAsync<object>(id, new PartitionKey(partitionKey));
        }

        public async Task<IEnumerable<T>> QueryItemsAsync<T>(string queryString) where T : class
        {
            var query = _container.GetItemQueryIterator<T>(new QueryDefinition(queryString));
            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }
    }
}
