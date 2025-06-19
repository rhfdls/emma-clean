using Emma.Models.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Service for enforcing contact access control based on organization membership and collaboration rules.
/// Ensures agents can only access contacts they own or have been granted collaboration access to.
/// </summary>
public interface IContactAccessService
{
    /// <summary>
    /// Gets all contacts that the requesting agent is authorized to access.
    /// Includes owned contacts and those with active collaboration permissions.
    /// </summary>
    Task<IEnumerable<Contact>> GetAuthorizedContactsAsync(Guid requestingAgentId);
    
    /// <summary>
    /// Checks if an agent can access a specific contact.
    /// </summary>
    Task<bool> CanAccessContactAsync(Guid contactId, Guid requestingAgentId);
    
    /// <summary>
    /// Gets the collaboration permissions for an agent on a specific contact.
    /// Returns null if no collaboration exists.
    /// </summary>
    Task<ContactCollaborator?> GetCollaborationPermissionsAsync(Guid contactId, Guid requestingAgentId);
    
    /// <summary>
    /// Checks if an agent is the primary owner of a contact.
    /// </summary>
    Task<bool> IsContactOwnerAsync(Guid contactId, Guid requestingAgentId);
    
    /// <summary>
    /// Checks if an agent is an organization owner and can access all business contacts.
    /// IMPORTANT: Even organization owners cannot access PERSONAL tagged interactions.
    /// </summary>
    Task<bool> IsOrganizationOwnerAsync(Guid requestingAgentId);
    
    /// <summary>
    /// Gets contacts within the same organization as the requesting agent.
    /// Filters based on collaboration rules and business/personal boundaries.
    /// </summary>
    Task<IEnumerable<Contact>> GetOrganizationContactsAsync(Guid requestingAgentId, bool businessOnly = true);
    
    /// <summary>
    /// Audits contact access attempts for compliance monitoring.
    /// </summary>
    Task LogContactAccessAsync(Guid contactId, Guid requestingAgentId, bool accessGranted, string reason);
}
