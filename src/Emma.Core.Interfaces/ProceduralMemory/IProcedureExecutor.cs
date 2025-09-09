namespace Emma.Core.Interfaces.ProceduralMemory;

public interface IProcedureExecutor
{
    System.Threading.Tasks.Task<ExecutionResult> ExecuteAsync(
        ReplayPlan plan,
        ValidationContext vctx,
        System.Threading.CancellationToken ct);
}
