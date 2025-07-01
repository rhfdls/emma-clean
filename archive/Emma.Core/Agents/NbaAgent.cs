using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using Emma.Models.Models;

namespace Emma.Core.Agents;

/// <summary>
/// NBA (Next Best Action) Agent implementation that provides intelligent recommendations
/// based on contact context and business rules.
/// </summary>
public class NbaAgent : AgentBase
{
    private const string AgentIdValue = "nba-agent";
    private const string DisplayNameValue = "Next Best Action Agent";
    private const string DescriptionValue = "Provides intelligent recommendations based on contact context and business rules";
    private const string VersionValue = "1.0.0";
    
    private readonly ILogger<NbaAgent> _logger;
    private readonly INbaContextService _nbaContextService;
    private readonly IActionRelevanceValidator _actionRelevanceValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="NbaAgent"/> class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="nbaContextService">The NBA context service</param>
    /// <param name="actionRelevanceValidator">The action relevance validator</param>
    public NbaAgent(
        ILogger<NbaAgent> logger,
        INbaContextService nbaContextService,
        IActionRelevanceValidator actionRelevanceValidator)
        : base(AgentIdValue, DisplayNameValue, DescriptionValue, VersionValue, logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _nbaContextService = nbaContextService ?? throw new ArgumentNullException(nameof(nbaContextService));
        _actionRelevanceValidator = actionRelevanceValidator ?? throw new ArgumentNullException(nameof(actionRelevanceValidator));
        
        // Initialize capability with NBA-specific information
        Capability = new AgentCapability
        {
            AgentId = AgentIdValue,
            AgentName = DisplayNameValue,
            Description = DescriptionValue,
            Version = VersionValue,
            SupportedTasks = new List<string>
            {
                "recommend-next-best-actions",
                "validate-action-relevance",
                "analyze-contact-context"
            },
            RequiredPermissions = new List<string>
            {
                "contacts:read",
                "interactions:read",
                "recommendations:write"
            },
            Configuration = new Dictionary<string, object>()
        };
    }
    
    /// <inheritdoc />
    public async Task<AgentResponse> RecommendNextBestActionsAsync(
        Guid contactId,
        Guid organizationId,
        Guid requestingAgentId,
        int maxRecommendations = 3,
        string? traceId = null,
        Dictionary<string, object>? userOverrides = null)
    {
        _logger.LogInformation("[{TraceId}] Getting next best actions for contact {ContactId} (max: {MaxRecommendations})", 
            traceId ?? "N/A", contactId, maxRecommendations);
            
        try
        {
            // Get the NBA context for this contact
            var nbaContext = await _nbaContextService.GetNbaContextAsync(contactId, organizationId, traceId);
            
            if (nbaContext == null)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Could not load NBA context for contact {contactId}",
                    StatusCode = 404
                };
            }
            
            // Generate recommendations based on the context
            var recommendations = await GenerateRecommendationsAsync(nbaContext, maxRecommendations, traceId);
            
            // Apply any user overrides
            if (userOverrides != null && userOverrides.Count > 0)
            {
                recommendations = ApplyUserOverrides(recommendations, userOverrides);
            }
            
            // Validate action relevance
            var validatedRecommendations = new List<RecommendedAction>();
            foreach (var recommendation in recommendations)
            {
                var validationResult = await _actionRelevanceValidator.ValidateAsync(recommendation, nbaContext, traceId);
                if (validationResult.IsRelevant)
                {
                    validatedRecommendations.Add(recommendation);
                }
                
                // Don't exceed max recommendations
                if (validatedRecommendations.Count >= maxRecommendations)
                    break;
            }
            
