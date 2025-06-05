using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Data.Models;

/// <summary>
/// Vector embedding and metadata for an interaction
/// </summary>
public class InteractionEmbedding
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    public Guid ContactId { get; set; }
    public Guid InteractionId { get; set; }
    public Guid OrganizationId { get; set; }
    
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Brief summary of the interaction content
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// Original interaction content (may be truncated)
    /// </summary>
    public string? RawContent { get; set; }
    
    /// <summary>
    /// Vector embedding as float array
    /// </summary>
    [NotMapped]
    public float[] Embedding { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Model used to generate embedding
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
    
    /// <summary>
    /// Model version for tracking changes
    /// </summary>
    public string ModelVersion { get; set; } = "1";
    
    /// <summary>
    /// Privacy tags inherited from interaction
    /// </summary>
    public List<string> PrivacyTags { get; set; } = new();
    
    /// <summary>
    /// Extracted entities from the interaction
    /// </summary>
    public Dictionary<string, object> ExtractedEntities { get; set; } = new();
    
    /// <summary>
    /// Key topics identified in the interaction
    /// </summary>
    public List<string> Topics { get; set; } = new();
    
    /// <summary>
    /// Sentiment score (-1.0 to 1.0)
    /// </summary>
    public double SentimentScore { get; set; }
    
    /// <summary>
    /// Custom fields for additional metadata
    /// </summary>
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Contact Contact { get; set; } = null!;
    public Interaction Interaction { get; set; } = null!;
}
