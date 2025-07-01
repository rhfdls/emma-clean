using Emma.Models.Models;
using Emma.Core.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Service for retrieving and managing contact context
/// </summary>
public interface IContextRetriever
{
    /// <summary>
    /// Get comprehensive context for a contact
    /// </summary>
    /// <param name="contactId">Contact identifier</param>
    /// <param name="includeInteractions">Include interaction history</param>
    /// <param name="includeAssignments">Include active assignments</param>
    /// <returns>Complete contact context</returns>
    Task<ContactContext?> GetContactContextAsync(
        Guid contactId, 
        bool includeInteractions = true, 
        bool includeAssignments = true);
    
    /// <summary>
    /// Get quick contact snapshot for performance-critical scenarios
    /// </summary>
    /// <param name="contactId">Contact identifier</param>
    /// <returns>Basic contact information</returns>
    Task<ContactSnapshot?> GetContactSnapshotAsync(Guid contactId);
    
    /// <summary>
    /// Update contact context with new interaction data
    /// </summary>
    /// <param name="contactId">Contact identifier</param>
    /// <param name="interaction">New interaction to add</param>
    /// <returns>Updated contact context</returns>
    Task<ContactContext?> UpdateContactContextAsync(Guid contactId, Interaction interaction);
}

/// <summary>
/// Quick snapshot of contact information
/// </summary>
public class ContactSnapshot
{
    public Guid ContactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastInteraction { get; set; }
    public int InteractionCount { get; set; }
}
