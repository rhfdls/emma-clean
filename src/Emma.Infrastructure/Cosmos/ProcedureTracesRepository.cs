using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Emma.Infrastructure.Config;

namespace Emma.Infrastructure.Cosmos;

public sealed class ProcedureTracesRepository : IProcedureTracesRepository
{
    private readonly ICosmosClientFactory _factory;
    private readonly CosmosOptions _opt;

    public ProcedureTracesRepository(ICosmosClientFactory factory, IOptions<CosmosOptions> opt)
    {
        _factory = factory;
        _opt = opt.Value;
    }

    public async Task CaptureAsync(ProcedureTrace doc, CancellationToken ct)
    {
        var (_, db) = await _factory.GetAsync(ct);
        var container = db.GetContainer(_opt.Containers.ProcedureTraces);
        await container.CreateItemAsync(doc, new PartitionKey(doc.tenantId.ToString()), cancellationToken: ct);
    }
}
