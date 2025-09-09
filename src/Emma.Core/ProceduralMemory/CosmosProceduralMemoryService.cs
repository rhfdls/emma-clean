using System;
using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces.ProceduralMemory;

namespace Emma.Core.ProceduralMemory;

public sealed class CosmosProceduralMemoryService : IProceduralMemoryService
{
    // Phase 0 placeholder: decouple Core from Infrastructure.
    // The real Cosmos-backed implementation will live in Emma.Infrastructure and be wired via DI.
    public CosmosProceduralMemoryService() { }

    public Task<ReplayPlan?> TryGetReplayAsync(ProcedureLookupRequest r, CancellationToken ct)
        => Task.FromResult<ReplayPlan?>(null);

    public Task CaptureTraceAsync(string traceId, object tracePayload, CancellationToken ct)
    {
        // No-op in Phase 0 core placeholder
        return Task.CompletedTask;
    }

    public Task PromoteAsync(string traceId, object options, CancellationToken ct)
        => Task.CompletedTask; // Phase 3
}

