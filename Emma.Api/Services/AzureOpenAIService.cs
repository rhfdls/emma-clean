using Azure.AI.OpenAI;
using Emma.Models.Models;
using Microsoft.Extensions.Options;
using Emma.Core.Config;
using System.Text.Json;

namespace Emma.Api.Services;

/// <summary>
/// Azure OpenAI service implementation for NBA context management
/// </summary>
public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly AzureOpenAIConfig _config;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(
        OpenAIClient openAIClient,
        IOptions<AzureOpenAIConfig> config,
        ILogger<AzureOpenAIService> logger)
    {
        _openAIClient = openAIClient;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generates vector embeddings for interaction content
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(string content, string model = "text-embedding-ada-002")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Empty content provided for embedding generation");
                return new float[1536]; // Return zero vector for empty content
            }

            // Truncate content if too long (embedding models have token limits)
            var truncatedContent = content.Length > 8000 ? content.Substring(0, 8000) : content;

            var response = await _openAIClient.GetEmbeddingsAsync(
                new EmbeddingsOptions(model, new[] { truncatedContent }));

            var embedding = response.Value.Data[0].Embedding.ToArray();
            
            _logger.LogDebug("Generated embedding for content of length {ContentLength}, embedding dimension: {EmbeddingDimension}", 
                content.Length, embedding.Length);

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for content");
            // Return zero vector as fallback
            return new float[1536];
        }
    }

    /// <summary>
    /// Generates or updates a rolling summary for a client
    /// </summary>
    public async Task<string> UpdateRollingSummaryAsync(
        string? existingSummary, 
        Interaction newInteraction, 
        List<Interaction> recentInteractions)
    {
        try
        {
            var systemPrompt = @"You are an AI assistant helping to maintain concise, up-to-date client summaries for real estate agents. 
Your task is to update a rolling summary that captures the client's journey, preferences, concerns, and current status.

Guidelines:
- Keep summaries concise but informative (200-500 words)
- Focus on actionable insights and client preferences
- Include key milestones, objections, and next steps
- Maintain a professional, factual tone
- Highlight any urgency or time-sensitive matters
- Note property preferences, budget, timeline, and decision-making process";

            var userPrompt = $@"Please update the client summary with the latest interaction.

EXISTING SUMMARY:
{existingSummary ?? "No previous summary - this is a new client."}

NEW INTERACTION:
Type: {newInteraction.Type}
Date: {newInteraction.Timestamp:yyyy-MM-dd HH:mm}
Content: {newInteraction.Content ?? "No content"}

RECENT CONTEXT (last {recentInteractions.Count} interactions):
{string.Join("\n", recentInteractions.Take(3).Select(i => 
    $"- {i.Timestamp:MM/dd} {i.Type}: {(i.Content?.Substring(0, Math.Min(100, i.Content.Length)) ?? "No content")}"))}

Please provide an updated summary that incorporates the new interaction while maintaining the most important historical context.";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 600,
                Temperature = 0.3f
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var updatedSummary = response.Value.Choices[0].Message.Content;

            _logger.LogInformation("Updated rolling summary for interaction {InteractionId}", newInteraction.Id);
            return updatedSummary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rolling summary for interaction {InteractionId}", newInteraction.Id);
            
            // Fallback: simple concatenation
            var fallbackSummary = existingSummary ?? "New client.";
            return $"{fallbackSummary} Latest: {newInteraction.Type} on {newInteraction.Timestamp:yyyy-MM-dd}.";
        }
    }

    /// <summary>
    /// Extracts key entities and topics from interaction content
    /// </summary>
    public async Task<Dictionary<string, object>> ExtractEntitiesAsync(string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new Dictionary<string, object>();
            }

            var systemPrompt = @"Extract key entities and topics from real estate interaction content. 
Return a JSON object with the following structure:
{
  ""properties"": [""address or property descriptions""],
  ""locations"": [""cities, neighborhoods, areas""],
  ""price_ranges"": [""budget or price mentions""],
  ""timeline"": [""dates, deadlines, timeframes""],
  ""people"": [""names of people mentioned""],
  ""topics"": [""main discussion topics""],
  ""concerns"": [""client concerns or objections""],
  ""preferences"": [""client preferences or requirements""]
}";

            var userPrompt = $"Extract entities from this content:\n\n{content}";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 400,
                Temperature = 0.1f
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var jsonResponse = response.Value.Choices[0].Message.Content;

            // Parse JSON response
            var entities = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
            return entities ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting entities from content");
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Analyzes sentiment of interaction content
    /// </summary>
    public async Task<double> AnalyzeSentimentAsync(string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return 0.0; // Neutral sentiment for empty content
            }

            var systemPrompt = @"Analyze the sentiment of this real estate interaction content. 
