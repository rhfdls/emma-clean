using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace Emma.Core.Services
{
    /// <summary>
    /// AI-powered context intelligence and interaction analysis service
    /// Extracts insights from agent-to-contact interactions
    /// </summary>
    public class ContextIntelligenceService : IContextIntelligenceService
    {
        private readonly IAIFoundryService _aiFoundryService;
        private readonly ILogger<ContextIntelligenceService> _logger;

        public ContextIntelligenceService(
            IAIFoundryService aiFoundryService,
            ILogger<ContextIntelligenceService> logger)
        {
            _aiFoundryService = aiFoundryService;
            _logger = logger;
        }

        public async Task<ContactContext> AnalyzeInteractionAsync(
            string interactionContent, 
            ContactContext? contactContext = null,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Analyzing interaction content, TraceId: {TraceId}", traceId);

                var systemPrompt = BuildInteractionAnalysisPrompt();
                var userPrompt = BuildInteractionUserPrompt(interactionContent, contactContext);

                var response = await _aiFoundryService.ProcessAgentRequestAsync(
                    systemPrompt, userPrompt, traceId);

                var analysisResult = ParseInteractionAnalysis(response, traceId);
                
                // Merge with existing contact context or create new
                var result = contactContext ?? new ContactContext();
                
                result.SentimentScore = analysisResult.SentimentScore;
                result.BuyingSignals = analysisResult.BuyingSignals;
                result.UrgencyLevel = analysisResult.UrgencyLevel;
                result.RecommendedActions = analysisResult.RecommendedActions;
                result.CloseProbability = analysisResult.CloseProbability;
                result.InteractionSummary = analysisResult.InteractionSummary;
                result.AnalysisTimestamp = DateTime.UtcNow;

                _logger.LogInformation("Interaction analyzed - Sentiment: {Sentiment}, Urgency: {Urgency}, Close Probability: {CloseProbability}, TraceId: {TraceId}",
                    result.SentimentScore, result.UrgencyLevel, result.CloseProbability, traceId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing interaction content, TraceId: {TraceId}", traceId);
                
                // Return existing context or empty context on error
                return contactContext ?? new ContactContext();
            }
        }

        public async Task<List<string>> GenerateRecommendedActionsAsync(
            ContactContext contactContext,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Generating recommended actions for contact {ContactId}, TraceId: {TraceId}",
                    contactContext.ContactId, traceId);

                var systemPrompt = BuildRecommendedActionsPrompt();
                var userPrompt = BuildContactContextPrompt(contactContext);

                var response = await _aiFoundryService.ProcessAgentRequestAsync(
                    systemPrompt, userPrompt, traceId);

                var actions = ParseRecommendedActions(response, traceId);
                
                _logger.LogInformation("Generated {Count} recommended actions, TraceId: {TraceId}",
                    actions.Count, traceId);

                return actions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommended actions, TraceId: {TraceId}", traceId);
                return new List<string>();
            }
        }

        public async Task<double> PredictCloseProbabilityAsync(
            ContactContext contactContext,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Predicting close probability for contact {ContactId}, TraceId: {TraceId}",
                    contactContext.ContactId, traceId);

                var systemPrompt = BuildCloseProbabilityPrompt();
                var userPrompt = BuildContactContextPrompt(contactContext);

                var response = await _aiFoundryService.ProcessAgentRequestAsync(
                    systemPrompt, userPrompt, traceId);

                var probability = ParseCloseProbability(response, traceId);
                
                _logger.LogInformation("Predicted close probability: {Probability}, TraceId: {TraceId}",
                    probability, traceId);

                return probability;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting close probability, TraceId: {TraceId}", traceId);
                return 0.5; // Default neutral probability
            }
        }

        public async Task<List<string>> ExtractBuyingSignalsAsync(
            string interactionContent,
            string? industry = null,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Extracting buying signals from interaction, Industry: {Industry}, TraceId: {TraceId}",
                    industry, traceId);

                var systemPrompt = BuildBuyingSignalsPrompt(industry);
                var userPrompt = $"INTERACTION CONTENT: {interactionContent}";

                var response = await _aiFoundryService.ProcessAgentRequestAsync(
                    systemPrompt, userPrompt, traceId);

                var signals = ParseBuyingSignals(response, traceId);
                
                _logger.LogInformation("Extracted {Count} buying signals, TraceId: {TraceId}",
                    signals.Count, traceId);

                return signals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting buying signals, TraceId: {TraceId}", traceId);
                return new List<string>();
            }
        }

        public async Task<double> AnalyzeSentimentAsync(
            string interactionContent,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Analyzing sentiment of interaction content, TraceId: {TraceId}", traceId);

                var systemPrompt = BuildSentimentAnalysisPrompt();
                var userPrompt = $"CONTENT TO ANALYZE: {interactionContent}";

                var response = await _aiFoundryService.ProcessAgentRequestAsync(
                    systemPrompt, userPrompt, traceId);

                var sentiment = ParseSentimentScore(response, traceId);
                
                _logger.LogInformation("Sentiment analyzed: {Sentiment}, TraceId: {TraceId}",
                    sentiment, traceId);

                return sentiment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sentiment, TraceId: {TraceId}", traceId);
                return 0.0; // Neutral sentiment on error
            }
        }

        public async Task<UrgencyLevel> DetermineUrgencyAsync(
            string interactionContent,
            ContactContext? contactContext = null,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Determining urgency level, TraceId: {TraceId}", traceId);

                var systemPrompt = BuildUrgencyAnalysisPrompt();
                var userPrompt = BuildUrgencyUserPrompt(interactionContent, contactContext);

                var response = await _aiFoundryService.ProcessAgentRequestAsync(
                    systemPrompt, userPrompt, traceId);

                var urgency = ParseUrgencyLevel(response, traceId);
                
                _logger.LogInformation("Urgency determined: {Urgency}, TraceId: {TraceId}",
                    urgency, traceId);

                return urgency;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining urgency, TraceId: {TraceId}", traceId);
                return UrgencyLevel.Medium; // Default to medium urgency
            }
        }

        public async Task<ContactContext> GetContactContextAsync(string contactId, string? traceId = null)
        {
            _logger.LogInformation("Getting contact context for ContactId: {ContactId}, TraceId: {TraceId}", contactId, traceId);
            
            // TODO: Implement actual contact context retrieval logic
            return new ContactContext
            {
                ContactId = Guid.TryParse(contactId, out var id) ? id : null,
                ContactName = "Unknown",
                AnalysisTimestamp = DateTime.UtcNow
            };
        }

        #region Prompt Building Methods

        private string BuildInteractionAnalysisPrompt()
        {
            return @"You are an AI interaction analyst for a CRM system. Analyze the provided interaction content and extract comprehensive insights.

ANALYSIS TASKS:
1. Sentiment Analysis (-1.0 to 1.0, where -1.0 is very negative, 0.0 is neutral, 1.0 is very positive)
2. Buying Signals Detection (specific phrases or behaviors indicating purchase intent)
3. Urgency Assessment (Low, Medium, High, Critical)
4. Recommended Actions (specific next steps based on the interaction)
5. Close Probability (0.0 to 1.0, likelihood of closing a deal)
6. Interaction Summary (brief summary of key points)

RESPONSE FORMAT (JSON):
{
    ""sentimentScore"": [number between -1.0 and 1.0],
    ""buyingSignals"": [""signal1"", ""signal2"", ...],
    ""urgencyLevel"": ""[Low|Medium|High|Critical]"",
    ""recommendedActions"": [""action1"", ""action2"", ...],
    ""closeProbability"": [number between 0.0 and 1.0],
    ""interactionSummary"": ""[brief summary]""
}";
        }

        private string BuildRecommendedActionsPrompt()
        {
            return @"You are an AI CRM advisor. Based on the contact context provided, generate specific, actionable recommendations for the next steps.

GUIDELINES:
- Provide 3-5 specific, actionable recommendations
- Consider the contact's current status, sentiment, and interaction history
- Prioritize actions that are most likely to advance the relationship
- Include timing recommendations when appropriate
- Consider industry-specific best practices

RESPONSE FORMAT (JSON):
{
    ""actions"": [""action1"", ""action2"", ""action3"", ...]
}";
        }

        private string BuildCloseProbabilityPrompt()
        {
            return @"You are an AI sales probability predictor. Based on the contact context, predict the likelihood of closing a deal.

FACTORS TO CONSIDER:
- Sentiment score and trend
- Buying signals present
- Interaction frequency and recency
- Contact status and engagement level
- Industry-specific patterns

RESPONSE FORMAT (JSON):
{
    ""probability"": [number between 0.0 and 1.0],
    ""reasoning"": ""[explanation of the prediction]""
}";
        }

        private string BuildBuyingSignalsPrompt(string? industry)
        {
            var industryContext = !string.IsNullOrEmpty(industry) 
                ? $"\nINDUSTRY CONTEXT: {industry} - Consider industry-specific buying signals."
                : "";

            return $@"You are an AI buying signals detector. Identify phrases, behaviors, or indicators that suggest purchase intent.

COMMON BUYING SIGNALS:
- Budget discussions
- Timeline mentions
- Decision-maker involvement
- Comparison requests
- Implementation questions
- Urgency indicators
- Pain point expressions{industryContext}

RESPONSE FORMAT (JSON):
{{
    ""signals"": [""signal1"", ""signal2"", ...]
}}";
        }

        private string BuildSentimentAnalysisPrompt()
        {
            return @"You are an AI sentiment analyzer. Analyze the emotional tone of the provided content.

SENTIMENT SCALE:
- -1.0: Very negative (angry, frustrated, dissatisfied)
- -0.5: Negative (disappointed, concerned)
- 0.0: Neutral (factual, no emotional indicators)
- 0.5: Positive (satisfied, interested)
- 1.0: Very positive (excited, enthusiastic, delighted)

RESPONSE FORMAT (JSON):
{
    ""sentiment"": [number between -1.0 and 1.0],
    ""reasoning"": ""[explanation of the sentiment analysis]""
}";
        }

        private string BuildUrgencyAnalysisPrompt()
        {
            return @"You are an AI urgency detector. Determine the urgency level of the interaction based on content and context.

URGENCY LEVELS:
- Low: General inquiry, no time pressure
- Medium: Standard business need, normal timeline
- High: Time-sensitive request, near-term deadline
- Critical: Immediate action required, urgent problem

URGENCY INDICATORS:
- Time-related keywords (urgent, ASAP, deadline, immediately)
- Problem severity (critical issue, system down, major problem)
- Business impact (losing money, missing opportunity, compliance issue)
- Escalation language (manager involved, legal implications)

RESPONSE FORMAT (JSON):
{
    ""urgency"": ""[Low|Medium|High|Critical]"",
    ""reasoning"": ""[explanation of urgency determination]""
}";
        }

        #endregion

        #region User Prompt Building Methods

        private string BuildInteractionUserPrompt(string interactionContent, ContactContext? contactContext)
        {
            var prompt = $"INTERACTION CONTENT: {interactionContent}";
            
            if (contactContext != null)
            {
                prompt += $"\n\nCONTACT CONTEXT:";
                if (!string.IsNullOrEmpty(contactContext.ContactName))
                    prompt += $"\nContact: {contactContext.ContactName}";
                if (!string.IsNullOrEmpty(contactContext.ContactStatus))
                    prompt += $"\nStatus: {contactContext.ContactStatus}";
                if (contactContext.LastInteraction.HasValue)
                    prompt += $"\nLast Interaction: {contactContext.LastInteraction:yyyy-MM-dd}";
                if (!string.IsNullOrEmpty(contactContext.InteractionSummary))
                    prompt += $"\nPrevious Summary: {contactContext.InteractionSummary}";
            }
            
            return prompt;
        }

        private string BuildContactContextPrompt(ContactContext contactContext)
        {
            var context = JsonSerializer.Serialize(contactContext, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            return $"CONTACT CONTEXT: {context}";
        }

        private string BuildUrgencyUserPrompt(string interactionContent, ContactContext? contactContext)
        {
            var prompt = $"INTERACTION CONTENT: {interactionContent}";
            
            if (contactContext != null)
            {
                prompt += $"\n\nADDITIONAL CONTEXT:";
                prompt += $"\nCurrent Urgency Level: {contactContext.UrgencyLevel}";
                if (contactContext.SentimentScore.HasValue)
                    prompt += $"\nSentiment Score: {contactContext.SentimentScore:F2}";
                if (contactContext.BuyingSignals.Any())
                    prompt += $"\nExisting Buying Signals: {string.Join(", ", contactContext.BuyingSignals)}";
            }
            
            return prompt;
        }

        #endregion

        #region Response Parsing Methods

        private InteractionAnalysisResult ParseInteractionAnalysis(string response, string traceId)
        {
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                
                return new InteractionAnalysisResult
                {
                    SentimentScore = jsonResponse.GetProperty("sentimentScore").GetDouble(),
                    BuyingSignals = ParseStringArray(jsonResponse, "buyingSignals"),
                    UrgencyLevel = ParseEnum<UrgencyLevel>(jsonResponse, "urgencyLevel", UrgencyLevel.Medium),
                    RecommendedActions = ParseStringArray(jsonResponse, "recommendedActions"),
                    CloseProbability = jsonResponse.GetProperty("closeProbability").GetDouble(),
                    InteractionSummary = jsonResponse.GetProperty("interactionSummary").GetString() ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing interaction analysis response, TraceId: {TraceId}", traceId);
                return new InteractionAnalysisResult();
            }
        }

        private List<string> ParseRecommendedActions(string response, string traceId)
        {
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                return ParseStringArray(jsonResponse, "actions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing recommended actions response, TraceId: {TraceId}", traceId);
                return new List<string>();
            }
        }

        private double ParseCloseProbability(string response, string traceId)
        {
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                return jsonResponse.GetProperty("probability").GetDouble();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing close probability response, TraceId: {TraceId}", traceId);
                return 0.5;
            }
        }

        private List<string> ParseBuyingSignals(string response, string traceId)
        {
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                return ParseStringArray(jsonResponse, "signals");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing buying signals response, TraceId: {TraceId}", traceId);
                return new List<string>();
            }
        }

        private double ParseSentimentScore(string response, string traceId)
        {
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                return jsonResponse.GetProperty("sentiment").GetDouble();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing sentiment response, TraceId: {TraceId}", traceId);
                return 0.0;
            }
        }

        private UrgencyLevel ParseUrgencyLevel(string response, string traceId)
        {
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
                return ParseEnum<UrgencyLevel>(jsonResponse, "urgency", UrgencyLevel.Medium);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing urgency response, TraceId: {TraceId}", traceId);
                return UrgencyLevel.Medium;
            }
        }

        private List<string> ParseStringArray(JsonElement jsonElement, string propertyName)
        {
            var result = new List<string>();
            
            if (jsonElement.TryGetProperty(propertyName, out var arrayElement))
            {
                foreach (var item in arrayElement.EnumerateArray())
                {
                    var value = item.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        result.Add(value);
                    }
                }
            }
            
            return result;
        }

        private T ParseEnum<T>(JsonElement jsonElement, string propertyName, T defaultValue) where T : struct, Enum
        {
            if (jsonElement.TryGetProperty(propertyName, out var property))
            {
                var value = property.GetString();
                if (!string.IsNullOrEmpty(value) && Enum.TryParse<T>(value, out var result))
                {
                    return result;
                }
            }
            
            return defaultValue;
        }

        #endregion
    }

    /// <summary>
    /// Internal class for interaction analysis results
    /// </summary>
    internal class InteractionAnalysisResult
    {
        public double SentimentScore { get; set; }
        public List<string> BuyingSignals { get; set; } = new();
        public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Medium;
        public List<string> RecommendedActions { get; set; } = new();
        public double CloseProbability { get; set; }
        public string InteractionSummary { get; set; } = string.Empty;
    }
}
