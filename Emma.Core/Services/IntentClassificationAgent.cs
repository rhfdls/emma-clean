using Emma.Models.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Emma.Core.Services
{
    /// <summary>
    /// Intent Classification Agent that provides intelligent intent recognition and classification
    /// Implements the layered agent pattern with IntentClassificationService as the data layer
    /// </summary>
    public class IntentClassificationAgent : IIntentClassificationAgent
    {
        private readonly IIntentClassificationService _intentService;
        private readonly ITenantContextService _tenantContext;
        private readonly ILogger<IntentClassificationAgent> _logger;

        public IntentClassificationAgent(
            IIntentClassificationService intentService,
            ITenantContextService tenantContext,
            ILogger<IntentClassificationAgent> logger)
        {
            _intentService = intentService;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Processing Intent Classification request: Intent={Intent}, TraceId={TraceId}", 
                    request.Intent, traceId);

                // Extract user input from request
                var userInput = request.OriginalUserInput;
                if (string.IsNullOrEmpty(userInput))
                {
                    return new AgentResponse
                    {
                        Success = false,
                        Message = "User input is required for intent classification",
                        Data = new Dictionary<string, object>(),
                        RequestId = request.Id,
                        AgentId = "IntentClassificationAgent",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Handle intent classification requests
                if (request.Intent == AgentIntent.IntentClassification || 
                    request.Context.ContainsKey("requestType") && 
                    request.Context["requestType"]?.ToString() == "intent_classification")
                {
                    return await ClassifyIntentAsync(userInput, request.Context, traceId, 
                        request.Context.ContainsKey("userOverrides") ? (Dictionary<string, object>)request.Context["userOverrides"] : null);
                }

                return new AgentResponse
                {
                    Success = false,
                    Message = $"Intent Classification Agent cannot handle intent: {request.Intent}",
                    Data = new Dictionary<string, object>(),
                    RequestId = request.Id,
                    AgentId = "IntentClassificationAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Intent Classification request: {TraceId}", traceId);
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Intent Classification processing failed: {ex.Message}",
                    RequestId = request.Id,
                    AgentId = "IntentClassificationAgent",
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
                    AgentType = "IntentClassificationAgent",
                    DisplayName = "Intent Classification Agent",
                    Description = "Provides intelligent intent recognition and classification for user inputs with confidence scoring",
                    SupportedTasks = new List<string>
                    {
                        "classify_intent",
                        "analyze_request",
                        "route_to_agent",
                        "extract_entities"
                    },
                    RequiredIndustries = new List<string>(), // Available for all industries
                    IsAvailable = true,
                    Version = "1.0.0",
                    Configuration = new Dictionary<string, object>
                    {
                        ["SupportsMultipleIntents"] = true,
                        ["SupportsEntityExtraction"] = true,
                        ["SupportsConfidenceScoring"] = true,
                        ["SupportsContextualClassification"] = true,
                        ["MinConfidenceThreshold"] = _intentService.GetConfidenceThreshold(),
                        ["SupportedIntents"] = Enum.GetNames<AgentIntent>().ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Intent Classification Agent capability");
                
                // Return basic capability even on error
                return new AgentCapability
                {
                    AgentType = "IntentClassificationAgent",
                    DisplayName = "Intent Classification Agent",
                    Description = "Intent classification and recognition agent",
                    SupportedTasks = new List<string> { "Intent Classification" },
                    RequiredIndustries = new List<string>(),
                    IsAvailable = false
                };
            }
        }

        public async Task<AgentResponse> ClassifyIntentAsync(string userInput, Dictionary<string, object>? conversationContext = null, string? traceId = null, Dictionary<string, object>? userOverrides = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Classifying intent for input: {UserInput}, TraceId: {TraceId}, UserOverrides: {HasOverrides}", 
                    userInput, traceId, userOverrides?.Count > 0);

                // Add industry context if available
                var context = conversationContext ?? new Dictionary<string, object>();
                try
                {
                    var industryProfile = await _tenantContext.GetIndustryProfileAsync();
                    context["industry"] = industryProfile.IndustryCode;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get industry context for intent classification");
                }

                // Perform intent classification
                var classificationResult = await _intentService.ClassifyIntentAsync(userInput, context, traceId);

                return new AgentResponse
                {
                    Success = true,
                    Message = $"Intent classified as {classificationResult.Intent} with {classificationResult.Confidence:P1} confidence",
                    Data = new Dictionary<string, object>
                    {
                        ["UserInput"] = userInput,
                        ["ClassifiedIntent"] = classificationResult.Intent.ToString(),
                        ["Confidence"] = classificationResult.Confidence,
                        ["Reasoning"] = classificationResult.Reasoning,
                        ["ExtractedEntities"] = classificationResult.ExtractedEntities,
                        ["Urgency"] = classificationResult.Urgency.ToString(),
                        ["AlternativeIntents"] = classificationResult.AlternativeIntents.Select(alt => new
                        {
                            Intent = alt.Intent.ToString(),
                            Confidence = alt.Confidence
                        }).ToList(),
                        ["ClassificationTimestamp"] = classificationResult.ClassifiedAt,
                        ["IsHighConfidence"] = classificationResult.Confidence >= _intentService.GetConfidenceThreshold()
                    },
                    RequestId = Guid.NewGuid().ToString(),
                    AgentId = "IntentClassificationAgent",
                    Timestamp = DateTime.UtcNow,
                    Confidence = classificationResult.Confidence
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying intent for input: {UserInput}, TraceId: {TraceId}", userInput, traceId);
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Intent classification failed: {ex.Message}",
                    RequestId = Guid.NewGuid().ToString(),
                    AgentId = "IntentClassificationAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }
}
