using Emma.Data;
using Emma.Data.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Retrieves relevant context and history for ongoing interactions
/// </summary>
public interface IContextRetriever
{
    /// <summary>
    /// Get comprehensive context for a contact
    /// </summary>
    Task<ContactContext> GetContactContextAsync(Guid contactId, int maxInteractions = 10);
    
    /// <summary>
    /// Find relevant interactions based on content similarity
    /// </summary>
    Task<List<RelevantInteraction>> FindRelevantInteractionsAsync(Guid contactId, string content, int maxResults = 5);
    
    /// <summary>
    /// Get recent interactions for a contact
    /// </summary>
    Task<List<Interaction>> GetRecentInteractionsAsync(Guid contactId, int maxResults = 10);
    
    /// <summary>
    /// Get contact summary and current state
    /// </summary>
    Task<ContactSnapshot> GetContactSnapshotAsync(Guid contactId);
}

/// <summary>
/// Comprehensive context for a contact
/// </summary>
public class ContactContext
{
    public Contact Contact { get; set; } = null!;
    public ContactSummary? Summary { get; set; }
    public ContactState? State { get; set; }
    public List<Interaction> RecentInteractions { get; set; } = new();
    public List<RelevantInteraction> RelevantInteractions { get; set; } = new();
    public List<ContactAssignment> ActiveAssignments { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Quick snapshot of contact information
/// </summary>
public class ContactSnapshot
{
    public Guid ContactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RelationshipState RelationshipState { get; set; }
    public bool IsActiveClient { get; set; }
    public string? CurrentStage { get; set; }
    public string? Priority { get; set; }
    public DateTime LastInteraction { get; set; }
    public int TotalInteractions { get; set; }
}