            return new AgentResponse
            {
                Success = true,
                Data = new { Recommendations = validatedRecommendations },
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] Error generating NBA recommendations: {ErrorMessage}", 
                traceId ?? "N/A", ex.Message);
                
            return new AgentResponse
            {
                Success = false,
                Message = $"Error generating recommendations: {ex.Message}",
                StatusCode = 500,
                ErrorDetails = ex.ToString()
            };
        }
    }
    
    /// <inheritdoc />
    public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
            
        _logger.LogInformation("[{TraceId}] Processing NBA agent request: {RequestType}", 
            traceId ?? "N/A", request.RequestType);
            
        try
        {
            // Route the request to the appropriate handler based on request type
            return request.RequestType?.ToLowerInvariant() switch
            {
                "recommend-next-best-actions" => await HandleRecommendNextBestActionsAsync(request, traceId),
                "validate-action-relevance" => await HandleValidateActionRelevanceAsync(request, traceId),
                "analyze-contact-context" => await HandleAnalyzeContactContextAsync(request, traceId),
                _ => new AgentResponse
                {
                    Success = false,
                    Message = $"Unsupported request type: {request.RequestType}",
                    StatusCode = 400
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] Error processing NBA agent request: {ErrorMessage}", 
                traceId ?? "N/A", ex.Message);
                
            return new AgentResponse
            {
                Success = false,
                Message = $"Error processing request: {ex.Message}",
                StatusCode = 500,
                ErrorDetails = ex.ToString()
            };
        }
    }
    
    /// <inheritdoc />
    public override async Task<AgentHealthStatus> GetHealthStatusAsync()
    {
        var status = await base.GetHealthStatusAsync();
        
        // Add NBA-specific health metrics
        status.Metrics["recommendations_generated"] = 0; // Would track actual metrics in a real implementation
        status.Metrics["average_processing_time_ms"] = 0;
        
        return status;
    }
    
    private async Task<AgentResponse> HandleRecommendNextBestActionsAsync(AgentRequest request, string? traceId)
    {
        if (!request.Parameters.TryGetValue("contactId", out var contactIdObj) || 
            !Guid.TryParse(contactIdObj?.ToString(), out var contactId))
        {
            return new AgentResponse
            {
                Success = false,
                Message = "Missing or invalid contactId parameter",
                StatusCode = 400
            };
        }
        
        if (!request.Parameters.TryGetValue("organizationId", out var orgIdObj) || 
            !Guid.TryParse(orgIdObj?.ToString(), out var organizationId))
        {
            return new AgentResponse
            {
                Success = false,
                Message = "Missing or invalid organizationId parameter",
                StatusCode = 400
            };
        }
        
        var requestingAgentId = Guid.Empty;
        if (request.Parameters.TryGetValue("requestingAgentId", out var agentIdObj))
        {
            Guid.TryParse(agentIdObj?.ToString(), out requestingAgentId);
        }
        
        var maxRecommendations = 3;
        if (request.Parameters.TryGetValue("maxRecommendations", out var maxRecsObj) && 
            int.TryParse(maxRecsObj?.ToString(), out var maxRecs))
        {
            maxRecommendations = maxRecs;
        }
        
        Dictionary<string, object>? userOverrides = null;
        if (request.Parameters.TryGetValue("userOverrides", out var overridesObj) && 
            overridesObj is Dictionary<string, object> overrides)
        {
            userOverrides = overrides;
        }
        
        return await RecommendNextBestActionsAsync(
            contactId, 
            organizationId, 
            requestingAgentId, 
            maxRecommendations, 
            traceId, 
            userOverrides);
    }
    
    private Task<AgentResponse> HandleValidateActionRelevanceAsync(AgentRequest request, string? traceId)
    {
        // Implementation for action relevance validation
        return Task.FromResult(new AgentResponse
        {
            Success = true,
            Message = "Action relevance validation not yet implemented",
            StatusCode = 501 // Not Implemented
        });
    }
    
    private Task<AgentResponse> HandleAnalyzeContactContextAsync(AgentRequest request, string? traceId)
    {
        // Implementation for contact context analysis
        return Task.FromResult(new AgentResponse
        {
            Success = true,
            Message = "Contact context analysis not yet implemented",
            StatusCode = 501 // Not Implemented
        });
    }
    
    private Task<List<RecommendedAction>> GenerateRecommendationsAsync(
        NbaContext context, 
        int maxRecommendations, 
        string? traceId)
    {
        // This is a simplified implementation. In a real system, this would use ML models,
        // business rules, and other logic to generate recommendations.
        
        var recommendations = new List<RecommendedAction>();
        
        // Example recommendation logic
        if (context.RecentInteractions.Count == 0)
        {
            recommendations.Add(new RecommendedAction
            {
                Id = Guid.NewGuid(),
                Type = "InitialContact",
                Title = "Send welcome message",
                Description = "Send a welcome message to the new contact",
                Priority = 1,
                Confidence = 0.9,
                Metadata = new Dictionary<string, object>
                {
                    ["template"] = "welcome-message-v1",
                    ["channel"] = "email"
                },
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }
        
        // Add more recommendation logic here based on the context
        
        return Task.FromResult(recommendations.Take(maxRecommendations).ToList());
    }
    
    private List<RecommendedAction> ApplyUserOverrides(
        List<RecommendedAction> recommendations,
        Dictionary<string, object> userOverrides)
    {
        // Apply any user overrides to the recommendations
        // This is a simplified implementation
        return recommendations;
    }

    /// <inheritdoc />
    public Task<AgentCapability> GetCapabilityAsync()
    {
        return Task.FromResult(Capability);
    }
}

