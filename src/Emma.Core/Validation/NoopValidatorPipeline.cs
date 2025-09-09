using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces.ProceduralMemory;
using Emma.Core.Interfaces.Validation;
using Emma.Core.Interfaces.Orchestration;

namespace Emma.Core.Validation;

public sealed class NoopValidatorPipeline : IValidatorPipeline
{
    public Task<ValidationOutcome> ValidateReplayAsync(ReplayPlan plan, ValidationContext context, CancellationToken ct)
        => Task.FromResult(new ValidationOutcome(Allowed: true, OverrideRequired: false));

    public Task<ValidationOutcome> ValidatePlannedAsync(PlannedExecution plannedExecution, ValidationContext context, CancellationToken ct)
        => Task.FromResult(new ValidationOutcome(Allowed: true, OverrideRequired: false));
}
