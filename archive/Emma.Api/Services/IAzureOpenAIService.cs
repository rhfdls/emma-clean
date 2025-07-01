using Emma.Models.Models;

namespace Emma.Api.Services;

/// <summary>
/// Interface for Azure OpenAI services supporting NBA context management
/// </summary>
public interface IAzureOpenAIService
{
    /// <summary>
    /// Generates vector embeddings for interaction content
    /// </summary>
    /// <param name="content">Text content to embed</param>
    /// <param name="model">Embedding model to use (default: text-embedding-ada-002)</param>
    /// <returns>Vector embedding as float array</returns>
    Task<float[]> GenerateEmbeddingAsync(string content, string model = "text-embedding-ada-002");

    /// <summary>
    /// Generates or updates a rolling summary for a client based on their interaction history
    /// </summary>
    /// <param name="existingSummary">Current summary (null for new clients)</param>
    /// <param name="newInteraction">Latest interaction to incorporate</param>
    /// <param name="recentInteractions">Recent interactions for context</param>
    /// <returns>Updated summary text</returns>
    Task<string> UpdateRollingSummaryAsync(
        string? existingSummary, 
        Interaction newInteraction, 
        List<Interaction> recentInteractions);

    /// <summary>
    /// Extracts key entities and topics from interaction content
    /// </summary>
    /// <param name="content">Interaction content to analyze</param>
    /// <returns>Dictionary of extracted entities and topics</returns>
    Task<Dictionary<string, object>> ExtractEntitiesAsync(string content);

    /// <summary>
    /// Analyzes sentiment of interaction content
    /// </summary>
    /// <param name="content">Content to analyze</param>
    /// <returns>Sentiment score between -1.0 (negative) and 1.0 (positive)</returns>
    Task<double> AnalyzeSentimentAsync(string content);

    /// <summary>
    /// Generates Next Best Action recommendations based on context
    /// </summary>
    /// <param name="context">Curated NBA context</param>
    /// <param name="scenario">Current scenario or goal</param>
    /// <returns>NBA recommendations</returns>
    Task<string> GenerateNbaRecommendationsAsync(NbaContext context, string? scenario = null);

    /// <summary>
    /// Determines if a client state transition should occur based on interaction content
    /// </summary>
    /// <param name="currentState">Current client state</param>
    /// <param name="interaction">New interaction</param>
    /// <returns>Suggested new state and transition reason, or null if no transition</returns>
    Task<(string? newState, string? reason)> SuggestStateTransitionAsync(
        ContactState currentState, 
        Interaction interaction);
}
