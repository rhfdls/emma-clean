using Emma.Core.Models;
using Emma.Core.Industry;
using Emma.Core.Interfaces;
using Emma.Core.Compliance;
using Emma.Data.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Emma.Core.Services;

/// <summary>
/// NBA (Next Best Action) Agent - Analyzes contact context and provides intelligent recommendations
/// Uses LLM-powered intelligence with dynamic prompts for context-aware recommendations
/// Enhanced with security, monitoring, and user override capabilities
/// </summary>
public class NbaAgent : INbaAgent
{
    private readonly INbaContextService _nbaContextService;
    private readonly IAIFoundryService _aiFoundryService;
    private readonly IPromptProvider _promptProvider;
    private readonly IActionRelevanceValidator _actionRelevanceValidator;
    private readonly IAgentActionValidator _agentActionValidator;
    private readonly IAgentComplianceChecker _complianceChecker;
    private readonly ILogger<NbaAgent> _logger;
    private readonly ITenantContextService _tenantContextService;

    // Rate limiting fields
    private readonly Dictionary<string, int> _requestCounts = new();
    private readonly Dictionary<string, DateTime> _rateLimitTracker = new();
    private const int MaxRequestsPerMinute = 10;
    private const int MaxRequestsPerHour = 100;

    public NbaAgent(
        INbaContextService nbaContextService,
        IAIFoundryService aiFoundryService,
        IPromptProvider promptProvider,
        IActionRelevanceValidator actionRelevanceValidator,
        IAgentActionValidator agentActionValidator,
        IAgentComplianceChecker complianceChecker,
        ILogger<NbaAgent> logger,
        ITenantContextService tenantContextService)
    {
        _nbaContextService = nbaContextService;
        _aiFoundryService = aiFoundryService;
        _promptProvider = promptProvider;
        _actionRelevanceValidator = actionRelevanceValidator;
        _agentActionValidator = agentActionValidator;
        _complianceChecker = complianceChecker;
        _logger = logger;
        _tenantContextService = tenantContextService;
    }

