using Emma.Data;
using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Emma.Api.Services;

/// <summary>
/// Basic vector search service implementation using cosine similarity
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    private readonly AppDbContext _context;
    private readonly IAzureOpenAIService _azureOpenAIService;
    private readonly ILogger<VectorSearchService> _logger;

    public VectorSearchService(
        AppDbContext context,
        IAzureOpenAIService azureOpenAIService,
        ILogger<VectorSearchService> logger)
    {
        _context = context;
        _azureOpenAIService = azureOpenAIService;
        _logger = logger;
    }

    /// <summary>
    /// Performs semantic search to find relevant interactions based on query embedding
    /// </summary>
    public async Task<List<RelevantInteraction>> FindSimilarInteractionsAsync(
        float[] queryEmbedding,
        Guid contactId,
        Guid organizationId,
        int topK = 5,
        double minSimilarity = 0.7)
    {
        try
        {
            _logger.LogDebug("Finding similar interactions for contact {ContactId} with topK={TopK}, minSimilarity={MinSimilarity}", 
                contactId, topK, minSimilarity);

            // Get all embeddings for the contact
            var embeddings = await _context.InteractionEmbeddings
                .Include(ie => ie.Interaction)
                .Include(ie => ie.Contact)
                .Where(ie => ie.ContactId == contactId && ie.OrganizationId == organizationId)
                .OrderByDescending(ie => ie.Timestamp)
                .Take(100) // Limit to recent 100 for performance
                .ToListAsync();

            if (!embeddings.Any())
            {
                _logger.LogInformation("No embeddings found for contact {ContactId}", contactId);
                return new List<RelevantInteraction>();
            }

            // Calculate similarities and sort
            var similarities = embeddings
                .Select(embedding => new
                {
                    Embedding = embedding,
                    Similarity = CalculateCosineSimilarity(queryEmbedding, embedding.Embedding)
                })
                .Where(x => x.Similarity >= minSimilarity)
                .OrderByDescending(x => x.Similarity)
                .Take(topK)
                .ToList();

            var results = similarities.Select(s => new RelevantInteraction
            {
                Interaction = s.Embedding.Interaction,
                Embedding = s.Embedding,
                SimilarityScore = s.Similarity,
                RelevanceReason = GetRelevanceReason(s.Similarity, s.Embedding)
            }).ToList();

            _logger.LogInformation("Found {Count} relevant interactions for contact {ContactId}", 
                results.Count, contactId);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar interactions for contact {ContactId}", contactId);
            return new List<RelevantInteraction>();
        }
    }

    /// <summary>
    /// Performs semantic search using text query (generates embedding internally)
    /// </summary>
    public async Task<List<RelevantInteraction>> SearchInteractionsAsync(
        string query,
        Guid contactId,
        Guid organizationId,
        int topK = 5,
        double minSimilarity = 0.7)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Empty query provided for interaction search");
                return new List<RelevantInteraction>();
            }

            // Generate embedding for the query
            var queryEmbedding = await _azureOpenAIService.GenerateEmbeddingAsync(query);

            // Perform vector search
            return await FindSimilarInteractionsAsync(
                queryEmbedding, contactId, organizationId, topK, minSimilarity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching interactions with query: {Query}", query);
            return new List<RelevantInteraction>();
        }
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors
    /// </summary>
    public double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1 == null || vector2 == null)
        {
            return 0.0;
        }

        if (vector1.Length != vector2.Length)
        {
            _logger.LogWarning("Vector dimension mismatch: {Dim1} vs {Dim2}", 
                vector1.Length, vector2.Length);
            return 0.0;
        }

        if (vector1.Length == 0)
        {
            return 0.0;
        }

        try
        {
            // Calculate dot product
            double dotProduct = 0.0;
            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
            }

            // Calculate magnitudes
            double magnitude1 = Math.Sqrt(vector1.Sum(x => x * x));
            double magnitude2 = Math.Sqrt(vector2.Sum(x => x * x));

            // Avoid division by zero
            if (magnitude1 == 0.0 || magnitude2 == 0.0)
            {
                return 0.0;
            }

            // Calculate cosine similarity
            double similarity = dotProduct / (magnitude1 * magnitude2);

            // Clamp to valid range [-1, 1] and convert to [0, 1]
            similarity = Math.Max(-1.0, Math.Min(1.0, similarity));
            return (similarity + 1.0) / 2.0; // Convert from [-1,1] to [0,1]
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cosine similarity");
            return 0.0;
        }
    }

    /// <summary>
    /// Indexes an interaction embedding for future search
    /// </summary>
    public async Task<bool> IndexInteractionAsync(InteractionEmbedding embedding)
    {
        try
        {
            // Check if embedding already exists
            var existing = await _context.InteractionEmbeddings
                .FirstOrDefaultAsync(ie => ie.InteractionId == embedding.InteractionId);

            if (existing != null)
            {
                // Update existing embedding
                existing.Embedding = embedding.Embedding;
                existing.Summary = embedding.Summary;
                existing.ExtractedEntities = embedding.ExtractedEntities;
                existing.Topics = embedding.Topics;
                existing.SentimentScore = embedding.SentimentScore;
                existing.CustomFields = embedding.CustomFields;
                
                _context.InteractionEmbeddings.Update(existing);
            }
            else
            {
                // Add new embedding
                _context.InteractionEmbeddings.Add(embedding);
            }

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Successfully indexed interaction embedding {InteractionId}", 
                embedding.InteractionId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing interaction embedding {InteractionId}", 
                embedding.InteractionId);
            return false;
        }
    }

    /// <summary>
    /// Removes an interaction from the search index
    /// </summary>
    public async Task<bool> RemoveInteractionAsync(Guid interactionId)
    {
        try
        {
            var embedding = await _context.InteractionEmbeddings
                .FirstOrDefaultAsync(ie => ie.InteractionId == interactionId);

            if (embedding != null)
            {
                _context.InteractionEmbeddings.Remove(embedding);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug("Successfully removed interaction embedding {InteractionId}", 
                    interactionId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing interaction embedding {InteractionId}", 
                interactionId);
            return false;
        }
    }

    /// <summary>
    /// Generates a relevance reason based on similarity score and embedding metadata
    /// </summary>
    private string GetRelevanceReason(double similarity, InteractionEmbedding embedding)
    {
        var reasons = new List<string>();

        // Similarity-based reasons
        if (similarity >= 0.9)
        {
            reasons.Add("Very high semantic similarity");
        }
        else if (similarity >= 0.8)
        {
            reasons.Add("High semantic similarity");
        }
        else if (similarity >= 0.7)
        {
            reasons.Add("Moderate semantic similarity");
        }

        // Topic-based reasons
        if (embedding.Topics.Any())
        {
            reasons.Add($"Related topics: {string.Join(", ", embedding.Topics.Take(2))}");
        }

        // Sentiment-based reasons
        if (Math.Abs(embedding.SentimentScore) > 0.5)
        {
            var sentiment = embedding.SentimentScore > 0 ? "positive" : "negative";
            reasons.Add($"Strong {sentiment} sentiment");
        }

        // Recency-based reasons
        var daysSince = (DateTime.UtcNow - embedding.Timestamp).TotalDays;
        if (daysSince <= 1)
        {
            reasons.Add("Recent interaction");
        }
        else if (daysSince <= 7)
        {
            reasons.Add("From this week");
        }

        return reasons.Any() ? string.Join("; ", reasons) : "Semantic match";
    }
}
