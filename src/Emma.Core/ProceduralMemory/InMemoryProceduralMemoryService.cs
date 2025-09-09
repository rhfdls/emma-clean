using Emma.Core.Interfaces.ProceduralMemory;

namespace Emma.Core.ProceduralMemory;

public sealed class InMemoryProceduralMemoryService : IProceduralMemoryService
{
    public System.Threading.Tasks.Task<ReplayPlan?> TryGetReplayAsync(ProcedureLookupRequest r, System.Threading.CancellationToken ct)
        => System.Threading.Tasks.Task.FromResult<ReplayPlan?>(null);

    public System.Threading.Tasks.Task CaptureTraceAsync(string traceId, object payload, System.Threading.CancellationToken ct)
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task PromoteAsync(string traceId, object options, System.Threading.CancellationToken ct)
        => System.Threading.Tasks.Task.CompletedTask;
}
