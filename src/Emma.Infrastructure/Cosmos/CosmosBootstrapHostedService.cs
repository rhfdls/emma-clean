using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emma.Infrastructure.Config;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emma.Infrastructure.Cosmos;

public sealed class CosmosBootstrapHostedService : IHostedService
{
    private readonly ICosmosClientFactory _factory;
    private readonly IOptions<CosmosOptions> _options;
    private readonly ILogger<CosmosBootstrapHostedService> _logger;

    public CosmosBootstrapHostedService(ICosmosClientFactory factory, IOptions<CosmosOptions> options, ILogger<CosmosBootstrapHostedService> logger)
    {
        _factory = factory;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var (_, db) = await _factory.GetAsync(cancellationToken);
        var containers = new (string name, string partitionKey)[]
        {
            (_options.Value.Containers.Procedures, "/tenantId"),
            (_options.Value.Containers.ProcedureTraces, "/tenantId"),
            (_options.Value.Containers.ProcedureVersions, "/tenantId"),
            (_options.Value.Containers.ProcedureInsights, "/tenantId")
        };

        foreach (var (name, pk) in containers)
        {
            _logger.LogInformation("Cosmos ensure container {Container}", name);
            await db.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = name,
                PartitionKeyPath = pk
            }, throughput: null, cancellationToken: cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

