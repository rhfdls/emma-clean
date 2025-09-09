using Emma.Core.Interfaces.ProceduralMemory;
using Emma.Core.Interfaces.AI;
using Emma.Core.Interfaces.Orchestration;

namespace Emma.Core.Interfaces.Validation;

public interface IValidatorPipeline
{
    // Validate a replay plan prior to execution
    System.Threading.Tasks.Task<ValidationOutcome> ValidateReplayAsync(ReplayPlan plan, ValidationContext context, System.Threading.CancellationToken ct);

    // Validate a freshly planned execution (LLM/FYX-backed)
    System.Threading.Tasks.Task<ValidationOutcome> ValidatePlannedAsync(PlannedExecution plannedExecution, ValidationContext context, System.Threading.CancellationToken ct);
}

public sealed record ValidationOutcome(bool Allowed, bool OverrideRequired, string? Reason = null);
