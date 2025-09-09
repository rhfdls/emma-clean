namespace Emma.Core.Interfaces.Orchestration;

public interface IAgentPlanner
{
    System.Threading.Tasks.Task<PlannedExecution> PlanAsync(AgentRequest request, System.Threading.CancellationToken ct);
}

public sealed record AgentRequest(
    System.Guid TenantId,
    System.Guid OrganizationId,
    System.Guid? UserId,
    string ActionType,
    string Channel,
    string Industry,
    string RiskBand,
    System.Collections.Generic.IDictionary<string, object>? Params,
    System.Collections.Generic.IDictionary<string, object>? UserOverrides);

public sealed record PlannedExecution(
    string TraceId,
    System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<Emma.Core.Interfaces.ProceduralMemory.ExecutionResult>> ExecuteAsync);
