using Emma.Core.Interfaces;
using Emma.Data;
using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services;

/// <summary>
/// Implementation of interaction access control service.
/// Enforces privacy tag filtering and collaboration permissions.
/// </summary>
public class InteractionAccessService : IInteractionAccessService
{
    private readonly AppDbContext _context;
    private readonly IContactAccessService _contactAccessService;
    private readonly ILogger<InteractionAccessService> _logger;

    public InteractionAccessService(
        AppDbContext context, 
        IContactAccessService contactAccessService,
        ILogger<InteractionAccessService> logger)
    {
        _context = context;
        _contactAccessService = contactAccessService;
        _logger = logger;
    }

    public async Task<IEnumerable<Interaction>> GetAuthorizedInteractionsAsync(Guid contactId, Guid requestingAgentId)
    {
        // First check if agent can access the contact at all
        var canAccessContact = await _contactAccessService.CanAccessContactAsync(contactId, requestingAgentId);
        if (!canAccessContact)
        {
            await LogInteractionAccessAsync(Guid.Empty, requestingAgentId, false, "Contact access denied");
            return Enumerable.Empty<Interaction>();
        }

        // Get collaboration permissions to determine what interactions are allowed
        var collaboration = await _contactAccessService.GetCollaborationPermissionsAsync(contactId, requestingAgentId);
        var isOwner = await _contactAccessService.IsContactOwnerAsync(contactId, requestingAgentId);

        var interactions = await _context.Interactions
            .Where(i => i.ContactId == contactId)
            .ToListAsync();

        var authorizedInteractions = new List<Interaction>();

        foreach (var interaction in interactions)
        {
            var canAccess = await CanAccessInteractionInternalAsync(interaction, requestingAgentId, collaboration, isOwner);
            if (canAccess)
            {
                authorizedInteractions.Add(interaction);
                await LogInteractionAccessAsync(interaction.Id, requestingAgentId, true, "Access granted");
            }
            else
            {
                await LogInteractionAccessAsync(interaction.Id, requestingAgentId, false, "Privacy tag restriction");
            }
        }

        _logger.LogInformation("Agent {AgentId} accessed {AuthorizedCount}/{TotalCount} interactions for contact {ContactId}",
            requestingAgentId, authorizedInteractions.Count, interactions.Count, contactId);

        return authorizedInteractions;
    }

    public async Task<bool> CanAccessInteractionAsync(Guid interactionId, Guid requestingAgentId)
    {
        var interaction = await _context.Interactions
            .FirstOrDefaultAsync(i => i.Id == interactionId);

        if (interaction == null)
        {
            await LogInteractionAccessAsync(interactionId, requestingAgentId, false, "Interaction not found");
            return false;
        }

        // Check contact access first
        var canAccessContact = await _contactAccessService.CanAccessContactAsync(interaction.ContactId, requestingAgentId);
        if (!canAccessContact)
        {
            await LogInteractionAccessAsync(interactionId, requestingAgentId, false, "Contact access denied");
            return false;
        }

        // Get permissions
        var collaboration = await _contactAccessService.GetCollaborationPermissionsAsync(interaction.ContactId, requestingAgentId);
        var isOwner = await _contactAccessService.IsContactOwnerAsync(interaction.ContactId, requestingAgentId);

        var canAccess = await CanAccessInteractionInternalAsync(interaction, requestingAgentId, collaboration, isOwner);
        
        var reason = canAccess ? "Access granted" : "Privacy tag restriction";
        await LogInteractionAccessAsync(interactionId, requestingAgentId, canAccess, reason);

        return canAccess;
    }