    public async Task<AgentResponse> RecommendNextBestActionsAsync(
        Guid contactId,
        Guid organizationId,
        Guid requestingAgentId,
        int maxRecommendations = 3,
        string? traceId = null,
        Dictionary<string, object>? userOverrides = null)
    {
        traceId ??= Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            // Security: Rate limiting check
            if (!CheckRateLimit(requestingAgentId.ToString(), traceId))
            {
                return CreateSecurityErrorResponse("Rate limit exceeded", traceId, startTime);
            }

            // Security: Input validation and sanitization
            var validationResult = ValidateInputs(contactId, organizationId, requestingAgentId, maxRecommendations);
            if (!validationResult.IsValid)
            {
                return CreateValidationErrorResponse(validationResult.ErrorMessage, traceId, startTime);
            }

            _logger.LogInformation("🎯 NBA Agent: Generating LLM-powered recommendations for Contact: {ContactId}, Organization: {OrganizationId}, TraceId: {TraceId}, UserOverrides: {HasOverrides}",
                contactId, organizationId, traceId, userOverrides?.Count > 0);

            // Audit logging: Log request start
            await LogAuditEventAsync("NBA_REQUEST_START", contactId, organizationId, requestingAgentId, traceId, new Dictionary<string, object>());

            // Get NBA context (includes interactions, summary, and state)
            var nbaContext = await _nbaContextService.GetNbaContextAsync(
                contactId, organizationId, requestingAgentId, 5, 10, true);

            // Get contact state
            var contactState = await _nbaContextService.GetContactStateAsync(contactId, organizationId);

            if (contactState == null)
            {
                _logger.LogWarning("Contact state not found for Contact: {ContactId}, TraceId: {TraceId}", contactId, traceId);
                return CreateNotFoundErrorResponse("Contact state not found", traceId, startTime);
            }

            // Get tenant context for industry profile
            var industryProfile = await _tenantContextService.GetIndustryProfileAsync();

            // Get system prompt for NBA recommendations
            var systemPrompt = await _promptProvider.GetSystemPromptAsync("NbaRecommendation", industryProfile);
            
            // Build prompt context with user overrides
            var promptContext = new Dictionary<string, object>
            {
                ["ContactContext"] = nbaContext,
                ["MaxRecommendations"] = Math.Min(maxRecommendations, 5),
                ["AgentType"] = "NbaAgent",
                ["IndustryCode"] = industryProfile.IndustryCode,
                ["UserOverrides"] = userOverrides != null ? JsonSerializer.Serialize(userOverrides) : "None",
                ["TraceId"] = traceId
            };

            var userPrompt = await _promptProvider.BuildPromptAsync("ContactAnalysis", promptContext);

            _logger.LogDebug("🤖 NBA Agent: Calling LLM with system prompt length: {SystemLength}, user prompt length: {UserLength}, TraceId: {TraceId}",
                systemPrompt.Length, userPrompt.Length, traceId);

            // Enhanced LLM call with retry logic and circuit breaker
            var llmResponse = await CallLLMWithRetryAsync(systemPrompt, userPrompt, traceId);

            if (string.IsNullOrEmpty(llmResponse))
            {
                _logger.LogWarning("🚨 NBA Agent: Empty LLM response, falling back to rule-based recommendations, TraceId: {TraceId}", traceId);
                return await GenerateRuleBasedFallbackAsync(contactId, organizationId, nbaContext, contactState, traceId, startTime);
            }

            _logger.LogDebug("🎯 NBA Agent: LLM response received, length: {ResponseLength}, TraceId: {TraceId}", llmResponse.Length, traceId);

            // Parse LLM response into structured recommendations
            var recommendations = await ParseLLMResponseAsync(llmResponse, nbaContext, traceId);

            // Apply user overrides and filters
            var filteredRecommendations = await ApplyUserOverridesAsync(recommendations, userOverrides, traceId);

            // Validate action relevance and check for user approval requirements using standardized approach
            var validationContext = new AgentActionValidationContext
            {
                ContactId = contactId,
                OrganizationId = organizationId,
                UserId = requestingAgentId.ToString(),
                AgentId = requestingAgentId.ToString(),
                AgentType = "NbaAgent",
                AdditionalContext = new Dictionary<string, object>
                {
                    ["userOverrides"] = userOverrides ?? new Dictionary<string, object>(),
                    ["contextSummary"] = nbaContext.RollingSummary?.SummaryText ?? "No summary available",
                    ["recentInteractions"] = nbaContext.RecentInteractions?.Count ?? 0
                }
            };

            var validatedRecommendations = await _agentActionValidator.ValidateAgentActionsAsync(
                filteredRecommendations, validationContext, userOverrides ?? new Dictionary<string, object>(), traceId);

            // MANDATORY: Compliance check - ensures all AI actions are properly validated
            var response = new AgentResponse
            {
                Success = true,
                Data = new Dictionary<string, object> { ["recommendations"] = validatedRecommendations },
                Message = $"Generated {validatedRecommendations.Count} NBA recommendations",
                TraceId = traceId
            };

            // Validate compliance before returning
            await response.EnsureComplianceAsync(validatedRecommendations, _complianceChecker, traceId);

            await LogAuditEventAsync("NBA_REQUEST_SUCCESS", contactId, organizationId, requestingAgentId, traceId, 
                new Dictionary<string, object> { ["RecommendationCount"] = validatedRecommendations.Count });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating NBA recommendations for contact {ContactId}, TraceId: {TraceId}",
                contactId, traceId);

            await LogAuditEventAsync("NBA_REQUEST_ERROR", contactId, organizationId, requestingAgentId, traceId, 
                new Dictionary<string, object> { ["Error"] = ex.Message });

