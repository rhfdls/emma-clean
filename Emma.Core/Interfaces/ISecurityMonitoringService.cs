using Emma.Core.Services;

namespace Emma.Core.Interfaces;

/// <summary>
/// Interface for security monitoring and audit logging for AI agents
/// </summary>
public interface ISecurityMonitoringService : IDisposable
{
    /// <summary>
    /// Check if a user/agent combination is within rate limits
    /// </summary>
    bool CheckRateLimit(string userId, string agentType, string traceId);

    /// <summary>
    /// Validate and sanitize input for security threats
    /// </summary>
    SecurityValidationResult ValidateInput(string input, string inputType, string traceId);

    /// <summary>
    /// Log an audit event for compliance and monitoring
    /// </summary>
    Task LogAuditEventAsync(AuditEvent auditEvent);

    /// <summary>
    /// Retrieve audit events for analysis
    /// </summary>
    Task<List<AuditEvent>> GetAuditEventsAsync(string? userId = null, string? agentType = null, 
        DateTime? startTime = null, DateTime? endTime = null, int maxResults = 100);

    /// <summary>
    /// Get security metrics for monitoring dashboard
    /// </summary>
    Task<SecurityMetrics> GetSecurityMetricsAsync(TimeSpan period);
}
