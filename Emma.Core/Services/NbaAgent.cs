using Emma.Core.Models;
using Emma.Core.Industry;
using Emma.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Emma.Core.Services;

/// <summary>
/// NBA (Next Best Action) Agent - Analyzes contact context and provides intelligent recommendations
/// Uses LLM-powered intelligence with dynamic prompts for context-aware recommendations
/// </summary>
public class NbaAgent : INbaAgent
{
    private readonly INbaContextService _contextService;
    private readonly ITenantContextService _tenantContextService;
    private readonly IPromptProvider _promptProvider;
    private readonly IAIFoundryService _aiFoundryService;
    private readonly ILogger<NbaAgent> _logger;

    public NbaAgent(
        INbaContextService contextService,
        ITenantContextService tenantContextService,
        IPromptProvider promptProvider,
        IAIFoundryService aiFoundryService,
        ILogger<NbaAgent> logger)
    {
        _contextService = contextService;
        _tenantContextService = tenantContextService;
        _promptProvider = promptProvider;
        _aiFoundryService = aiFoundryService;
        _logger = logger;
    }

    public async Task<AgentResponse> RecommendNextBestActionsAsync(
        Guid contactId,
        Guid organizationId,
        Guid requestingAgentId,
        int maxRecommendations = 3,
        string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("🎯 NBA Agent: Generating LLM-powered recommendations for Contact: {ContactId}, Organization: {OrganizationId}, TraceId: {TraceId}",
                contactId, organizationId, traceId);

            // Get NBA context (includes interactions, summary, and state)
            var nbaContext = await _contextService.GetNbaContextAsync(
                contactId, organizationId, requestingAgentId, 5, 10, true);

            // Get contact state
            var contactState = await _contextService.GetContactStateAsync(contactId, organizationId);

            if (contactState == null)
            {
                _logger.LogWarning("Contact state not found for Contact: {ContactId}", contactId);
                return new AgentResponse
                {
                    Success = false,
                    Message = "Contact state not found",
                    RequestId = traceId,
                    AgentId = "NbaAgent",
                    Timestamp = DateTime.UtcNow
                };
            }

            // Get tenant context for industry profile
            var tenantContext = await _tenantContextService.GetTenantContextAsync(organizationId);
            var industryProfile = tenantContext?.IndustryProfile ?? new Emma.Core.Industry.Profiles.RealEstateProfile();

            // Build LLM prompts using dynamic prompt provider
            var systemPrompt = await _promptProvider.GetSystemPromptAsync("NbaAgent", industryProfile);
            
            var promptContext = new Dictionary<string, object>
            {
                ["ContactId"] = contactId.ToString(),
                ["ContactSummary"] = nbaContext.ContactSummary ?? "No summary available",
                ["RecentInteractions"] = FormatInteractions(nbaContext.RecentInteractions),
                ["CurrentStage"] = contactState.CurrentStage ?? "Unknown",
                ["EngagementMetrics"] = FormatEngagementMetrics(nbaContext),
                ["MaxRecommendations"] = maxRecommendations,
                ["AgentType"] = "NbaAgent",
                ["IndustryCode"] = industryProfile.IndustryCode
            };

            var userPrompt = await _promptProvider.BuildPromptAsync("ContactAnalysis", promptContext);

            _logger.LogDebug("🤖 NBA Agent: Calling LLM with system prompt length: {SystemLength}, user prompt length: {UserLength}",
                systemPrompt.Length, userPrompt.Length);

            // Call LLM for intelligent recommendations
            var llmResponse = await _aiFoundryService.ProcessAgentRequestAsync(systemPrompt, userPrompt, traceId);

            _logger.LogDebug("🎯 NBA Agent: LLM response received, length: {ResponseLength}", llmResponse.Length);

            // Parse LLM response into structured recommendations
            var recommendations = ParseLlmRecommendations(llmResponse, maxRecommendations);

            // Create successful response
            var response = new AgentResponse
            {
                Success = true,
                Message = $"Generated {recommendations.Count} NBA recommendations using LLM intelligence",
                RequestId = traceId,
                AgentId = "NbaAgent",
                Timestamp = DateTime.UtcNow,
                Data = new Dictionary<string, object>
                {
                    ["recommendations"] = recommendations,
                    ["contactStage"] = contactState.CurrentStage ?? "Unknown",
                    ["analysisMethod"] = "LLM-Powered",
                    ["promptVersion"] = "Dynamic",
                    ["industryProfile"] = industryProfile.IndustryCode
                }
            };

            _logger.LogInformation("✅ NBA Agent: Successfully generated {Count} LLM-powered recommendations for Contact: {ContactId}",
                recommendations.Count, contactId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ NBA Agent: Error generating recommendations for Contact: {ContactId}, TraceId: {TraceId}",
                contactId, traceId);

            return new AgentResponse
            {
                Success = false,
                Message = $"Error generating NBA recommendations: {ex.Message}",
                RequestId = traceId,
                AgentId = "NbaAgent",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Format recent interactions for LLM context
    /// </summary>
    private string FormatInteractions(List<Emma.Core.Models.InteractionSummary>? interactions)
    {
        if (interactions == null || !interactions.Any())
            return "No recent interactions available";

        var formatted = interactions.Take(5).Select(i => 
            $"- {i.InteractionDate:yyyy-MM-dd}: {i.InteractionType} - {i.Summary ?? "No summary"}");
        
        return string.Join("\n", formatted);
    }

    /// <summary>
    /// Format engagement metrics for LLM context
    /// </summary>
    private string FormatEngagementMetrics(Emma.Core.Models.NbaContext nbaContext)
    {
        return $@"
- Total Interactions: {nbaContext.RecentInteractions?.Count ?? 0}
- Contact Summary: {nbaContext.ContactSummary ?? "Not available"}
- Last Activity: {nbaContext.RecentInteractions?.FirstOrDefault()?.InteractionDate:yyyy-MM-dd ?? "Unknown"}";
    }

    /// <summary>
    /// Parse LLM response into structured NextBestAction recommendations
    /// </summary>
    private List<NextBestAction> ParseLlmRecommendations(string llmResponse, int maxRecommendations)
    {
        try
        {
            // Try to parse as JSON first
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Look for JSON structure in response
            var jsonStart = llmResponse.IndexOf('{');
            var jsonEnd = llmResponse.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsedResponse = JsonSerializer.Deserialize<LlmRecommendationResponse>(jsonContent, jsonOptions);
                
                if (parsedResponse?.Recommendations != null)
                {
                    return parsedResponse.Recommendations.Take(maxRecommendations).ToList();
                }
            }

            // Fallback: Parse text-based response
            return ParseTextRecommendations(llmResponse, maxRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM recommendations, using fallback parsing");
            return ParseTextRecommendations(llmResponse, maxRecommendations);
        }
    }

    /// <summary>
    /// Fallback text parsing for LLM recommendations
    /// </summary>
    private List<NextBestAction> ParseTextRecommendations(string response, int maxRecommendations)
    {
        var recommendations = new List<NextBestAction>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        int priority = 1;
        foreach (var line in lines.Take(maxRecommendations))
        {
            if (line.Trim().Length > 10) // Skip very short lines
            {
                recommendations.Add(new NextBestAction
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
            recommendations.Add(new NextBestAction
            {
                ActionType = "follow_up",
                Priority = 1,
                Description = "Follow up with contact to maintain engagement",
                Reasoning = "Default recommendation when LLM parsing fails",
                Timing = "Within 24 hours",
                ExpectedOutcome = "Maintain contact relationship"
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
        public List<NextBestAction>? Recommendations { get; set; }
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
                    contactId != null ? Guid.Parse(contactId) : Guid.Empty,
                    organizationId != null ? Guid.Parse(organizationId) : Guid.Empty,
                    agentId != null ? Guid.Parse(agentId) : Guid.Empty,
                    3,
                    traceId);
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
public class NextBestAction
{
    public string ActionType { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
}
