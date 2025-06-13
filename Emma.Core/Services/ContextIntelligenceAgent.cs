using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services
{
    /// <summary>
    /// Context Intelligence Agent that provides intelligent context analysis and insights
    /// Implements the layered agent pattern with ContextIntelligenceService as the data layer
    /// </summary>
    public class ContextIntelligenceAgent : IContextIntelligenceAgent
    {
        private readonly IContextIntelligenceService _contextService;
        private readonly ITenantContextService _tenantContext;
        private readonly IPromptProvider _promptProvider;
        private readonly IAIFoundryService _aiFoundryService;
        private readonly ILogger<ContextIntelligenceAgent> _logger;

        public ContextIntelligenceAgent(
            IContextIntelligenceService contextService,
            ITenantContextService tenantContext,
            IPromptProvider promptProvider,
            IAIFoundryService aiFoundryService,
            ILogger<ContextIntelligenceAgent> logger)
        {
            _contextService = contextService;
            _tenantContext = tenantContext;
            _promptProvider = promptProvider;
            _aiFoundryService = aiFoundryService;
            _logger = logger;
        }

        public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Processing Context Intelligence request: Intent={Intent}, TraceId={TraceId}", 
                    request.Intent, traceId);

                // Extract parameters from request context
                var contactId = request.Context.TryGetValue("contactId", out var cId) ? 
                    Guid.TryParse(cId.ToString(), out var parsedContactId) ? parsedContactId : Guid.Empty : Guid.Empty;
                var organizationId = request.Context.TryGetValue("organizationId", out var oId) ? 
                    Guid.TryParse(oId.ToString(), out var parsedOrganizationId) ? parsedOrganizationId : Guid.Empty : Guid.Empty;
                var analysisType = request.Context.TryGetValue("analysisType", out var aType) ? 
                    aType.ToString()! : "interaction";

                // Validate required parameters
                if (contactId == Guid.Empty)
                {
                    return new AgentResponse
                    {
                        Success = false,
                        Message = "ContactId is required for context intelligence analysis",
                        Data = new Dictionary<string, object>(),
                        RequestId = request.Id,
                        AgentId = "ContextIntelligenceAgent",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Handle different types of context intelligence requests
                return request.Intent switch
                {
                    AgentIntent.InteractionAnalysis => await AnalyzeContextAsync(contactId, organizationId, analysisType, traceId, 
                        request.Context.ContainsKey("userOverrides") ? (Dictionary<string, object>)request.Context["userOverrides"] : null),
                    AgentIntent.DataAnalysis => await AnalyzeContextAsync(contactId, organizationId, "comprehensive", traceId,
                        request.Context.ContainsKey("userOverrides") ? (Dictionary<string, object>)request.Context["userOverrides"] : null),
                    _ => new AgentResponse
                    {
                        Success = false,
                        Message = $"Context Intelligence Agent cannot handle intent: {request.Intent}",
                        Data = new Dictionary<string, object>(),
                        RequestId = request.Id,
                        AgentId = "ContextIntelligenceAgent",
                        Timestamp = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Context Intelligence request: {TraceId}", traceId);
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Context Intelligence processing failed: {ex.Message}",
                    RequestId = request.Id,
                    AgentId = "ContextIntelligenceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<AgentCapability> GetCapabilityAsync()
        {
            try
            {
                var industryProfile = await _tenantContext.GetIndustryProfileAsync();
                
                return new AgentCapability
                {
                    AgentType = "ContextIntelligenceAgent",
                    DisplayName = "Context Intelligence Agent",
                    Description = "Provides intelligent context analysis, interaction insights, sentiment analysis, and behavioral predictions",
                    SupportedTasks = new List<string>
                    {
                        "analyze_context",
                        "extract_insights",
                        "identify_patterns",
                        "recommend_actions"
                    },
                    RequiredIndustries = new List<string>(), // Available for all industries
                    IsAvailable = true,
                    Version = "1.0.0",
                    Configuration = new Dictionary<string, object>
                    {
                        ["SupportsRealTimeAnalysis"] = true,
                        ["SupportsHistoricalAnalysis"] = true,
                        ["SupportsSentimentAnalysis"] = true,
                        ["SupportsPredictiveAnalytics"] = true,
                        ["MaxInteractionsPerAnalysis"] = 100
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Context Intelligence Agent capability");
                
                // Return basic capability even on error
                return new AgentCapability
                {
                    AgentType = "ContextIntelligenceAgent",
                    DisplayName = "Context Intelligence Agent",
                    Description = "Context analysis and insights agent",
                    SupportedTasks = new List<string> { "Context Analysis" },
                    RequiredIndustries = new List<string>(),
                    IsAvailable = false
                };
            }
        }

        public async Task<AgentResponse> AnalyzeContextAsync(Guid contactId, Guid organizationId, string analysisType, string? traceId = null, Dictionary<string, object>? userOverrides = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Analyzing context for Contact: {ContactId}, Type: {AnalysisType}, TraceId: {TraceId}, UserOverrides: {HasOverrides}", 
                    contactId, analysisType, traceId, userOverrides?.Count > 0);

                var results = new Dictionary<string, object>();

                // Perform different types of analysis based on request
                switch (analysisType.ToLowerInvariant())
                {
                    case "interaction":
                        // Analyze recent interaction if provided in context
                        var interactionContent = ""; // Would get from request context
                        var contactContext = await _contextService.AnalyzeInteractionAsync(
                            interactionContent,
                            null,
                            traceId);
                        results["InteractionAnalysis"] = contactContext;
                        break;

                    case "sentiment":
                        // Perform sentiment analysis
                        var sentimentResult = await _contextService.AnalyzeSentimentAsync("", traceId);
                        results["SentimentScore"] = sentimentResult;
                        break;

                    case "comprehensive":
                        // Perform comprehensive analysis
                        var comprehensiveContext = await _contextService.AnalyzeInteractionAsync(
                            "",
                            null,
                            traceId);
                        var recommendations = await _contextService.GenerateRecommendedActionsAsync(comprehensiveContext, traceId);
                        var closeProbability = await _contextService.PredictCloseProbabilityAsync(comprehensiveContext, traceId);
                        
                        results["ContactContext"] = comprehensiveContext;
                        results["RecommendedActions"] = recommendations;
                        results["CloseProbability"] = closeProbability;
                        break;

                    default:
                        return new AgentResponse
                        {
                            Success = false,
                            Message = $"Unknown analysis type: {analysisType}",
                            Data = new Dictionary<string, object>(),
                            RequestId = Guid.NewGuid().ToString(),
                            AgentId = "ContextIntelligenceAgent",
                            Timestamp = DateTime.UtcNow
                        };
                }

                return new AgentResponse
                {
                    Success = true,
                    Message = $"Context intelligence analysis completed for {analysisType}",
                    Data = new Dictionary<string, object>
                    {
                        ["ContactId"] = contactId.ToString(),
                        ["OrganizationId"] = organizationId.ToString(),
                        ["AnalysisType"] = analysisType,
                        ["Results"] = results,
                        ["AnalysisTimestamp"] = DateTime.UtcNow
                    },
                    RequestId = Guid.NewGuid().ToString(),
                    AgentId = "ContextIntelligenceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing context for Contact: {ContactId}, TraceId: {TraceId}", contactId, traceId);
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Context analysis failed: {ex.Message}",
                    RequestId = Guid.NewGuid().ToString(),
                    AgentId = "ContextIntelligenceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }
}