Return only a single number between -1.0 (very negative) and 1.0 (very positive), where 0.0 is neutral.
Consider the client's satisfaction, enthusiasm, concerns, and overall tone.";

            var userPrompt = $"Analyze sentiment: {content}";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 10,
                Temperature = 0.1f
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var sentimentText = response.Value.Choices[0].Message.Content.Trim();

            if (double.TryParse(sentimentText, out double sentiment))
            {
                return Math.Max(-1.0, Math.Min(1.0, sentiment)); // Clamp to valid range
            }

            return 0.0; // Default to neutral if parsing fails
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            return 0.0;
        }
    }

    /// <summary>
    /// Generates Next Best Action recommendations based on context
    /// </summary>
    public async Task<string> GenerateNbaRecommendationsAsync(NbaContext context, string? scenario = null)
    {
        try
        {
            var systemPrompt = @"You are an AI assistant helping real estate agents determine the Next Best Action (NBA) for their clients.
Analyze the provided client context and recommend 2-3 specific, actionable next steps.

Guidelines:
- Be specific and actionable
- Consider the client's current stage, preferences, and timeline
- Address any concerns or objections
- Suggest appropriate resources or follow-ups
- Prioritize actions by urgency and impact
- Keep recommendations concise but detailed enough to be actionable";

            var contextSummary = BuildContextSummary(context);
            var userPrompt = $@"Based on this client context, what are the next best actions?

{contextSummary}

{(string.IsNullOrEmpty(scenario) ? "" : $"Current Scenario: {scenario}")}

Please provide 2-3 specific next best actions in order of priority.";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 500,
                Temperature = 0.4f
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var recommendations = response.Value.Choices[0].Message.Content;

            _logger.LogInformation("Generated NBA recommendations for client {ContactId}", context.ContactId);
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating NBA recommendations for client {ContactId}", context.ContactId);
            return "Unable to generate recommendations at this time. Please review client context manually.";
        }
    }

    /// <summary>
    /// Suggests state transitions based on interaction content
    /// </summary>
    public async Task<(string? newState, string? reason)> SuggestStateTransitionAsync(
        ContactState currentState, 
        Interaction interaction)
    {
        try
        {
            var systemPrompt = @"Analyze a real estate client interaction to determine if a state transition should occur.
Current possible states: Initial Contact, Qualifying, Property Search, Viewing Properties, Making Offers, Under Contract, Closing, Closed, On Hold

Return JSON with this structure:
{
  ""shouldTransition"": true/false,
  ""newState"": ""new state name or null"",
  ""reason"": ""brief explanation""
}";

            var userPrompt = $@"Current State: {currentState.CurrentStage}
Interaction Type: {interaction.Type}
Interaction Content: {interaction.Content ?? "No content"}

Should the client state change?";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _config.ChatDeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                },
                MaxTokens = 150,
                Temperature = 0.2f
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var jsonResponse = response.Value.Choices[0].Message.Content;

            var result = JsonSerializer.Deserialize<StateTransitionResult>(jsonResponse);
            
            if (result?.ShouldTransition == true && !string.IsNullOrEmpty(result.NewState))
            {
                return (result.NewState, result.Reason);
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting state transition");
            return (null, null);
        }
    }

    private string BuildContextSummary(NbaContext context)
    {
        var summary = $@"CLIENT CONTEXT:

SUMMARY: {context.RollingSummary?.SummaryText ?? "No summary available"}

CURRENT STATE: {context.CurrentState?.CurrentStage ?? "Unknown"}
NEXT MILESTONE: {context.CurrentState?.NextMilestone ?? "Not set"}
PRIORITY: {context.CurrentState?.Priority ?? "Medium"}

RECENT INTERACTIONS ({context.RecentInteractions.Count}):
{string.Join("\n", context.RecentInteractions.Take(3).Select(i => 
    $"- {i.Timestamp:MM/dd} {i.Type}: {(i.Content?.Substring(0, Math.Min(100, i.Content.Length)) ?? "No content")}"))}

RELEVANT INTERACTIONS ({context.RelevantInteractions.Count}):
{string.Join("\n", context.RelevantInteractions.Take(3).Select(ri => 
    $"- {ri.Interaction.Timestamp:MM/dd} {ri.Interaction.Type} (Score: {ri.SimilarityScore:F2})"))}

ACTIVE RESOURCES: {context.ActiveContactAssignments.Count} assignments
TOTAL INTERACTIONS: {context.Metadata.TotalInteractionCount}";

        return summary;
    }

    private class StateTransitionResult
    {
        public bool ShouldTransition { get; set; }
        public string? NewState { get; set; }
        public string? Reason { get; set; }
    }
}