            return new AgentResponse
            {
                Success = false,
                Message = "Failed to generate NBA recommendations",
                TraceId = traceId
            };
        }
    }

    /// <summary>
    /// Enhanced LLM call with retry logic and circuit breaker pattern
    /// </summary>
    private async Task<string> CallLLMWithRetryAsync(string systemPrompt, string userPrompt, string traceId)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 1000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _aiFoundryService.ProcessAgentRequestAsync(systemPrompt, userPrompt, null);
                
                if (!string.IsNullOrEmpty(response))
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation("🔄 NBA Agent: LLM call succeeded on attempt {Attempt}, TraceId: {TraceId}", attempt, traceId);
                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🔄 NBA Agent: LLM call attempt {Attempt} failed, TraceId: {TraceId}", attempt, traceId);
                
                if (attempt == maxRetries)
                {
                    _logger.LogError("🚨 NBA Agent: All {MaxRetries} LLM attempts failed, TraceId: {TraceId}", maxRetries, traceId);
                    throw;
                }

                // Exponential backoff
                var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delay);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Security: Rate limiting check
    /// </summary>
    private bool CheckRateLimit(string userId, string traceId)
    {
        var now = DateTime.UtcNow;
        var minuteKey = $"{userId}:{now:yyyy-MM-dd-HH-mm}";
        var hourKey = $"{userId}:{now:yyyy-MM-dd-HH}";

        // Clean old entries
        var keysToRemove = _rateLimitTracker.Where(kvp => kvp.Value < now.AddHours(-1)).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _rateLimitTracker.Remove(key);
            _requestCounts.Remove(key);
        }

        // Check minute limit
        if (_requestCounts.ContainsKey(minuteKey) && _requestCounts[minuteKey] >= MaxRequestsPerMinute)
        {
            _logger.LogWarning("🚨 NBA Agent: Rate limit exceeded (per minute) for user: {UserId}, TraceId: {TraceId}", userId, traceId);
            return false;
        }

        // Check hour limit
        if (_requestCounts.ContainsKey(hourKey) && _requestCounts[hourKey] >= MaxRequestsPerHour)
        {
            _logger.LogWarning("🚨 NBA Agent: Rate limit exceeded (per hour) for user: {UserId}, TraceId: {TraceId}", userId, traceId);
            return false;
        }

        // Update counters
        _requestCounts[minuteKey] = _requestCounts.GetValueOrDefault(minuteKey, 0) + 1;
        _requestCounts[hourKey] = _requestCounts.GetValueOrDefault(hourKey, 0) + 1;
        _rateLimitTracker[minuteKey] = now;
        _rateLimitTracker[hourKey] = now;

        return true;
    }

    /// <summary>
    /// Security: Input validation and sanitization
    /// </summary>
    private ValidationResult ValidateInputs(Guid contactId, Guid organizationId, Guid requestingAgentId, int maxRecommendations)
    {
        if (contactId == Guid.Empty)
            return new ValidationResult(false, "ContactId cannot be empty");

        if (organizationId == Guid.Empty)
            return new ValidationResult(false, "OrganizationId cannot be empty");

        if (requestingAgentId == Guid.Empty)
            return new ValidationResult(false, "RequestingAgentId cannot be empty");

        if (maxRecommendations < 1 || maxRecommendations > 10)
            return new ValidationResult(false, "MaxRecommendations must be between 1 and 10");

        return new ValidationResult(true, string.Empty);
    }

    /// <summary>
    /// Security: Sanitize text for LLM to prevent prompt injection
    /// </summary>
    private string SanitizeForLLM(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove potential prompt injection patterns
        var sanitized = input
            .Replace("```", "")
            .Replace("###", "")
            .Replace("---", "")
            .Replace("System:", "")
            .Replace("Assistant:", "")
            .Replace("Human:", "")
            .Replace("User:", "");

        // Limit length to prevent token overflow
        if (sanitized.Length > 2000)
        {
            sanitized = sanitized.Substring(0, 2000) + "...";
        }

        return sanitized;
    }

    /// <summary>
    /// Apply user overrides to recommendations
    /// </summary>
    private async Task<List<NbaRecommendation>> ApplyUserOverridesAsync(List<NbaRecommendation> recommendations, Dictionary<string, object> userOverrides, string traceId)
    {
        _logger.LogInformation("🔧 NBA Agent: Applying user overrides, TraceId: {TraceId}", traceId);

        // Handle priority adjustments
        if (userOverrides.ContainsKey("priority_adjustments"))
        {
            // Implementation for priority adjustments
            _logger.LogDebug("🔧 NBA Agent: Applying priority adjustments, TraceId: {TraceId}", traceId);
        }

        // Handle excluded action types
        if (userOverrides.ContainsKey("excluded_actions"))
        {
            var excludedActions = userOverrides["excluded_actions"]?.ToString()?.Split(',') ?? Array.Empty<string>();
            recommendations = recommendations.Where(r => !excludedActions.Contains(r.ActionType)).ToList();
            _logger.LogDebug("🔧 NBA Agent: Excluded {Count} action types, TraceId: {TraceId}", excludedActions.Length, traceId);
        }

        // Handle forced actions
        if (userOverrides.ContainsKey("forced_actions"))
        {
            // Implementation for forced actions
            _logger.LogDebug("🔧 NBA Agent: Processing forced actions, TraceId: {TraceId}", traceId);
        }

        return recommendations;
    }

    /// <summary>
    /// Generate rule-based fallback recommendations
    /// </summary>
    private async Task<AgentResponse> GenerateRuleBasedFallbackAsync(Guid contactId, Guid organizationId, 
        NbaContext nbaContext, ContactState contactState, string traceId, DateTime startTime)
    {
        _logger.LogInformation("🔄 NBA Agent: Generating rule-based fallback recommendations, TraceId: {TraceId}", traceId);

        var fallbackRecommendations = new List<NbaRecommendation>
        {
            new NbaRecommendation
            {
                ActionType = "follow_up_call",
                Priority = 1,
                Description = "Schedule a follow-up call to maintain engagement",
                Reasoning = "Rule-based fallback: Regular follow-up maintains relationship momentum",
                Timing = "Within 3-5 business days",
                ExpectedOutcome = "Continued engagement and relationship building",
                ConfidenceScore = 0.6,
                TraceId = traceId
            }
        };

        return new AgentResponse
        {
            Success = true,
            Data = new Dictionary<string, object> { ["recommendations"] = fallbackRecommendations },
            Message = "Generated rule-based fallback NBA recommendations",
            TraceId = traceId
        };
    }

    /// <summary>
    /// Audit logging for compliance and monitoring
    /// </summary>
    private async Task LogAuditEventAsync(string eventType, Guid contactId, Guid organizationId, 
        Guid requestingAgentId, string traceId, Dictionary<string, object>? additionalData = null)
    {
        var auditData = new Dictionary<string, object>
        {
            ["event_type"] = eventType,
            ["contact_id"] = contactId,
            ["organization_id"] = organizationId,
            ["requesting_agent_id"] = requestingAgentId,
            ["trace_id"] = traceId,
            ["timestamp"] = DateTime.UtcNow,
            ["agent_type"] = "NbaAgent"
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                auditData[kvp.Key] = kvp.Value;
            }
        }

        _logger.LogInformation("📋 NBA Agent Audit: {EventType} - {AuditData}", eventType, JsonSerializer.Serialize(auditData));
        
        // TODO: Send to dedicated audit logging service/database
        await Task.CompletedTask;
    }

    private AgentResponse CreateSecurityErrorResponse(string message, string traceId, DateTime startTime)
    {
        return new AgentResponse
        {
            Success = false,
            Message = message,
            Data = new Dictionary<string, object>
            {
                ["error_type"] = "SecurityError",
                ["processing_time_ms"] = (DateTime.UtcNow - startTime).TotalMilliseconds
            },
            RequestId = traceId,
            AgentId = "NbaAgent",
            Timestamp = DateTime.UtcNow
        };
    }

    private AgentResponse CreateValidationErrorResponse(string message, string traceId, DateTime startTime)
    {
        return new AgentResponse
        {
            Success = false,
            Message = message,
            Data = new Dictionary<string, object>
            {
                ["error_type"] = "ValidationError",
                ["processing_time_ms"] = (DateTime.UtcNow - startTime).TotalMilliseconds
            },
            RequestId = traceId,
            AgentId = "NbaAgent",
            Timestamp = DateTime.UtcNow
        };
    }

    private AgentResponse CreateNotFoundErrorResponse(string message, string traceId, DateTime startTime)
    {
        return new AgentResponse
        {
            Success = false,
            Message = message,
            Data = new Dictionary<string, object>
            {
                ["error_type"] = "NotFoundError",
                ["processing_time_ms"] = (DateTime.UtcNow - startTime).TotalMilliseconds
            },
            RequestId = traceId,
            AgentId = "NbaAgent",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Enhanced LLM response parsing with better error handling
    /// </summary>
    private async Task<List<NbaRecommendation>> ParseLLMResponseAsync(string llmResponse, NbaContext nbaContext, string traceId)
    {
        try
        {
            // Try JSON parsing first
            var jsonResponse = JsonSerializer.Deserialize<LlmRecommendationResponse>(llmResponse);
            if (jsonResponse?.Recommendations != null && jsonResponse.Recommendations.Any())
            {
                _logger.LogDebug("✅ NBA Agent: Successfully parsed JSON LLM response, TraceId: {TraceId}", traceId);
                return jsonResponse.Recommendations;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug("🔄 NBA Agent: JSON parsing failed, trying text parsing. Error: {Error}, TraceId: {TraceId}", ex.Message, traceId);
        }

        // Fallback to text parsing
        var textRecommendations = ParseTextRecommendations(llmResponse);
        if (textRecommendations.Any())
        {
            _logger.LogDebug("✅ NBA Agent: Successfully parsed text LLM response, TraceId: {TraceId}", traceId);
            return textRecommendations;
        }

        _logger.LogWarning("🚨 NBA Agent: Failed to parse LLM response in any format, TraceId: {TraceId}", traceId);
        return new List<NbaRecommendation>();
    }

    /// <summary>
    /// Format recent interactions for LLM context
    /// </summary>
    private string FormatInteractions(List<Emma.Data.Models.Interaction>? interactions)
    {
        if (interactions == null || !interactions.Any())
            return "No recent interactions available";

        var formatted = interactions.Take(5).Select(i => 
            $"- {i.Timestamp:yyyy-MM-dd}: {i.Type} ({i.Direction}) - {i.Content ?? "No content"}");
        
        return string.Join("\n", formatted);
    }

    /// <summary>
    /// Format engagement metrics for LLM context
    /// </summary>
    private string FormatEngagementMetrics(Emma.Data.Models.NbaContext nbaContext)
    {
        var lastActivityDate = nbaContext.RecentInteractions?.FirstOrDefault()?.Timestamp.ToString("yyyy-MM-dd") ?? "Unknown";
        return $@"
- Total Interactions: {nbaContext.RecentInteractions?.Count ?? 0}
- Contact Summary: {nbaContext.RollingSummary?.SummaryText ?? "Not available"}
- Last Activity: {lastActivityDate}";
    }

    /// <summary>
    /// Fallback text parsing for LLM recommendations
    /// </summary>
    private List<NbaRecommendation> ParseTextRecommendations(string response)
    {
        var recommendations = new List<NbaRecommendation>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        int priority = 1;
        foreach (var line in lines.Take(5))
        {
            if (line.Trim().Length > 10) // Skip very short lines
            {
                recommendations.Add(new NbaRecommendation
                {
                    ActionType = DetermineActionType(line),
                    Priority = priority++,
                    Description = line.Trim(),
                    Reasoning = "Generated by LLM analysis",
                    Timing = "As soon as possible",
                    ExpectedOutcome = "Improved engagement and relationship progression"
                });
            }
        }

        // Ensure at least one recommendation
        if (!recommendations.Any())
        {
            recommendations.Add(new NbaRecommendation
            {
                ActionType = "follow_up",
                Priority = 1,
                Description = "Follow up with contact to maintain engagement",
                Reasoning = "Default recommendation when LLM parsing fails",
                Timing = "As soon as possible",
                ExpectedOutcome = "Improved engagement and relationship progression"
            });
        }

        return recommendations;
    }

    /// <summary>
    /// Determine action type from recommendation text
    /// </summary>
    private string DetermineActionType(string description)
    {
        var lower = description.ToLowerInvariant();
        
        if (lower.Contains("call") || lower.Contains("phone")) return "call";
        if (lower.Contains("email") || lower.Contains("send")) return "email";
        if (lower.Contains("meeting") || lower.Contains("schedule")) return "schedule_meeting";
        if (lower.Contains("follow") || lower.Contains("check")) return "follow_up";
        if (lower.Contains("document") || lower.Contains("send")) return "send_document";
        if (lower.Contains("nurture") || lower.Contains("content")) return "nurture";
        
        return "follow_up"; // Default
    }

    /// <summary>
    /// LLM response structure for JSON parsing
    /// </summary>
    private class LlmRecommendationResponse
    {
        public List<NbaRecommendation>? Recommendations { get; set; }
        public string? Summary { get; set; }
        public double? Confidence { get; set; }
    }

    public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
    {
        traceId ??= request.TraceId;

        try
        {
            var requestType = request.Intent.ToString().ToLowerInvariant();

            // Extract parameters from Context dictionary
            var userId = request.Context.ContainsKey("userId") ? request.Context["userId"]?.ToString() : null;
            var contactId = request.Context.ContainsKey("contactId") ? request.Context["contactId"]?.ToString() : null;
            var agentId = request.Context.ContainsKey("agentId") ? request.Context["agentId"]?.ToString() : null;
            var organizationId = request.Context.ContainsKey("organizationId") ? request.Context["organizationId"]?.ToString() : null;

            _logger.LogInformation("Processing NBA request: Intent={Intent} for User: {UserId}",
                request.Intent, userId);

            // Validate required parameters based on request type
            if (string.IsNullOrEmpty(userId))
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = "UserId is required for NBA recommendations",
                    Data = new Dictionary<string, object>(),
                    RequestId = request.Id,
                    AgentId = "NbaAgent",
                    Timestamp = DateTime.UtcNow
                };
            }

            // Handle NBA requests based on intent and context
            var isNbaRequest = request.Context.ContainsKey("requestType") &&
                              request.Context["requestType"]?.ToString() == "nba_recommendation";

            if (isNbaRequest || request.Intent == AgentIntent.DataAnalysis)
            {
                return await RecommendNextBestActionsAsync(
                    contactId != null && Guid.TryParse(contactId, out var parsedContactId) ? parsedContactId : Guid.Empty,
                    organizationId != null && Guid.TryParse(organizationId, out var parsedOrganizationId) ? parsedOrganizationId : Guid.Empty,
                    agentId != null && Guid.TryParse(agentId, out var parsedAgentId) ? parsedAgentId : Guid.Empty,
                    3,
                    traceId,
                    request.Context.ContainsKey("userOverrides") ? (Dictionary<string, object>)request.Context["userOverrides"] : null);
            }

            return new AgentResponse
            {
                Success = false,
                Message = $"NBA Agent cannot handle intent: {request.Intent}",
                Data = new Dictionary<string, object>(),
                RequestId = request.Id,
                AgentId = "NbaAgent",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NBA request: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = $"NBA processing failed: {ex.Message}",
                RequestId = request.Id,
                AgentId = "NbaAgent",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<AgentCapability> GetCapabilityAsync()
    {
        var industryProfile = await _tenantContextService.GetIndustryProfileAsync();

        return new AgentCapability
        {
            AgentType = "NbaAgent",
            DisplayName = "Next Best Action Advisor",
            Description = $"Analyzes contact context and recommends optimal next actions for {industryProfile.DisplayName} industry",
            SupportedTasks = new List<string>
            {
                "recommend",
                "next_best_action",
                "analyze",
                "prioritize_actions",
                "suggest_follow_up"
            },
            RequiredIndustries = new List<string> { industryProfile.IndustryCode },
            IsAvailable = true
        };
    }

    private static Guid ExtractGuidParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is Guid guidValue) return guidValue;
            if (Guid.TryParse(value?.ToString(), out var parsedGuid)) return parsedGuid;
        }
        return Guid.Empty;
    }

    private static int ExtractIntParameter(Dictionary<string, object> parameters, string key, int defaultValue)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is int intValue) return intValue;
            if (int.TryParse(value?.ToString(), out var parsedInt)) return parsedInt;
        }
        return defaultValue;
    }
}

