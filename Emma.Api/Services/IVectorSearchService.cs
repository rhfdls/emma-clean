using Emma.Data.Models;

namespace Emma.Api.Services;

/// <summary>
/// Interface for vector search service supporting NBA context management
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Performs semantic search to find relevant interactions based on query embedding
    /// </summary>
    /// <param name="queryEmbedding">Query vector embedding</param>
    /// <param name="clientId">Client ID to filter results</param>
    /// <param name="organizationId">Organization ID to filter results</param>
    /// <param name="topK">Number of top results to return</param>
    /// <param name="minSimilarity">Minimum similarity threshold (0.0 to 1.0)</param>
    /// <returns>List of relevant interactions with similarity scores</returns>
    Task<List<RelevantInteraction>> FindSimilarInteractionsAsync(
        float[] queryEmbedding,
        Guid clientId,
        Guid organizationId,
        int topK = 5,
        double minSimilarity = 0.7);

    /// <summary>
    /// Performs semantic search using text query (generates embedding internally)
    /// </summary>
    /// <param name="query">Text query</param>
    /// <param name="clientId">Client ID to filter results</param>
    /// <param name="organizationId">Organization ID to filter results</param>
    /// <param name="topK">Number of top results to return</param>
    /// <param name="minSimilarity">Minimum similarity threshold (0.0 to 1.0)</param>
    /// <returns>List of relevant interactions with similarity scores</returns>
    Task<List<RelevantInteraction>> SearchInteractionsAsync(
        string query,
        Guid clientId,
        Guid organizationId,
        int topK = 5,
        double minSimilarity = 0.7);

    /// <summary>
    /// Calculates cosine similarity between two vectors
    /// </summary>
    /// <param name="vector1">First vector</param>
    /// <param name="vector2">Second vector</param>
    /// <returns>Cosine similarity score (0.0 to 1.0)</returns>
    double CalculateCosineSimilarity(float[] vector1, float[] vector2);

    /// <summary>
    /// Indexes an interaction embedding for future search
    /// </summary>
    /// <param name="embedding">Interaction embedding to index</param>
    /// <returns>Success status</returns>
    Task<bool> IndexInteractionAsync(InteractionEmbedding embedding);

    /// <summary>
    /// Removes an interaction from the search index
    /// </summary>
    /// <param name="interactionId">Interaction ID to remove</param>
    /// <returns>Success status</returns>
    Task<bool> RemoveInteractionAsync(Guid interactionId);
}
