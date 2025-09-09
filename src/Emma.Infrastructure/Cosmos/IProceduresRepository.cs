using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Interfaces.ProceduralMemory;

namespace Emma.Infrastructure.Cosmos;

public interface IProceduresRepository
{
    // SPRINT2: Phase0
    // Expected Cosmos document fields:
    // tenantId (pk), industry, actionType, channel, version, enabled,
    // organizationId? (optional), ring? (optional)
    Task<ReplayPlan?> TryFindAsync(
        Guid tenantId,
        string actionType,
        string channel,
        string? industry,
        Guid? organizationId,
        bool useIndustry,
        CancellationToken ct);
    Task UpsertAsync(CompiledProcedure doc, CancellationToken ct);
}

public sealed record CompiledProcedure(
    string id,
    Guid tenantId,
    string scope,
    string name,
    string actionType,
    string channel,
    int version,
    IReadOnlyList<ReplayStep> steps,
    IDictionary<string, object> parameters,
    IDictionary<string, object>? preconditions,
    bool enabled,
    string ring
);