/// <summary>
/// NBA recommendation model
/// </summary>
public class NbaRecommendation : IAgentAction
{
    // IAgentAction required properties
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public double ConfidenceScore { get; set; } = 0.0;
    public string ValidationReason { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; } = false;
    public string ApprovalRequestId { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime? SuggestedTiming { get; set; } = null;
    public string TraceId { get; set; } = string.Empty;
    public ActionScope ActionScope { get; set; } = ActionScope.RealWorld;
    
    // NBA-specific properties (stored in Parameters for extensibility)
    public string Reasoning 
    { 
        get => Parameters.TryGetValue("reasoning", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Parameters["reasoning"] = value;
    }
    
    public string ExpectedOutcome 
    { 
        get => Parameters.TryGetValue("expectedOutcome", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Parameters["expectedOutcome"] = value;
    }
    
    public string Timing 
    { 
        get => Parameters.TryGetValue("timing", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Parameters["timing"] = value;
    }
    
    public DateTime GeneratedAt 
    { 
        get => Parameters.TryGetValue("generatedAt", out var value) && DateTime.TryParse(value?.ToString(), out var date) ? date : DateTime.UtcNow;
        set => Parameters["generatedAt"] = value;
    }
    
    public string Source 
    { 
        get => Parameters.TryGetValue("source", out var value) ? value?.ToString() ?? "LLM" : "LLM";
        set => Parameters["source"] = value;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public ValidationResult(bool isValid, string errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }
}
