using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Emma.Core.Agents;

/// <summary>
/// Intent Classification Agent implementation that provides intelligent intent recognition and classification.
/// </summary>
public class IntentClassificationAgent : AgentBase, IIntentClassificationAgent
{
    private const string AgentIdValue = "intent-classification-agent";
    private const string DisplayNameValue = "Intent Classification Agent";
    private const string DescriptionValue = "Classifies user intents from input text to route requests appropriately";
    private const string VersionValue = "1.0.0";
    
    private readonly ILogger<IntentClassificationAgent> _logger;
    private readonly IAIFoundryService _aiFoundryService;
    private readonly IEnumProvider _enumProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntentClassificationAgent"/> class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="aiFoundryService">The AI Foundry service for ML-based classification</param>
    /// <param name="enumProvider">The enum provider for dynamic intent categories</param>
    public IntentClassificationAgent(
        ILogger<IntentClassificationAgent> logger,
        IAIFoundryService aiFoundryService,
        IEnumProvider enumProvider)
        : base(AgentIdValue, DisplayNameValue, DescriptionValue, VersionValue, logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiFoundryService = aiFoundryService ?? throw new ArgumentNullException(nameof(aiFoundryService));
        _enumProvider = enumProvider ?? throw new ArgumentNullException(nameof(enumProvider));
        
        // Initialize capability with intent classification-specific information
        Capability = new AgentCapability
        {
            AgentId = AgentIdValue,
            AgentName = DisplayNameValue,
            Description = DescriptionValue,
            Version = VersionValue,
            SupportedTasks = new List<string>
            {
                "classify-intent",
                "list-available-intents",
                "train-model"
            },
            RequiredPermissions = new List<string>
            {
                "nlp:intent:classify",
                "nlp:intent:list"
            },
            Configuration = new Dictionary<string, object>()
        };
    }
    
    /// <inheritdoc />
    public override async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
            
        _logger.LogInformation("[{TraceId}] Processing intent classification request: {RequestType}", 
            traceId ?? "N/A", request.RequestType);
            
        try
        {
            // Route the request to the appropriate handler based on request type
            return request.RequestType?.ToLowerInvariant() switch
            {
                "classify-intent" => await HandleClassifyIntentAsync(request, traceId),
                "list-available-intents" => await HandleListIntentsAsync(request, traceId),
                "train-model" => await HandleTrainModelAsync(request, traceId),
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
            _logger.LogError(ex, "[{TraceId}] Error processing intent classification: {ErrorMessage}", 
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
    public async Task<AgentResponse> ClassifyIntentAsync(
        string userInput, 
        Dictionary<string, object>? conversationContext = null, 
        string? traceId = null, 
        Dictionary<string, object>? userOverrides = null)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return new AgentResponse
            {
                Success = false,
                Message = "User input cannot be empty",
                StatusCode = 400
            };
        }
        
        _logger.LogInformation("[{TraceId}] Classifying intent for input: {UserInput}", 
            traceId ?? "N/A", userInput);
            
        try
        {
            // Get available intents from the enum provider
            var intentEnum = _enumProvider.GetValues("IntentType");
            if (intentEnum == null || !intentEnum.Any())
            {
                _logger.LogWarning("[{TraceId}] No intent types found in enum provider", traceId ?? "N/A");
                return CreateFallbackResponse(userInput);
            }
            
            // Prepare the prompt for the AI model
            var prompt = CreateClassificationPrompt(userInput, intentEnum, conversationContext);
            
            // Call the AI service to classify the intent
            var classificationResult = await _aiFoundryService.ClassifyTextAsync(prompt, traceId);
            
            // Parse and validate the response
            var intentResult = ParseClassificationResult(classificationResult, intentEnum);
            
            // Apply any user overrides if provided
            if (userOverrides != null && userOverrides.TryGetValue("forceIntent", out var forcedIntent))
            {
                _logger.LogInformation("[{TraceId}] Applying forced intent override: {ForcedIntent}", 
                    traceId ?? "N/A", forcedIntent);
                    
                intentResult.Intent = forcedIntent.ToString();
                intentResult.Confidence = 1.0; // Max confidence for overridden intents
            }
            
            return new AgentResponse
            {
                Success = true,
                Data = intentResult,
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] Error classifying intent: {ErrorMessage}", 
                traceId ?? "N/A", ex.Message);
                
            // Fall back to a default response if classification fails
            return CreateFallbackResponse(userInput);
        }
    }
    
