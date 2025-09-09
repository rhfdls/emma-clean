using Emma.Core.Interfaces.ProceduralMemory;

namespace Emma.Core.ProceduralMemory;

public sealed class NoopProcedureExecutor : IProcedureExecutor
{
    public System.Threading.Tasks.Task<ExecutionResult> ExecuteAsync(ReplayPlan plan, ValidationContext vctx, System.Threading.CancellationToken ct)
        => System.Threading.Tasks.Task.FromResult(new ExecutionResult(true));
}
