namespace Emma.Core.Interfaces.ProceduralMemory;

public interface IProceduralMemoryService
{
    System.Threading.Tasks.Task<ReplayPlan?> TryGetReplayAsync(ProcedureLookupRequest request, System.Threading.CancellationToken ct);
    System.Threading.Tasks.Task CaptureTraceAsync(string traceId, object tracePayload, System.Threading.CancellationToken ct);
    System.Threading.Tasks.Task PromoteAsync(string traceId, object options, System.Threading.CancellationToken ct);
}
