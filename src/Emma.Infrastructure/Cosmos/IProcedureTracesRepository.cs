using System;
using System.Threading;
using System.Threading.Tasks;

namespace Emma.Infrastructure.Cosmos;

public interface IProcedureTracesRepository
{
    Task CaptureAsync(ProcedureTrace doc, CancellationToken ct);
}

public sealed record ProcedureTrace(
    string id,
    Guid tenantId,
    string actionType,
    string channel,
    string contextFingerprint,
    object redactedInputs,
    object outcome);
