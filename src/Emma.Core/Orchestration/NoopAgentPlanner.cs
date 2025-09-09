using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces.Orchestration;
using Emma.Core.Interfaces.ProceduralMemory;

namespace Emma.Core.Orchestration;

public sealed class NoopAgentPlanner : IAgentPlanner
{
    public Task<PlannedExecution> PlanAsync(AgentRequest request, CancellationToken ct)
    {
        // Returns a trivial execution that succeeds; replace with Azure AI Foundry-backed planner later
        var planned = new PlannedExecution(
            TraceId: System.Guid.NewGuid().ToString("n"),
            ExecuteAsync: async token =>
            {
                await Task.CompletedTask;
                return new ExecutionResult(true);
            });
        return Task.FromResult(planned);
    }
}
