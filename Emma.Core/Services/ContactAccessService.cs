using Emma.Core.Interfaces;
using Emma.Data;
using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services;

/// <summary>
/// Implementation of contact access control service.
/// Enforces organization boundaries and collaboration permissions.
/// </summary>
public class ContactAccessService : IContactAccessService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ContactAccessService> _logger;

    public ContactAccessService(AppDbContext context, ILogger<ContactAccessService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Contact>> GetAuthorizedContactsAsync(Guid requestingAgentId)
    {
        var agent = await _context.Agents
            .Include(a => a.Organization)
            .FirstOrDefaultAsync(a => a.Id == requestingAgentId);

        if (agent == null)
        {
            _logger.LogWarning("Agent {AgentId} not found", requestingAgentId);
            return Enumerable.Empty<Contact>();
        }

        // Get contacts the agent owns
        var ownedContacts = _context.Contacts
            .Where(c => c.OwnerId == requestingAgentId);

        // Get contacts the agent has collaboration access to
        var collaboratedContacts = _context.Contacts
            .Where(c => c.Collaborators.Any(collab => 
                collab.CollaboratorAgentId == requestingAgentId && 
                collab.IsActive && 
                (!collab.ExpiresAt.HasValue || collab.ExpiresAt.Value > DateTime.UtcNow)));

        // Combine and return unique contacts
        var authorizedContacts = await ownedContacts
            .Union(collaboratedContacts)
            .Include(c => c.Collaborators)
            .ToListAsync();

        _logger.LogInformation("Agent {AgentId} has access to {ContactCount} contacts", 
            requestingAgentId, authorizedContacts.Count);

        return authorizedContacts;
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
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(c => c.Id == contactId);

            if (contact?.Owner?.OrganizationId != null)
            {
                var requestingAgent = await _context.Agents
                    .FirstOrDefaultAsync(a => a.Id == requestingAgentId);
                
                return contact.Owner.OrganizationId == requestingAgent?.OrganizationId;
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
            .AnyAsync(c => c.Id == contactId && c.OwnerId == requestingAgentId);
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
            .Include(c => c.Owner)
            .Where(c => c.Owner.OrganizationId == requestingAgent.OrganizationId);

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
        try
        {
            // TODO: Re-enable when AccessAuditLog entity is implemented
            // Temporarily disabled for demo - just log to console
            _logger.LogInformation("Contact access logged: Agent {AgentId}, Contact {ContactId}, Granted: {AccessGranted}, Reason: {Reason}",
                requestingAgentId, contactId, accessGranted, reason);
            
            /*
            var requestingAgent = await _context.Agents
                .Include(a => a.Organization)
                .FirstOrDefaultAsync(a => a.Id == requestingAgentId);

            if (requestingAgent?.OrganizationId == null)
            {
                _logger.LogWarning("Cannot log access for agent {AgentId} - no organization found", requestingAgentId);
                return;
            }

            var auditLog = new AccessAuditLog
            {
                RequestingAgentId = requestingAgentId,
                ResourceType = "Contact",
                ResourceId = contactId,
                ContactId = contactId,
                AccessGranted = accessGranted,
                Reason = reason,
                OrganizationId = requestingAgent.OrganizationId.Value,
                PrivacyTags = new List<string>(), // Will be populated by interaction service
                AccessedAt = DateTime.UtcNow
            };

            _context.AccessAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Logged contact access: Agent {AgentId}, Contact {ContactId}, Granted: {AccessGranted}, Reason: {Reason}",
                requestingAgentId, contactId, accessGranted, reason);
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log contact access for agent {AgentId}, contact {ContactId}", 
                requestingAgentId, contactId);
        }
    }
}
