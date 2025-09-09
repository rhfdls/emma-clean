using System.Threading;
using System.Threading.Tasks;
using Emma.Infrastructure.Config;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Emma.Infrastructure.Cosmos;

public sealed class CosmosClientFactory : ICosmosClientFactory
{
    private readonly CosmosOptions _opt;
    private CosmosClient? _client;
    private Database? _db;

    public CosmosClientFactory(IOptions<CosmosOptions> opt)
    {
        _opt = opt.Value;
    }

    public async Task<(CosmosClient, Database)> GetAsync(CancellationToken ct)
    {
        if (_client is null)
        {
            _client = new CosmosClient(_opt.Endpoint, _opt.Key, new CosmosClientOptions { AllowBulkExecution = true });
            _db = await _client.CreateDatabaseIfNotExistsAsync(_opt.DatabaseId, throughput: null, cancellationToken: ct);
        }
        return (_client!, _db!);
    }
}
