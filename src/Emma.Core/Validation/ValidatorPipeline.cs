using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces.Validation;
using Emma.Core.Interfaces.ProceduralMemory;
using Emma.Core.Interfaces.Orchestration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emma.Core.Validation;

// Skeleton implementation: replace with real Relevance → Risk/Confidence → Override rules
public sealed class ValidatorPipeline : IValidatorPipeline
{
    private readonly ILogger<ValidatorPipeline> _logger;
    public ValidatorPipeline(ILogger<ValidatorPipeline> logger) => _logger = logger;

    public Task<ValidationOutcome> ValidateReplayAsync(ReplayPlan plan, ValidationContext ctx, CancellationToken ct)
    {
        _logger.LogInformation("ValidateReplay: proc={Proc} v={Ver} org={Org}", plan.ProcedureId, plan.Version, ctx.OrganizationId);

        // Relevance: deny if PERSONAL tag present unless BypassPrivacy=true
        if (HasPersonalTag(ctx.Params) && !BypassPrivacy(ctx.UserOverrides))
        {
            return Task.FromResult(new ValidationOutcome(false, false, "Blocked: PERSONAL tag"));
        }

        return Task.FromResult(new ValidationOutcome(true, false));
    }

    public Task<ValidationOutcome> ValidatePlannedAsync(PlannedExecution plannedExecution, ValidationContext ctx, CancellationToken ct)
    {
        _logger.LogInformation("ValidatePlanned: trace={Trace} org={Org}", plannedExecution.TraceId, ctx.OrganizationId);

        // Relevance: deny if PERSONAL tag present unless BypassPrivacy=true
        if (HasPersonalTag(ctx.Params) && !BypassPrivacy(ctx.UserOverrides))
        {
            return Task.FromResult(new ValidationOutcome(false, false, "Blocked: PERSONAL tag"));
        }

        // Risk/Override: require override for after-hours SMS or low confidence (<0.3)
        var overrideRequired = false;

        if (IsAfterHoursSms(ctx.Params))
        {
            overrideRequired = true;
        }

        if (plannedExecution is not null && plannedExecution.TraceId is not null)
        {
            // Confidence threshold check (if present, and < 0.3)
            var confProp = plannedExecution.GetType().GetProperty("Confidence");
            if (confProp != null && confProp.GetValue(plannedExecution) is double conf && conf < 0.3)
            {
                overrideRequired = true;
            }
        }

        return Task.FromResult(new ValidationOutcome(!overrideRequired, overrideRequired,
            overrideRequired ? "Override: after-hours SMS or low confidence" : null));
    }

    private static bool HasPersonalTag(IDictionary<string, object>? @params)
    {
        if (@params == null) return false;
        if (@params.TryGetValue("tags", out var t) && t is IEnumerable<string> tags)
        {
            return tags.Any(x => string.Equals(x, "PERSONAL", StringComparison.OrdinalIgnoreCase));
        }
        return false;
    }

    private static bool BypassPrivacy(IDictionary<string, object>? overrides)
    {
        if (overrides == null) return false;
        return overrides.TryGetValue("BypassPrivacy", out var v) && v is bool b && b;
    }

    private static bool IsAfterHoursSms(IDictionary<string, object>? @params)
    {
        if (@params == null) return false;
        var channel = @params.TryGetValue("channel", out var ch) ? ch as string : null;
        if (!string.Equals(channel, "sms", StringComparison.OrdinalIgnoreCase)) return false;

        DateTime occurredUtc;
        if (@params.TryGetValue("occurredAt", out var o))
        {
            if (o is DateTime dt) occurredUtc = dt.ToUniversalTime();
            else if (o is string s && DateTime.TryParse(s, out var p)) occurredUtc = p.ToUniversalTime();
            else occurredUtc = DateTime.UtcNow;
        }
        else
        {
            occurredUtc = DateTime.UtcNow;
        }

        var hour = occurredUtc.Hour; // 0-23 UTC
        // Define after-hours as 21:00-08:00 UTC for pilot
        return hour >= 21 || hour < 8;
    }
}
