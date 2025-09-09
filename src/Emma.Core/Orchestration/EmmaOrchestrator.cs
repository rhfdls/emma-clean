using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces.Orchestration;
using Emma.Core.Interfaces.ProceduralMemory;
using Emma.Core.Interfaces.Validation;
using Microsoft.Extensions.Logging;
using Emma.Core.Telemetry;
using Microsoft.Extensions.Configuration;

namespace Emma.Core.Orchestration;

public sealed class EmmaOrchestrator
{
    private readonly IProceduralMemoryService _pms;
    private readonly IProcedureExecutor _executor;
    private readonly IAgentPlanner _planner;
    private readonly IValidatorPipeline _validator;
    private readonly ILogger<EmmaOrchestrator> _logger;
    private readonly bool _enrichTelemetry; // SPRINT2: Phase0

    public EmmaOrchestrator(
        IProceduralMemoryService pms,
        IProcedureExecutor executor,
        IAgentPlanner planner,
        IValidatorPipeline validator,
        ILogger<EmmaOrchestrator> logger,
        IConfiguration config)
    {
        _pms = pms;
        _executor = executor;
        _planner = planner;
        _validator = validator;
        _logger = logger;
        // SPRINT2: Phase0
        _enrichTelemetry = bool.TryParse(config["Features:Telemetry:EnrichProcedureFields"], out var b) && b;
    }

    public async Task<ExecutionResult> HandleAsync(AgentRequest req, CancellationToken ct)
    {
        _logger.LogDebug("EmmaOrchestrator.HandleAsync invoked for action={ActionType} org={OrgId}", req.ActionType, req.OrganizationId);

        // 1) Try replay
        var lookup = BuildLookup(req);
        var replay = await _pms.TryGetReplayAsync(lookup, ct);
        if (replay is not null)
        {
            var vctx = BuildValidationContext(req);
            var v = await _validator.ValidateReplayAsync(replay, vctx, ct);
            using var hitScope = ReplayLogScope.BeginReplayScope(_logger,
                procedureId: replay.ProcedureId,
                procedureVersion: replay.Version,
                traceId: System.Guid.NewGuid().ToString("n"),
                tenantId: req.TenantId.ToString(),
                replay: true,
                fallback: false,
                overrideRequired: v.OverrideRequired);
            // SPRINT2: Phase0 — optional enrichment
            if (_enrichTelemetry)
            {
                using var extra = _logger.BeginScope(new System.Collections.Generic.Dictionary<string, object>
                {
                    ["decisionPath"] = "replay",
                    // procedureIndustry/procedureRing unavailable in Phase 0 data model
                    ["procedureIndustry"] = string.Empty,
                    ["procedureRing"] = string.Empty
                });
                // scope lifetime ends on dispose
            }

            if (!v.Allowed)
            {
                // validation blocked replay; fall back to LLM
                return await PlanWithLlm(req, ct, markFallback: true);
            }
            return await _executor.ExecuteAsync(replay, vctx, ct);
        }

        // 2) Plan with LLM and capture trace (observability)
        return await PlanWithLlm(req, ct);
    }

    private async Task<ExecutionResult> PlanWithLlm(AgentRequest req, CancellationToken ct, bool markFallback = false)
    {
        var planned = await _planner.PlanAsync(req, ct);
        await _pms.CaptureTraceAsync(planned.TraceId, planned, ct);
        var vctx = BuildValidationContext(req);
        var v = await _validator.ValidatePlannedAsync(planned, vctx, ct);
        using var scope = ReplayLogScope.BeginReplayScope(_logger,
            traceId: planned.TraceId,
            tenantId: req.TenantId.ToString(),
            replay: false,
            fallback: markFallback,
            overrideRequired: v.OverrideRequired);
        // SPRINT2: Phase0 — optional enrichment
        if (_enrichTelemetry)
        {
            using var extra = _logger.BeginScope(new System.Collections.Generic.Dictionary<string, object>
            {
                ["decisionPath"] = markFallback ? "fallback" : "planned"
            });
        }

        if (!v.Allowed) return new ExecutionResult(false, "Validation failed");
        return await planned.ExecuteAsync(ct);
    }

    private static ProcedureLookupRequest BuildLookup(AgentRequest r) => new(
        r.TenantId,
        r.OrganizationId,
        r.UserId,
        r.ActionType,
        r.Channel,
        r.Industry,
        r.RiskBand,
        ContextFingerprint.From(r),
        r.Params,
        r.UserOverrides);

    private static ValidationContext BuildValidationContext(AgentRequest r) => new(
        r.TenantId,
        r.OrganizationId,
        r.UserId,
        ContactId: null,
        r.UserOverrides,
        r.Params);
}

internal static class ContextFingerprint
{
    // Minimal deterministic placeholder; replace with richer fingerprint in Phase 2
    public static string From(AgentRequest r)
        => $"{r.TenantId}:{r.OrganizationId}:{r.ActionType}:{r.Channel}:{r.Industry}:{r.RiskBand}";
}
