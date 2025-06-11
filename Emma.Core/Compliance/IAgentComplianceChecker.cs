using Emma.Core.Interfaces;
using Emma.Core.Models;

namespace Emma.Core.Compliance;

/// <summary>
/// Compliance checker to ensure all agents follow mandatory validation patterns
/// Enforces Responsible AI principles across the platform
/// </summary>
public interface IAgentComplianceChecker
{
    /// <summary>
    /// Validates that an agent response contains only validated actions
    /// Throws exception if unvalidated actions are detected
    /// </summary>
    /// <param name="response">Agent response to validate</param>
    /// <param name="actions">List of actions from the response</param>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <returns>Compliance validation result</returns>
    Task<ComplianceValidationResult> ValidateAgentResponseAsync<T>(
        AgentResponse response,
        List<T> actions,
        string traceId) where T : IAgentAction;

    /// <summary>
    /// Checks if an agent action has been properly validated
    /// </summary>
    /// <param name="action">Action to check</param>
    /// <returns>True if action has validation metadata</returns>
    bool IsActionValidated<T>(T action) where T : IAgentAction;

    /// <summary>
    /// Audits agent compliance across the platform
    /// </summary>
    /// <param name="timeRange">Time range for audit</param>
    /// <returns>Compliance audit report</returns>
    Task<ComplianceAuditReport> AuditAgentComplianceAsync(TimeRange timeRange);

    /// <summary>
    /// Reports a compliance violation
    /// </summary>
    /// <param name="violation">Violation details</param>
    /// <param name="traceId">Trace ID for correlation</param>
    Task ReportComplianceViolationAsync(ComplianceViolation violation, string traceId);
}

/// <summary>
/// Result of compliance validation
/// </summary>
public class ComplianceValidationResult
{
    public bool IsCompliant { get; set; }
    public List<string> Violations { get; set; } = new();
    public int TotalActions { get; set; }
    public int ValidatedActions { get; set; }
    public int UnvalidatedActions { get; set; }
    public string TraceId { get; set; } = string.Empty;
}

/// <summary>
/// Compliance audit report
/// </summary>
public class ComplianceAuditReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeRange AuditPeriod { get; set; } = new();
    public int TotalAgentResponses { get; set; }
    public int CompliantResponses { get; set; }
    public int NonCompliantResponses { get; set; }
    public double ComplianceRate => TotalAgentResponses > 0 ? (double)CompliantResponses / TotalAgentResponses * 100 : 0;
    public List<ComplianceViolation> Violations { get; set; } = new();
    public Dictionary<string, int> ViolationsByAgent { get; set; } = new();
    public Dictionary<string, int> ViolationsByType { get; set; } = new();
}

/// <summary>
/// Compliance violation details
/// </summary>
public class ComplianceViolation
{
    public string ViolationType { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
}

/// <summary>
/// Time range for auditing
/// </summary>
public class TimeRange
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
