using System.ComponentModel.DataAnnotations;

namespace Emma.Data.Models;

/// <summary>
/// Rolling summary of client interactions and context
/// </summary>
public class ClientSummary
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    public Guid ClientId { get; set; }
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Type of summary (e.g., "rolling", "milestone", "periodic")
    /// </summary>
    public string SummaryType { get; set; } = "rolling";
    
    /// <summary>
    /// AI-generated summary text
    /// </summary>
    public string SummaryText { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of interactions included in this summary
    /// </summary>
    public int InteractionCount { get; set; }
    
    /// <summary>
    /// Timestamp of earliest interaction in summary
    /// </summary>
    public DateTime EarliestInteraction { get; set; }
    
    /// <summary>
    /// Timestamp of latest interaction in summary
    /// </summary>
    public DateTime LatestInteraction { get; set; }
    
    /// <summary>
    /// Key milestones or events
    /// </summary>
    public List<string> KeyMilestones { get; set; } = new();
    
    /// <summary>
    /// Important client preferences extracted from interactions
    /// </summary>
    public Dictionary<string, object> ImportantPreferences { get; set; } = new();
    
    /// <summary>
    /// Custom fields for additional context
    /// </summary>
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Contact Contact { get; set; } = null!;
}
