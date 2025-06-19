namespace Emma.Models.Models;

/// <summary>
/// Represents a relevant interaction with similarity score and relevance context
/// </summary>
public class RelevantInteraction
{
    /// <summary>
    /// The interaction that was found to be relevant
    /// </summary>
    public Interaction Interaction { get; set; } = null!;
    
    /// <summary>
    /// The embedding data associated with this interaction
    /// </summary>
    public InteractionEmbedding Embedding { get; set; } = null!;
    
    /// <summary>
    /// Similarity score between 0.0 and 1.0 indicating how relevant this interaction is
    /// </summary>
    public double SimilarityScore { get; set; }
    
    /// <summary>
    /// Human-readable explanation of why this interaction is considered relevant
    /// </summary>
    public string RelevanceReason { get; set; } = string.Empty;
}
