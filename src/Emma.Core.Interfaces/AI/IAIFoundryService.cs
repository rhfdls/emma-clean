using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces.AI;

public interface IAIFoundryService
{
    Task<FoundryPlanResult> PlanAsync(FoundryPlanRequest request, CancellationToken ct);
}

public sealed record FoundryPlanRequest(
    Guid TenantId, Guid OrganizationId, Guid? UserId,
    string ActionType, string Channel, string Industry, string RiskBand,
    string Prompt,
    IReadOnlyDictionary<string,string>? ToolSchemaJson = null,
    IReadOnlyDictionary<string,string>? SystemDirectives = null
);

public sealed record FoundryPlanResult(
    string TraceId,
    string RawResponseJson,
    IReadOnlyList<PlannedToolStep> Steps,
    double? Confidence = null
);

public sealed record PlannedToolStep(
    string Name,
    IReadOnlyDictionary<string,object?> Args,
    double? Risk = null
);
