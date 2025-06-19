using Emma.Models.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Compliance;

/// <summary>
/// Implementation of compliance checker to enforce mandatory validation patterns
/// Ensures all agents follow Responsible AI principles
/// </summary>
public class AgentComplianceChecker : IAgentComplianceChecker
{
    private readonly ILogger<AgentComplianceChecker> _logger;

    public AgentComplianceChecker(ILogger<AgentComplianceChecker> logger)
    {
        _logger = logger;
    }

    public async Task<ComplianceValidationResult> ValidateAgentResponseAsync<T>(
        AgentResponse response,
        List<T> actions,
        string traceId) where T : IAgentAction
    {
        var result = new ComplianceValidationResult
        {
            TraceId = traceId,
            TotalActions = actions?.Count ?? 0
        };

        if (actions == null || !actions.Any())
        {
            result.IsCompliant = true;
            return result;
        }

        var violations = new List<string>();

        foreach (var action in actions)
        {
            if (!IsActionValidated(action))
            {
                result.UnvalidatedActions++;
                violations.Add($"Action '{action.ActionType}' lacks validation metadata");
                
                // Report critical compliance violation
                await ReportComplianceViolationAsync(new ComplianceViolation
                {
                    ViolationType = "UnvalidatedAction",
                    AgentType = ExtractAgentTypeFromTrace(traceId),
                    ActionType = action.ActionType,
                    Description = $"AI-generated action bypassed mandatory validation: {action.Description}",
                    TraceId = traceId,
                    Severity = "Critical"
                }, traceId);
            }
            else
            {
                result.ValidatedActions++;
            }
        }

        result.Violations = violations;
        result.IsCompliant = result.UnvalidatedActions == 0;

        if (!result.IsCompliant)
        {
            _logger.LogError("🚨 COMPLIANCE VIOLATION: Agent response contains {Count} unvalidated actions, TraceId: {TraceId}",
                result.UnvalidatedActions, traceId);
        }
        else
        {
            _logger.LogDebug("✅ Compliance check passed: {Count} actions validated, TraceId: {TraceId}",
                result.ValidatedActions, traceId);
        }

        return result;
    }

    public bool IsActionValidated<T>(T action) where T : IAgentAction
    {
        // Check for required validation metadata
        var hasValidationReason = !string.IsNullOrEmpty(action.ValidationReason);
        var hasConfidenceScore = action.ConfidenceScore >= 0.0 && action.ConfidenceScore <= 1.0;
        var hasApprovalDecision = action.RequiresApproval == false || !string.IsNullOrEmpty(action.ApprovalRequestId);

        return hasValidationReason && hasConfidenceScore && hasApprovalDecision;
    }

    public async Task<ComplianceAuditReport> AuditAgentComplianceAsync(TimeRange timeRange)
    {
        _logger.LogInformation("🔍 Starting compliance audit for period {Start} to {End}",
            timeRange.StartTime, timeRange.EndTime);

        // This would typically query audit logs from a database
        // For now, return a template structure
        var report = new ComplianceAuditReport
        {
            AuditPeriod = timeRange,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("📊 Compliance audit completed: {Rate:F1}% compliance rate",
            report.ComplianceRate);

        return report;
    }

    public async Task ReportComplianceViolationAsync(ComplianceViolation violation, string traceId)
    {
        _logger.LogError("🚨 COMPLIANCE VIOLATION [{Severity}]: {Type} in {Agent} - {Description}, TraceId: {TraceId}",
            violation.Severity, violation.ViolationType, violation.AgentType, violation.Description, traceId);

        // In production, this would:
        // 1. Store violation in audit database
        // 2. Send alerts to compliance team
        // 3. Trigger automated responses for critical violations
        // 4. Update compliance metrics

        await Task.CompletedTask; // Placeholder for async operations
    }

    private string ExtractAgentTypeFromTrace(string traceId)
    {
        // Extract agent type from trace ID or context
        // This is a simplified implementation
        return "UnknownAgent";
    }
}

/// <summary>
/// Extension methods for compliance checking
/// </summary>
public static class ComplianceExtensions
{
    /// <summary>
    /// Ensures an agent response is compliant before returning
    /// Throws exception if compliance violations are detected
    /// </summary>
    public static async Task<AgentResponse> EnsureComplianceAsync<T>(
        this AgentResponse response,
        List<T> actions,
        IAgentComplianceChecker complianceChecker,
        string traceId) where T : IAgentAction
    {
        var complianceResult = await complianceChecker.ValidateAgentResponseAsync(response, actions, traceId);
        
        if (!complianceResult.IsCompliant)
        {
            throw new ComplianceViolationException(
                $"Agent response failed compliance validation: {string.Join(", ", complianceResult.Violations)}",
                complianceResult);
        }

        return response;
    }
}

/// <summary>
/// Exception thrown when compliance violations are detected
/// </summary>
public class ComplianceViolationException : Exception
{
    public ComplianceValidationResult ComplianceResult { get; }

    public ComplianceViolationException(string message, ComplianceValidationResult result) 
        : base(message)
    {
        ComplianceResult = result;
    }
}
