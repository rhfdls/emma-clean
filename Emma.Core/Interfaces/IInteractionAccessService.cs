using Emma.Data.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Service for enforcing privacy-aware access to interactions.
/// Ensures agents can only access interactions they're authorized to see.
/// </summary>
public interface IInteractionAccessService
{
    /// <summary>
    /// Gets interactions for a contact that the requesting agent is authorized to access.
    /// Filters out PERSONAL tagged interactions unless explicitly granted access.
    /// </summary>
    Task<IEnumerable<Interaction>> GetAuthorizedInteractionsAsync(Guid contactId, Guid requestingAgentId);
    
    /// <summary>
    /// Checks if an agent can access a specific interaction based on privacy tags and collaboration rules.
    /// </summary>
    Task<bool> CanAccessInteractionAsync(Guid interactionId, Guid requestingAgentId);
    
    /// <summary>
    /// Gets only business interactions (CRM tagged or untagged) for a contact.
    /// Excludes all PERSONAL/PRIVATE tagged interactions regardless of permissions.
    /// </summary>
    Task<IEnumerable<Interaction>> GetBusinessInteractionsAsync(Guid contactId, Guid requestingAgentId);
    
    /// <summary>
    /// Audits interaction access attempts for compliance and security monitoring.
    /// </summary>
    Task LogInteractionAccessAsync(Guid interactionId, Guid requestingAgentId, bool accessGranted, string reason);
    
    /// <summary>
    /// Filters a collection of interactions based on agent's privacy permissions.
    /// </summary>
    Task<IEnumerable<Interaction>> FilterByPrivacyPermissionsAsync(IEnumerable<Interaction> interactions, Guid requestingAgentId);
}
