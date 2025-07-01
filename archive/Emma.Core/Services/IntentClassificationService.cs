using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace Emma.Core.Services
{
    /// <summary>
    /// Intent classification service using prompt engineering
    /// Abstracted for easy swapping when Microsoft's semantic routing matures
    /// </summary>
    public class IntentClassificationService : IIntentClassificationService
    {
        private readonly IAIFoundryService _aiFoundryService;
        private readonly ILogger<IntentClassificationService> _logger;
        private readonly double _confidenceThreshold;

        public IntentClassificationService(
            IAIFoundryService aiFoundryService,
            ILogger<IntentClassificationService> logger)
        {
            _aiFoundryService = aiFoundryService;
            _logger = logger;
            _confidenceThreshold = 0.7; // Default confidence threshold
        }

        public async Task<IntentClassificationResult> ClassifyIntentAsync(
            string userInput, 
            Dictionary<string, object>? context = null,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Classifying intent for input: {UserInput}, TraceId: {TraceId}", 
                    userInput, traceId);

                var systemPrompt = BuildIntentClassificationPrompt(context);
                var userPrompt = BuildUserPrompt(userInput, context);

                var response = await _aiFoundryService.ProcessAgentRequestAsync(
                    systemPrompt, userPrompt, traceId);

                var result = ParseIntentResponse(response, traceId);
                
                _logger.LogInformation("Intent classified as {Intent} with confidence {Confidence}, TraceId: {TraceId}",
                    result.Intent, result.Confidence, traceId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying intent for input: {UserInput}, TraceId: {TraceId}", 
                    userInput, traceId);
                
                // Return fallback intent
                return new IntentClassificationResult
                {
                    Intent = AgentIntent.GeneralInquiry,
                    Confidence = 0.5,
                    Reasoning = "Fallback due to classification error",
                    TraceId = traceId,
                    Urgency = UrgencyLevel.Medium
                };
            }
        }

        public double GetConfidenceThreshold()
        {
            return _confidenceThreshold;
        }

        public async Task UpdateWithFeedbackAsync(string userInput, AgentIntent actualIntent, string traceId)
        {
            _logger.LogInformation("Updating classification model with feedback - Input: {UserInput}, Actual: {ActualIntent}, TraceId: {TraceId}",
                userInput, actualIntent, traceId);
            
            // TODO: Implement feedback learning mechanism
            // This could involve storing feedback for model fine-tuning
            await Task.CompletedTask;
        }

        private string BuildIntentClassificationPrompt(Dictionary<string, object>? context)
        {
            var contextInfo = context != null ? JsonSerializer.Serialize(context) : "{}";
            
            return $@"You are an AI intent classifier for an AI-first CRM system. Your job is to analyze user input and classify it into one of the following intents:

AVAILABLE INTENTS:
- ContactManagement: Creating, updating, searching, or managing contact information
- InteractionAnalysis: Analyzing past interactions, sentiment analysis, or extracting insights
- SchedulingAndTasks: Scheduling meetings, setting reminders, or managing tasks
- Communication: Sending emails, messages, or other communications
- MarketIntelligence: Market research, competitive analysis, or business insights
- GeneralInquiry: General questions or requests that don't fit other categories
- DataAnalysis: Analyzing data, generating reports, or extracting metrics
- ReportGeneration: Creating reports, summaries, or documentation
- WorkflowAutomation: Automating processes or creating workflows

CONTEXT: {contextInfo}

INSTRUCTIONS:
1. Analyze the user input carefully
2. Consider the context provided
3. Classify the intent with high confidence
4. Provide reasoning for your classification
5. Determine urgency level (Low, Medium, High, Critical)
6. Extract relevant entities from the input
7. Suggest alternative intents if confidence is low

RESPONSE FORMAT (JSON):
{{
    ""intent"": ""[INTENT_NAME]"",
    ""confidence"": [0.0-1.0],
    ""reasoning"": ""[Explanation of classification]"",
    ""extractedEntities"": {{
        ""[entity_type]"": ""[entity_value]""
    }},
    ""urgency"": ""[Low|Medium|High|Critical]"",
    ""alternativeIntents"": [
        {{""intent"": ""[ALTERNATIVE_INTENT]"", ""confidence"": [0.0-1.0]}}
    ]
}}";
        }

        private string BuildUserPrompt(string userInput, Dictionary<string, object>? context)
        {
            var industry = context?.GetValueOrDefault("industry")?.ToString();
            var previousIntent = context?.GetValueOrDefault("previousIntent")?.ToString();
            
            var prompt = $"USER INPUT: {userInput}";
            
            if (!string.IsNullOrEmpty(industry))
            {
                prompt += $"\nINDUSTRY CONTEXT: {industry}";
            }
            
            if (!string.IsNullOrEmpty(previousIntent))
            {
                prompt += $"\nPREVIOUS INTENT: {previousIntent}";
            }
            
            return prompt;
        }

        private IntentClassificationResult ParseIntentResponse(string response, string traceId)
        {
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                
                var intentStr = jsonResponse.GetProperty("intent").GetString() ?? "GeneralInquiry";
                var confidence = jsonResponse.GetProperty("confidence").GetDouble();
                var reasoning = jsonResponse.GetProperty("reasoning").GetString() ?? "";
                var urgencyStr = jsonResponse.GetProperty("urgency").GetString() ?? "Medium";
                
                // Parse intent enum
                if (!Enum.TryParse<AgentIntent>(intentStr, out var intent))
                {
                    intent = AgentIntent.GeneralInquiry;
                }
                
                // Parse urgency enum
                if (!Enum.TryParse<UrgencyLevel>(urgencyStr, out var urgency))
                {
                    urgency = UrgencyLevel.Medium;
                }
                
                // Extract entities
                var extractedEntities = new Dictionary<string, object>();
                if (jsonResponse.TryGetProperty("extractedEntities", out var entitiesElement))
                {
                    foreach (var property in entitiesElement.EnumerateObject())
                    {
                        extractedEntities[property.Name] = property.Value.ToString() ?? "";
                    }
                }
                
                // Parse alternative intents
                var alternativeIntents = new List<(AgentIntent Intent, double Confidence)>();
                if (jsonResponse.TryGetProperty("alternativeIntents", out var alternativesElement))
                {
                    foreach (var alternative in alternativesElement.EnumerateArray())
                    {
                        var altIntentStr = alternative.GetProperty("intent").GetString() ?? "";
                        var altConfidence = alternative.GetProperty("confidence").GetDouble();
                        
                        if (Enum.TryParse<AgentIntent>(altIntentStr, out var altIntent))
                        {
                            alternativeIntents.Add((altIntent, altConfidence));
                        }
                    }
                }
                
                return new IntentClassificationResult
                {
                    Intent = intent,
                    Confidence = confidence,
                    Reasoning = reasoning,
                    ExtractedEntities = extractedEntities,
                    Urgency = urgency,
                    AlternativeIntents = alternativeIntents,
                    TraceId = traceId,
                    ClassifiedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing intent classification response: {Response}, TraceId: {TraceId}", 
                    response, traceId);
                
                // Return fallback result
                return new IntentClassificationResult
                {
                    Intent = AgentIntent.GeneralInquiry,
                    Confidence = 0.5,
                    Reasoning = "Fallback due to parsing error",
                    TraceId = traceId,
                    Urgency = UrgencyLevel.Medium
                };
            }
        }
    }
}
