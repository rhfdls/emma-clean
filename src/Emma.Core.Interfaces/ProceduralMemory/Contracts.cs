namespace Emma.Core.Interfaces.ProceduralMemory;

public record ProcedureLookupRequest(
    System.Guid TenantId,
    System.Guid OrganizationId,
    System.Guid? UserId,
    string ActionType,
    string Channel,
    string Industry,
    string RiskBand,
    string ContextFingerprint,
    System.Collections.Generic.IDictionary<string, object>? Params,
    System.Collections.Generic.IDictionary<string, object>? UserOverrides);

public record ReplayPlan(
    string ProcedureId,
    int Version,
    System.Collections.Generic.IReadOnlyList<ReplayStep> Steps,
    System.Collections.Generic.IDictionary<string, object> BoundParameters,
    bool RequiresValidation);

public record ReplayStep(
    string Kind,
    string Name,
    System.Collections.Generic.IDictionary<string, object>? Args = null);

public sealed record ExecutionResult(bool Success, string? FailureReason = null);

public sealed record ValidationContext(
    System.Guid TenantId,
    System.Guid OrganizationId,
    System.Guid? UserId,
    System.Guid? ContactId,
    System.Collections.Generic.IDictionary<string, object>? UserOverrides,
    System.Collections.Generic.IDictionary<string, object>? Params);
