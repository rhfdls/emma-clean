using Emma.Core.Models;
using Emma.Models.Interfaces;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Emma.Core.Enums; // For ContactType
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services;

/// <summary>
/// Implementation of contact access control service.
/// Enforces organization boundaries and collaboration permissions.
/// </summary>
/// <summary>
/// Implementation of contact access control service with proper User/Agent separation.
/// Enforces organization boundaries and collaboration permissions.
/// </summary>
public class ContactAccessService : IContactAccessService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<ContactAccessService> _logger;
    private readonly IUserRepository _userRepository;

    public ContactAccessService(
        IAppDbContext context, 
        ILogger<ContactAccessService> logger,
        IUserRepository userRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contact>> GetAuthorizedContactsAsync(Guid requestingAgentId)
    {
        // First check if this is a user or an agent
        var isUser = await _context.Users.AnyAsync(u => u.Id == requestingAgentId);
        var isAgent = await _context.Agents.AnyAsync(a => a.Id == requestingAgentId);

        if (!isUser && !isAgent)
        {
            _logger.LogWarning("User/Agent {Id} not found", requestingAgentId);
            return Enumerable.Empty<Contact>();
        }

        // Get the organization ID
        Guid? organizationId = isUser 
            ? (await _context.Users.FindAsync(requestingAgentId))?.OrganizationId
            : (await _context.Agents.FindAsync(requestingAgentId))?.OrganizationId;

        if (!organizationId.HasValue)
        {
            _logger.LogWarning("No organization found for {EntityType} {Id}", 
                isUser ? "User" : "Agent", requestingAgentId);
            return Enumerable.Empty<Contact>();
        }

        // Get contacts the user/agent owns
        var ownedContacts = _context.Contacts
            .Where(c => c.OwnerId == requestingAgentId);

        // Get contacts the user/agent has collaboration access to
        var collaboratedContacts = _context.Contacts
            .Where(c => c.Collaborators.Any(collab => 
                collab.CollaboratorId == requestingAgentId && 
                collab.IsActive && 
                (!collab.ExpiresAt.HasValue || collab.ExpiresAt.Value > DateTime.UtcNow)));

        // If user is an admin, include all organization contacts (respecting business/personal boundaries)
        IQueryable<Contact> adminContacts = Enumerable.Empty<Contact>().AsQueryable();
        if (isUser)
        {
            var user = await _context.Users.FindAsync(requestingAgentId);
            if (user?.IsAdmin == true)
            {
                adminContacts = _context.Contacts
                    .Where(c => c.OrganizationId == organizationId.Value);
            }
        }

        // Combine and return unique contacts
        var authorizedContacts = await ownedContacts
            .Union(collaboratedContacts)
            .Union(adminContacts)
            .Distinct()
            .Include(c => c.Collaborators)
            .ToListAsync();

        _logger.LogInformation("Agent {AgentId} has access to {ContactCount} contacts", requestingAgentId.ToString(), authorizedContacts.Count().ToString());

        return authorizedContacts;
    }

    /// <inheritdoc/>
    public async Task<bool> CanAccessContactAsync(Guid contactId, Guid requestingAgentId)
    {
        // First check if the contact exists
        var contact = await _context.Contacts
            .Include(c => c.Collaborators)
            .FirstOrDefaultAsync(c => c.Id == contactId);

        if (contact == null)
        {
            _logger.LogWarning("Contact {ContactId} not found", contactId);
            return false;
        }


        // Check if the requesting entity is the owner
        if (contact.OwnerId == requestingAgentId)
        {
            await LogContactAccessAsync(contactId, requestingAgentId, true, "Is contact owner");
            return true;
        }

        // Check if the requesting entity is a collaborator
        var isCollaborator = contact.Collaborators.Any(c => 
            c.CollaboratorId == requestingAgentId && 
            c.IsActive && 
            (!c.ExpiresAt.HasValue || c.ExpiresAt.Value > DateTime.UtcNow));

        if (isCollaborator)
        {
            await LogContactAccessAsync(contactId, requestingAgentId, true, "Has active collaboration permission");
            return true;
        }

        // Check if the requesting user is an admin in the same organization
        var isUser = await _context.Users.AnyAsync(u => u.Id == requestingAgentId);
        if (isUser)
        {
            var user = await _context.Users.FindAsync(requestingAgentId);
            if (user?.IsAdmin == true && user.OrganizationId == contact.OrganizationId)
            {
                await LogContactAccessAsync(contactId, requestingAgentId, true, "Is organization admin");
                return true;
            }
        }

        // Check if the requesting agent belongs to the same organization
        var isAgent = await _context.Agents.AnyAsync(a => a.Id == requestingAgentId);
        if (isAgent)
        {
            var agent = await _context.Agents.FindAsync(requestingAgentId);
            if (agent?.OrganizationId == contact.OrganizationId)
            {
                // Agents can only access contacts they own or have explicit access to
                // which we've already checked above
                _logger.LogInformation("Agent {AgentId} does not have access to contact {ContactId}", 
                    requestingAgentId, contactId);
            }
        }

        await LogContactAccessAsync(contactId, requestingAgentId, false, "No valid access permissions found");
        return false;
    }

    /// <inheritdoc/>
    public async Task<ContactCollaborator?> GetCollaborationPermissionsAsync(Guid contactId, Guid requestingAgentId)
    {
        // First verify the contact exists
        var contact = await _context.Contacts
            .Include(c => c.Collaborators)
            .FirstOrDefaultAsync(c => c.Id == contactId);

        if (contact == null)
        {
            _logger.LogWarning("Contact {ContactId} not found", contactId);
            return null;
        }

        // Return the collaboration record if it exists
        return contact.Collaborators
            .FirstOrDefault(c => c.CollaboratorId == requestingAgentId);
    }

    /// <inheritdoc/>
    public async Task<bool> IsContactOwnerAsync(Guid contactId, Guid requestingAgentId)
    {
        var contact = await _context.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contactId);

        if (contact == null)
        {
            _logger.LogWarning("Contact {ContactId} not found", contactId);
            return false;
        }


        bool isOwner = contact.OwnerId == requestingAgentId;
        _logger.LogDebug("User/Agent {AgentId} {IsOwner} the owner of contact {ContactId}",
            requestingAgentId, isOwner ? "is" : "is not", contactId);
            
        return isOwner;
    }

    /// <inheritdoc/>
    public async Task<bool> IsOrganizationOwnerAsync(Guid requestingAgentId)
    {
        // First check if this is a user (only users can be organization owners)
        var user = await _context.Users.FindAsync(requestingAgentId);
        if (user == null)
        {
            _logger.LogDebug("Entity {Id} is not a user, cannot be an organization owner", requestingAgentId);
            return false;
        }

        bool isOwner = user.IsAdmin;
        _logger.LogDebug("User {UserId} {IsOwner} an organization owner", 
            requestingAgentId, isOwner ? "is" : "is not");
            
        return isOwner;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contact>> GetOrganizationContactsAsync(Guid requestingAgentId, bool businessOnly = true)
    {
        // First determine if this is a user or agent and get their organization
        var user = await _context.Users.FindAsync(requestingAgentId);
        var agent = user == null ? await _context.Agents.FindAsync(requestingAgentId) : null;

        if (user == null && agent == null)
        {
            _logger.LogWarning("User/Agent {Id} not found", requestingAgentId);
            return Enumerable.Empty<Contact>();
        }

        var organizationId = user?.OrganizationId ?? agent?.OrganizationId;
        if (!organizationId.HasValue)
        {
            _logger.LogWarning("No organization found for {EntityType} {Id}", 
                user != null ? "User" : "Agent", requestingAgentId);
            return Enumerable.Empty<Contact>();
        }

        // Build the base query
        var query = _context.Contacts
            .Where(c => c.OrganizationId == organizationId.Value);

        // Apply business/personal filtering if requested
        if (businessOnly)
        {
            query = query.Where(c => c.ContactType == ContactType.Business);
        }

        // If this is a non-admin user, filter to only contacts they own or have access to
        if (user != null && !user.IsAdmin)
        {
            // Get IDs of contacts the user owns or has collaboration access to
            var accessibleContactIds = await _context.Contacts
                .Where(c => c.OrganizationId == organizationId.Value && 
                           (c.OwnerId == requestingAgentId || 
                            c.Collaborators.Any(collab => collab.CollaboratorId == requestingAgentId && 
                                                        collab.IsActive && 
                                                        (!collab.ExpiresAt.HasValue || collab.ExpiresAt.Value > DateTime.UtcNow))))
                .Select(c => c.Id)
                .ToListAsync();

            query = query.Where(c => accessibleContactIds.Contains(c.Id));
        }
        // If this is an agent, only return contacts they own or have access to
        else if (agent != null)
        {
            var accessibleContactIds = await _context.Contacts
                .Where(c => c.OrganizationId == organizationId.Value && 
                           (c.OwnerId == requestingAgentId || 
                            c.Collaborators.Any(collab => collab.CollaboratorId == requestingAgentId && 
                                                        collab.IsActive && 
                                                        (!collab.ExpiresAt.HasValue || collab.ExpiresAt.Value > DateTime.UtcNow))))
                .Select(c => c.Id)
                .ToListAsync();

            query = query.Where(c => accessibleContactIds.Contains(c.Id));
        }

        return await query
            .Include(c => c.Collaborators)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task LogContactAccessAsync(Guid contactId, Guid requestingAgentId, bool accessGranted, string reason)
    {
        try
        {
            var auditLog = new ContactAccessAudit
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                AccessingEntityId = requestingAgentId,
                AccessTimestamp = DateTime.UtcNow,
                AccessGranted = accessGranted,
                Reason = reason,
                IpAddress = null, // Would be populated from HTTP context in a real implementation
                UserAgent = null  // Would be populated from HTTP context in a real implementation
            };

            _context.ContactAccessAudits.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Access {AccessStatus} for {EntityType} {EntityId} to contact {ContactId}: {Reason}",
                accessGranted ? "granted" : "denied",
                await _context.Users.AnyAsync(u => u.Id == requestingAgentId) ? "User" : "Agent",
                requestingAgentId,
                contactId,
                reason);
        }
        catch (Exception ex)
        {
            // Don't let audit logging failures affect the main flow
            _logger.LogError(ex, "Error logging contact access for {EntityId} to contact {ContactId}", 
                requestingAgentId, contactId);
        }
    }

    public async Task<bool> CanAccessContactAsync(Guid contactId, Guid requestingAgentId)
    {
        // Check if agent owns the contact
        var isOwner = await IsContactOwnerAsync(contactId, requestingAgentId);
        if (isOwner) return true;

        // Check if agent has active collaboration access
        var collaboration = await GetCollaborationPermissionsAsync(contactId, requestingAgentId);
        if (collaboration != null && collaboration.IsActive && 
            (!collaboration.ExpiresAt.HasValue || collaboration.ExpiresAt.Value > DateTime.UtcNow))
        {
            return true;
        }

        // Check if agent is organization owner (for business contacts only)
        var isOrgOwner = await IsOrganizationOwnerAsync(requestingAgentId);
        if (isOrgOwner)
        {
            // Organization owners can access business contacts in their org
            var contact = await _context.Contacts
                .Include(c => c.OwnerAgent)
                .FirstOrDefaultAsync(c => c.Id == contactId);

            if (contact?.OwnerAgent?.OrganizationId != null)
            {
                var requestingAgent = await _context.Agents
                    .FirstOrDefaultAsync(a => a.Id == requestingAgentId);
                
                return contact.OwnerAgent.OrganizationId == requestingAgent?.OrganizationId;
            }
        }

        return false;
    }

    public async Task<ContactCollaborator?> GetCollaborationPermissionsAsync(Guid contactId, Guid requestingAgentId)
    {
        return await _context.ContactCollaborators
            .FirstOrDefaultAsync(cc => 
                cc.ContactId == contactId && 
                cc.CollaboratorAgentId == requestingAgentId);
    }

    public async Task<bool> IsContactOwnerAsync(Guid contactId, Guid requestingAgentId)
    {
        return await _context.Contacts
            .AnyAsync(c => c.Id == contactId && c.OwnerAgent.Id == requestingAgentId);
    }

    public async Task<bool> IsOrganizationOwnerAsync(Guid requestingAgentId)
    {
        return await _context.Organizations
            .AnyAsync(o => o.OwnerAgentId == requestingAgentId);
    }

    public async Task<IEnumerable<Contact>> GetOrganizationContactsAsync(Guid requestingAgentId, bool businessOnly = true)
    {
        var requestingAgent = await _context.Agents
            .FirstOrDefaultAsync(a => a.Id == requestingAgentId);

        if (requestingAgent?.OrganizationId == null)
        {
            return Enumerable.Empty<Contact>();
        }

        var query = _context.Contacts
            .Include(c => c.OwnerAgent)
            .Where(c => c.OwnerAgent.OrganizationId == requestingAgent.OrganizationId);

        // If business only, we would filter out personal contacts
        // This would require additional logic based on contact relationship state
        if (businessOnly)
        {
            query = query.Where(c => c.RelationshipState != RelationshipState.Friend && 
                                   c.RelationshipState != RelationshipState.Family);
        }

        return await query.ToListAsync();
    }

    public async Task LogContactAccessAsync(Guid contactId, Guid requestingAgentId, bool accessGranted, string reason)
    {
        await LogContactAccessInternalAsync(contactId, requestingAgentId, accessGranted, reason);
    }

    private async Task LogContactAccessInternalAsync(Guid contactId, Guid requestingAgentId, bool accessGranted, string reason, string[]? privacyTags = null)
    {
        try
        {
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Id == requestingAgentId);
            var organizationId = agent?.OrganizationId ?? Guid.Empty;

            var auditLog = new AccessAuditLog
            {
                RequestingAgentId = requestingAgentId,
                OrganizationId = organizationId,
                ResourceType = "Contact",
                ResourceId = contactId,
                ContactId = contactId,
                AccessGranted = accessGranted,
                Reason = reason,
                PrivacyTags = System.Text.Json.JsonSerializer.Serialize(privacyTags ?? Array.Empty<string>()),
                AccessedAt = DateTime.UtcNow
            };

            _context.AccessAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contact access logged: Agent {AgentId} {AccessResult} contact {ContactId} - {Reason} (Tags: {Tags})",
                requestingAgentId, accessGranted ? "accessed" : "denied access to", contactId, reason, 
                privacyTags != null ? string.Join(",", privacyTags) : "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log contact access for agent {AgentId} and contact {ContactId}", 
                requestingAgentId, contactId);
        }
    }
}
