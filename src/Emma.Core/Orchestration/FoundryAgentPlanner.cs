using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces.AI;
using Emma.Core.Interfaces.Context;
using Emma.Core.Interfaces.Orchestration;
using Emma.Core.Interfaces.ProceduralMemory;
using System.Collections.Generic;

namespace Emma.Core.Orchestration;

public sealed class FoundryAgentPlanner : IAgentPlanner
{
    private readonly IAIFoundryService _foundry;
    private readonly IContextRetrievalService _retrieval;

    public FoundryAgentPlanner(IAIFoundryService foundry, IContextRetrievalService retrieval)
    { _foundry = foundry; _retrieval = retrieval; }

    public async Task<PlannedExecution> PlanAsync(AgentRequest req, CancellationToken ct)
    {
        // Convert Params (IDictionary<string, object>?) to IReadOnlyDictionary<string, object?>? for Hints
        IReadOnlyDictionary<string, object?>? hints = null;
        if (req.Params is not null)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var kv in req.Params)
            {
                dict[kv.Key] = kv.Value; // covariant to object?
            }
            hints = dict; // Dictionary<> implements IReadOnlyDictionary<>
        }

        var ctx = await _retrieval.GetAsync(new Interfaces.Context.RetrievalQuery(
            req.TenantId, req.OrganizationId, req.UserId, null,
            req.ActionType, req.Channel, req.Industry, req.RiskBand, hints), ct);

        var prompt = ComposePrompt(req, ctx);
        var result = await _foundry.PlanAsync(new Interfaces.AI.FoundryPlanRequest(
            req.TenantId, req.OrganizationId, req.UserId,
            req.ActionType, req.Channel, req.Industry, req.RiskBand,
            Prompt: prompt,
            ToolSchemaJson: null,
            SystemDirectives: ctx.PolicyDirectives), ct);

        // Bridge: wrap planned steps into an ExecuteAsync delegate.
        return new PlannedExecution(
            TraceId: result.TraceId,
            ExecuteAsync: async (c) =>
            {
                // TODO: Map PlannedToolStep -> actual tool invocations.
                // For now, succeed as a stub.
                await Task.CompletedTask;
                return new ExecutionResult(true, "planned (stub)");
            });
    }

    private static string ComposePrompt(AgentRequest r, Interfaces.Context.RetrievalBundle b)
    {
        var snippets = string.Join("\n- ", b.Snippets.Select(s => $"[{s.Kind}:{s.RefId}] {s.RedactedText}"));
        return $@"You are EMMA’s planner. ActionType={r.ActionType}, Channel={r.Channel}, Industry={r.Industry}, RiskBand={r.RiskBand}.
RollingSummary: {b.RollingSummary}
Context Snippets:
- {snippets}
Follow org policies and propose minimal tool steps.";
    }
}
