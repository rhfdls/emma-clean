using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Emma.Core.Services;

/// <summary>
/// Security and monitoring service for AI agents
/// Provides rate limiting, audit logging, and security validation
/// </summary>
public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();
    private readonly ConcurrentQueue<AuditEvent> _auditQueue = new();
    private readonly Timer _cleanupTimer;

    // Security thresholds
    private const int MaxRequestsPerMinute = 30;
    private const int MaxRequestsPerHour = 200;
    private const int MaxPromptLength = 10000;
    private const int MaxResponseLength = 50000;

    public SecurityMonitoringService(ILogger<SecurityMonitoringService> logger)
    {
        _logger = logger;
        
        // Cleanup timer runs every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<bool> CheckRateLimitAsync(string userId, string agentType, string traceId)
    {
        var now = DateTime.UtcNow;
        var key = $"{userId}:{agentType}";
        
        var rateLimitInfo = _rateLimits.GetOrAdd(key, _ => new RateLimitInfo());
        
        lock (rateLimitInfo)
        {
            // Clean old entries
            rateLimitInfo.RequestTimes.RemoveAll(time => time < now.AddHours(-1));
            
            // Check minute limit
            var recentRequests = rateLimitInfo.RequestTimes.Count(time => time > now.AddMinutes(-1));
            if (recentRequests >= MaxRequestsPerMinute)
            {
                _logger.LogWarning("🚨 Rate limit exceeded (per minute) for user: {UserId}, agent: {AgentType}, TraceId: {TraceId}", 
                    userId, agentType, traceId);
                return false;
            }
            
            // Check hour limit
            if (rateLimitInfo.RequestTimes.Count >= MaxRequestsPerHour)
            {
                _logger.LogWarning("🚨 Rate limit exceeded (per hour) for user: {UserId}, agent: {AgentType}, TraceId: {TraceId}", 
                    userId, agentType, traceId);
                return false;
            }
            
            // Add current request
            rateLimitInfo.RequestTimes.Add(now);
            return true;
        }
    }

    public async Task<SecurityValidationResult> ValidateInputAsync(string input, string inputType, string traceId)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new SecurityValidationResult(false, "Input cannot be empty", string.Empty);
        }

        var sanitizedInput = input;
        var warnings = new List<string>();

        // Check length limits
        if (inputType == "prompt" && input.Length > MaxPromptLength)
        {
            sanitizedInput = input.Substring(0, MaxPromptLength) + "...";
            warnings.Add($"Input truncated from {input.Length} to {MaxPromptLength} characters");
        }
        else if (inputType == "response" && input.Length > MaxResponseLength)
        {
            sanitizedInput = input.Substring(0, MaxResponseLength) + "...";
            warnings.Add($"Response truncated from {input.Length} to {MaxResponseLength} characters");
        }

        // Check for potential prompt injection patterns
        var suspiciousPatterns = new[]
        {
            "```", "###", "---", "System:", "Assistant:", "Human:", "User:",
            "IGNORE", "OVERRIDE", "BYPASS", "ADMIN", "ROOT"
        };

        var foundPatterns = suspiciousPatterns.Where(pattern => 
            sanitizedInput.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();

        if (foundPatterns.Any())
        {
            // Remove suspicious patterns
            foreach (var pattern in foundPatterns)
            {
                sanitizedInput = sanitizedInput.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
            }
            warnings.Add($"Removed suspicious patterns: {string.Join(", ", foundPatterns)}");
            
            _logger.LogWarning("🔒 Potential prompt injection detected, TraceId: {TraceId}, Patterns: {Patterns}", 
                traceId, string.Join(", ", foundPatterns));
        }

        // Additional sanitization
        sanitizedInput = sanitizedInput.Trim();

        return new SecurityValidationResult(true, string.Empty, sanitizedInput, warnings);
    }

    public async Task LogAuditEventAsync(AuditEvent auditEvent)
    {
        auditEvent.Timestamp = DateTime.UtcNow;
        _auditQueue.Enqueue(auditEvent);

        // Log to standard logger
        _logger.LogInformation("📋 Audit Event: {EventType} - Agent: {AgentType} - User: {UserId} - TraceId: {TraceId} - Data: {Data}",
            auditEvent.EventType, auditEvent.AgentType, auditEvent.UserId, auditEvent.TraceId, 
            JsonSerializer.Serialize(auditEvent.AdditionalData));

        // TODO: Send to dedicated audit database/service
        await Task.CompletedTask;
    }

    public async Task<List<AuditEvent>> GetAuditEventsAsync(string? userId = null, string? agentType = null, 
        DateTime? startTime = null, DateTime? endTime = null, int maxResults = 100)
    {
        var events = _auditQueue.ToArray()
            .Where(e => userId == null || e.UserId == userId)
            .Where(e => agentType == null || e.AgentType == agentType)
            .Where(e => startTime == null || e.Timestamp >= startTime)
            .Where(e => endTime == null || e.Timestamp <= endTime)
            .OrderByDescending(e => e.Timestamp)
            .Take(maxResults)
            .ToList();

        return await Task.FromResult(events);
    }

    public async Task<SecurityMetrics> GetSecurityMetricsAsync(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        var recentEvents = _auditQueue.Where(e => e.Timestamp >= cutoff).ToList();

        var metrics = new SecurityMetrics
        {
            TotalRequests = recentEvents.Count,
            FailedRequests = recentEvents.Count(e => e.EventType.Contains("ERROR")),
            RateLimitViolations = recentEvents.Count(e => e.EventType == "RATE_LIMIT_EXCEEDED"),
            SecurityViolations = recentEvents.Count(e => e.EventType == "SECURITY_VIOLATION"),
            AverageResponseTime = recentEvents
                .Where(e => e.AdditionalData.ContainsKey("processing_time_ms"))
                .Select(e => Convert.ToDouble(e.AdditionalData["processing_time_ms"]))
                .DefaultIfEmpty(0)
                .Average(),
            Period = period,
            GeneratedAt = DateTime.UtcNow
        };

        return await Task.FromResult(metrics);
    }

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddHours(-2);
            
            // Clean rate limit entries
            var expiredKeys = _rateLimits
                .Where(kvp => kvp.Value.RequestTimes.All(time => time < cutoff))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _rateLimits.TryRemove(key, out _);
            }

            // Clean audit queue (keep last 1000 events)
            while (_auditQueue.Count > 1000)
            {
                _auditQueue.TryDequeue(out _);
            }

            _logger.LogDebug("🧹 Cleanup completed: Removed {ExpiredKeys} rate limit entries, audit queue size: {QueueSize}",
                expiredKeys.Count, _auditQueue.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during security monitoring cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

/// <summary>
/// Rate limiting information for a user/agent combination
/// </summary>
public class RateLimitInfo
{
    public List<DateTime> RequestTimes { get; set; } = new();
}

/// <summary>
/// Security validation result
/// </summary>
public record SecurityValidationResult(
    bool IsValid, 
    string ErrorMessage, 
    string SanitizedInput, 
    List<string>? Warnings = null);

/// <summary>
/// Audit event for security monitoring
/// </summary>
public class AuditEvent
{
    public string EventType { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Security metrics for monitoring dashboard
/// </summary>
public class SecurityMetrics
{
    public int TotalRequests { get; set; }
    public int FailedRequests { get; set; }
    public int RateLimitViolations { get; set; }
    public int SecurityViolations { get; set; }
    public double AverageResponseTime { get; set; }
    public TimeSpan Period { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    public double SuccessRate => TotalRequests > 0 ? (double)(TotalRequests - FailedRequests) / TotalRequests * 100 : 0;
}