    public async Task<IEnumerable<Interaction>> GetBusinessInteractionsAsync(Guid contactId, Guid requestingAgentId)
    {
        // First check if agent can access the contact at all
        var canAccessContact = await _contactAccessService.CanAccessContactAsync(contactId, requestingAgentId);
        if (!canAccessContact)
        {
            await LogInteractionAccessAsync(Guid.Empty, requestingAgentId, false, "Contact access denied");
            return Enumerable.Empty<Interaction>();
        }

        // Get only business interactions (CRM tagged or untagged, but NOT personal/private)
        var businessInteractions = await _context.Interactions
            .Where(i => i.ContactId == contactId)
            .Where(i => !i.Tags.Any(tag => 
                tag.Equals("PERSONAL", StringComparison.OrdinalIgnoreCase) ||
                tag.Equals("PRIVATE", StringComparison.OrdinalIgnoreCase)))
            .ToListAsync();

        // Log access for each interaction
        foreach (var interaction in businessInteractions)
        {
            await LogInteractionAccessAsync(interaction.Id, requestingAgentId, true, "Business interaction access");
        }

        _logger.LogInformation("Agent {AgentId} accessed {Count} business interactions for contact {ContactId}",
            requestingAgentId, businessInteractions.Count, contactId);

        return businessInteractions;
    }

    public async Task<IEnumerable<Interaction>> FilterByPrivacyPermissionsAsync(IEnumerable<Interaction> interactions, Guid requestingAgentId)
    {
        var filteredInteractions = new List<Interaction>();

        foreach (var interaction in interactions)
        {
            var canAccess = await CanAccessInteractionAsync(interaction.Id, requestingAgentId);
            if (canAccess)
            {
                filteredInteractions.Add(interaction);
            }
        }

        return filteredInteractions;
    }

    public async Task LogInteractionAccessAsync(Guid interactionId, Guid requestingAgentId, bool accessGranted, string reason)
    {
        try
        {
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Id == requestingAgentId);
            var organizationId = agent?.OrganizationId ?? Guid.Empty;

            Interaction? interaction = null;
            Guid? contactId = null;
            List<string> privacyTags = new();

            if (interactionId != Guid.Empty)
            {
                interaction = await _context.Interactions
                    .FirstOrDefaultAsync(i => i.Id == interactionId);
                
                if (interaction != null)
                {
                    contactId = interaction.ContactId;
                    privacyTags = interaction.Tags.Where(tag => 
                        tag.Equals("PERSONAL", StringComparison.OrdinalIgnoreCase) ||
                        tag.Equals("PRIVATE", StringComparison.OrdinalIgnoreCase) ||
                        tag.Equals("CRM", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            var auditLog = new AccessAuditLog
            {
                RequestingAgentId = requestingAgentId,
                OrganizationId = organizationId,
                ResourceType = "Interaction",
                ResourceId = interactionId,
                ContactId = contactId,
                AccessGranted = accessGranted,
                Reason = reason,
                PrivacyTags = System.Text.Json.JsonSerializer.Serialize(privacyTags),
                AccessedAt = DateTime.UtcNow
            };

            _context.AccessAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Interaction access logged: Agent {AgentId} {AccessResult} interaction {InteractionId} - {Reason} (Tags: {Tags})",
                requestingAgentId, accessGranted ? "accessed" : "denied access to", interactionId, reason, string.Join(",", privacyTags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log interaction access for agent {AgentId} and interaction {InteractionId}", 
                requestingAgentId, interactionId);
        }
    }

    /// <summary>
    /// Internal method to check interaction access based on privacy tags and permissions.
    /// Uses the existing ContactCollaborator.CanAccessInteraction logic.
    /// </summary>
    private async Task<bool> CanAccessInteractionInternalAsync(
        Interaction interaction, 
        Guid requestingAgentId, 
        ContactCollaborator? collaboration, 
        bool isOwner)
    {
        // Contact owners have full access to all interactions
        if (isOwner)
        {
            return true;
        }

        // If no collaboration exists, deny access
        if (collaboration == null)
        {
            return false;
        }

        // Use the existing ContactCollaborator.CanAccessInteraction method
        // This enforces the privacy tag logic we already validated
        return collaboration.CanAccessInteraction(interaction);
    }
}