    private async Task<AgentResponse> HandleClassifyIntentAsync(AgentRequest request, string? traceId)
    {
        if (!request.Parameters.TryGetValue("userInput", out var userInput) || 
            string.IsNullOrWhiteSpace(userInput?.ToString()))
        {
            return new AgentResponse
            {
                Success = false,
                Message = "Missing or empty userInput parameter",
                StatusCode = 400
            };
        }
        
        // Extract conversation context if provided
        Dictionary<string, object>? conversationContext = null;
        if (request.Parameters.TryGetValue("conversationContext", out var contextObj) && 
            contextObj is Dictionary<string, object> contextDict)
        {
            conversationContext = contextDict;
        }
        
        // Extract user overrides if provided
        Dictionary<string, object>? userOverrides = null;
        if (request.Parameters.TryGetValue("userOverrides", out var overridesObj) && 
            overridesObj is Dictionary<string, object> overridesDict)
        {
            userOverrides = overridesDict;
        }
        
        return await ClassifyIntentAsync(
            userInput.ToString()!, 
            conversationContext, 
            traceId, 
            userOverrides);
    }
    
    private Task<AgentResponse> HandleListIntentsAsync(AgentRequest request, string? traceId)
    {
        try
        {
            var intents = _enumProvider.GetValues("IntentType") ?? new List<EnumValue>();
            
            return Task.FromResult(new AgentResponse
            {
                Success = true,
                Data = new { Intents = intents },
                StatusCode = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] Error listing intents: {ErrorMessage}", 
                traceId ?? "N/A", ex.Message);
                
            return Task.FromResult(new AgentResponse
            {
                Success = false,
                Message = $"Error listing intents: {ex.Message}",
                StatusCode = 500,
                ErrorDetails = ex.ToString()
            });
        }
    }
    
    private Task<AgentResponse> HandleTrainModelAsync(AgentRequest request, string? traceId)
    {
        // Implementation for model training would go here
        return Task.FromResult(new AgentResponse
        {
            Success = true,
            Message = "Model training not yet implemented",
            StatusCode = 501 // Not Implemented
        });
    }
    
    private string CreateClassificationPrompt(
        string userInput, 
        List<EnumValue> intentTypes, 
        Dictionary<string, object>? conversationContext)
    {
        // Build a prompt that includes the available intent types
        var intentDescriptions = string.Join("\n", 
            intentTypes.Select(i => $"- {i.Value}: {i.Description ?? "No description"}"));
        
        var prompt = $"""
        Analyze the following user input and classify its intent based on the available intent types.
        
        Available intents:
        {intentDescriptions}
        
        User input: ""{userInput}""
        
        """;
        
        // Add conversation context if available
        if (conversationContext != null && conversationContext.Count > 0)
        {
            prompt += "\nConversation context:\n" + 
                    JsonSerializer.Serialize(conversationContext, new JsonSerializerOptions { WriteIndented = true });
        }
        
        prompt += "\n\nPlease respond with a JSON object containing the following fields:\n" +
                "- intent: The most likely intent (must be one of the values from the list above)\n" +
                "- confidence: A confidence score between 0 and 1\n" +
                "- explanation: A brief explanation of why this intent was chosen";
                
        return prompt;
    }
    
    private dynamic ParseClassificationResult(string aiResponse, List<EnumValue> validIntents)
    {
        try
        {
            // Try to parse the JSON response
            var result = JsonSerializer.Deserialize<JsonElement>(aiResponse);
            
            // Extract the intent and validate it against known values
            var intent = result.GetProperty("intent").GetString() ?? "unknown";
            var confidence = result.GetProperty("confidence").GetDouble();
            var explanation = result.GetProperty("explanation").GetString() ?? "No explanation provided";
            
            // Validate that the intent is one of the known values
            if (!validIntents.Any(i => i.Value.Equals(intent, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("AI returned invalid intent: {Intent}", intent);
                return CreateFallbackResponse("unknown");
            }
            
            return new 
            {
                Intent = intent,
                Confidence = Math.Clamp(confidence, 0, 1),
                Explanation = explanation,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI classification response: {Response}", aiResponse);
            return CreateFallbackResponse("error");
        }
    }
    
    private AgentResponse CreateFallbackResponse(string userInput, string intent = "unknown")
    {
        return new AgentResponse
        {
            Success = true,
            Data = new 
            {
                Intent = intent,
                Confidence = 0.1, // Low confidence for fallback
                Explanation = "Unable to determine intent with high confidence",
                IsFallback = true,
                Timestamp = DateTime.UtcNow
            },
            StatusCode = 200
        };
    }
}
