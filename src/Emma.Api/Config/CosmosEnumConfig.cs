using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emma.Api.Config
{
    // SPRINT1: Cosmos DB enum config provider
    public class CosmosEnumConfig
    {
        private readonly CosmosClient _client;
        private readonly Container _container;
        private readonly string _databaseName;
        private readonly string _containerName;
#pragma warning disable CS0414
        private readonly string _partitionKey = "/type";
#pragma warning restore CS0414

        public CosmosEnumConfig(IConfiguration config)
        {
            var endpoint = config["COSMOSDB__ACCOUNTENDPOINT"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTENDPOINT");
            var key = config["COSMOSDB__ACCOUNTKEY"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ACCOUNTKEY");
            _databaseName = config["COSMOSDB__DATABASENAME"] ?? Environment.GetEnvironmentVariable("COSMOSDB__DATABASENAME") ?? "emma-agent";
            _containerName = config["COSMOSDB__ENUMCONTAINERNAME"] ?? Environment.GetEnvironmentVariable("COSMOSDB__ENUMCONTAINERNAME") ?? "enumConfig";
            _partitionKey = "/type";
            Console.WriteLine($"Cosmos endpoint: {endpoint}");
            Console.WriteLine($"Cosmos key: {(string.IsNullOrEmpty(key) ? "[NULL OR EMPTY]" : "[LOADED]")}");
            _client = new CosmosClient(endpoint, key);
            _container = _client.GetContainer(_databaseName, _containerName);
        }

        public async Task<List<dynamic>> GetEnumAsync(string type)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.type = @type").WithParameter("@type", type);
            var iterator = _container.GetItemQueryIterator<dynamic>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(type) });
            var results = new List<dynamic>();
            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                    results.Add(item);
                }
            }
            return results;
        }
    }
}
