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
            .Where(c => c.OwnerAgent.Id == requestingAgentId);

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

        _logger.LogInformation("Agent {AgentId} has access to {ContactCount} contacts", requestingAgentId.ToString(), authorizedContacts.Count().ToString());

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
