using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Emma.Infrastructure.Cosmos;

public interface ICosmosClientFactory
{
    Task<(CosmosClient Client, Database Db)> GetAsync(CancellationToken ct);
}
