using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Emma.Infrastructure.Config;
using Emma.Core.Interfaces.ProceduralMemory;

namespace Emma.Infrastructure.Cosmos;

public sealed class ProceduresRepository : IProceduresRepository
{
    private readonly ICosmosClientFactory _factory;
    private readonly CosmosOptions _opt;

    public ProceduresRepository(ICosmosClientFactory factory, IOptions<CosmosOptions> opt)
    {
        _factory = factory;
        _opt = opt.Value;
    }

    public async Task<ReplayPlan?> TryFindAsync(System.Guid tenantId, string actionType, string channel, string? industry, System.Guid? organizationId, bool useIndustry, CancellationToken ct)
    {
        var (_, db) = await _factory.GetAsync(ct);
        var container = db.GetContainer(_opt.Containers.Procedures);

        // SPRINT2: Phase0 — org-specific first when industry filtering is enabled
        if (useIndustry && !string.IsNullOrWhiteSpace(industry))
        {
            if (organizationId is System.Guid orgId && orgId != System.Guid.Empty)
            {
                var qOrg = new QueryDefinition(
                    "SELECT TOP 1 * FROM c WHERE c.tenantId = @tid AND c.organizationId = @oid AND c.industry = @ind AND c.actionType = @at AND c.channel = @ch AND c.enabled = true ORDER BY c.version DESC")
                    .WithParameter("@tid", tenantId)
                    .WithParameter("@oid", orgId)
                    .WithParameter("@ind", industry)
                    .WithParameter("@at", actionType)
                    .WithParameter("@ch", channel);

                using (var feedOrg = container.GetItemQueryIterator<CompiledProcedure>(qOrg, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tenantId.ToString()) }))
                {
                    if (feedOrg.HasMoreResults)
                    {
                        var page = await feedOrg.ReadNextAsync(ct);
                        var proc = page.Resource.FirstOrDefault();
                        if (proc is not null)
                        {
                            return new ReplayPlan(proc.id, proc.version, proc.steps, proc.parameters, RequiresValidation: true);
                        }
                    }
                }
            }

            // Tenant-level industry match
            var qInd = new QueryDefinition(
                "SELECT TOP 1 * FROM c WHERE c.tenantId = @tid AND c.industry = @ind AND c.actionType = @at AND c.channel = @ch AND c.enabled = true ORDER BY c.version DESC")
                .WithParameter("@tid", tenantId)
                .WithParameter("@ind", industry)
                .WithParameter("@at", actionType)
                .WithParameter("@ch", channel);

            using (var feedInd = container.GetItemQueryIterator<CompiledProcedure>(qInd, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tenantId.ToString()) }))
            {
                if (feedInd.HasMoreResults)
                {
                    var page = await feedInd.ReadNextAsync(ct);
                    var proc = page.Resource.FirstOrDefault();
                    if (proc is not null)
                    {
                        return new ReplayPlan(proc.id, proc.version, proc.steps, proc.parameters, RequiresValidation: true);
                    }
                }
            }
        }

        // Legacy behavior (no industry filtering)
        var q = new QueryDefinition(
            "SELECT TOP 1 * FROM c WHERE c.tenantId = @tid AND c.actionType = @at AND c.channel = @ch AND c.enabled = true ORDER BY c.version DESC")
            .WithParameter("@tid", tenantId)
            .WithParameter("@at", actionType)
            .WithParameter("@ch", channel);

        using (var feed = container.GetItemQueryIterator<CompiledProcedure>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tenantId.ToString()) }))
        {
            if (feed.HasMoreResults)
            {
                var page = await feed.ReadNextAsync(ct);
                var proc = page.Resource.FirstOrDefault();
                if (proc is not null)
                {
                    return new ReplayPlan(proc.id, proc.version, proc.steps, proc.parameters, RequiresValidation: true);
                }
            }
        }
        return null;
    }

    public async Task UpsertAsync(CompiledProcedure doc, CancellationToken ct)
    {
        var (_, db) = await _factory.GetAsync(ct);
        var container = db.GetContainer(_opt.Containers.Procedures);
        await container.UpsertItemAsync(doc, new PartitionKey(doc.tenantId.ToString()), cancellationToken: ct);
    }
}

