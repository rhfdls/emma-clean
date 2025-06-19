using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Models.Enums;
using Emma.Models.Interfaces;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Emma.Core.Services;

/// <summary>
/// Implementation of interaction access control service.
/// Enforces privacy tag filtering and collaboration permissions.
/// </summary>
/// <summary>
/// Implementation of interaction access control service with proper User/Agent separation.
/// Enforces privacy tag filtering and collaboration permissions.
/// </summary>
public class InteractionAccessService : IInteractionAccessService
{
    private readonly IAppDbContext _context;
    private readonly IContactAccessService _contactAccessService;
    private readonly ILogger<InteractionAccessService> _logger;

    public InteractionAccessService(
        IAppDbContext context, 
        IContactAccessService contactAccessService,
        ILogger<InteractionAccessService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _contactAccessService = contactAccessService ?? throw new ArgumentNullException(nameof(contactAccessService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Interaction>> GetAuthorizedInteractionsAsync(Guid contactId, Guid requestingAgentId)
    {
        // First check if the requesting entity can access the contact at all
        var canAccessContact = await _contactAccessService.CanAccessContactAsync(contactId, requestingAgentId);
        if (!canAccessContact)
        {
            await LogInteractionAccessAsync(Guid.Empty, requestingAgentId, false, "Contact access denied");
            return Enumerable.Empty<Interaction>();
        }

        // Check if this is a user or an agent
        var isUser = await _context.Users.AnyAsync(u => u.Id == requestingAgentId);
        var isAgent = await _context.Agents.AnyAsync(a => a.Id == requestingAgentId);

        if (!isUser && !isAgent)
        {
            _logger.LogWarning("User/Agent {Id} not found", requestingAgentId);
            return Enumerable.Empty<Interaction>();
        }

        // Get collaboration permissions and ownership status
        var collaboration = await _contactAccessService.GetCollaborationPermissionsAsync(contactId, requestingAgentId);
        var isOwner = await _contactAccessService.IsContactOwnerAsync(contactId, requestingAgentId);
        var isAdmin = isUser && await _contactAccessService.IsOrganizationOwnerAsync(requestingAgentId);

        // Get all interactions for the contact
        var interactions = await _context.Interactions
            .Where(i => i.ContactId == contactId)
            .OrderByDescending(i => i.Timestamp)
            .ToListAsync();

        var authorizedInteractions = new List<Interaction>();

        foreach (var interaction in interactions)
        {
            var canAccess = await CanAccessInteractionInternalAsync(
                interaction, 
                requestingAgentId, 
                isUser, 
                isAgent, 
                isOwner, 
                isAdmin, 
                collaboration);
                
            if (canAccess)
            {
                authorizedInteractions.Add(interaction);
                await LogInteractionAccessAsync(interaction.Id, requestingAgentId, true, "Access granted");
            }
            else
            {
                await LogInteractionAccessAsync(interaction.Id, requestingAgentId, false, "Access restricted by privacy settings");
            }
        }

        _logger.LogInformation("{EntityType} {EntityId} accessed {AuthorizedCount}/{TotalCount} interactions for contact {ContactId}",
            isUser ? "User" : "Agent", 
            requestingAgentId, 
            authorizedInteractions.Count, 
            interactions.Count, 
            contactId);

        return authorizedInteractions;
    }

    /// <inheritdoc/>
    public async Task<bool> CanAccessInteractionAsync(Guid interactionId, Guid requestingAgentId)
    {
        // First check if the interaction exists
        var interaction = await _context.Interactions
            .FirstOrDefaultAsync(i => i.Id == interactionId);

        if (interaction == null)
        {
            _logger.LogWarning("Interaction {InteractionId} not found", interactionId);
            await LogInteractionAccessAsync(interactionId, requestingAgentId, false, "Interaction not found");
            return false;
        }

        // Check if the requesting entity can access the contact
        var canAccessContact = await _contactAccessService.CanAccessContactAsync(interaction.ContactId, requestingAgentId);
        if (!canAccessContact)
        {
            await LogInteractionAccessAsync(interactionId, requestingAgentId, false, "Contact access denied");
            return false;
        }

        // Check if this is a user or an agent
        var isUser = await _context.Users.AnyAsync(u => u.Id == requestingAgentId);
        var isAgent = await _context.Agents.AnyAsync(a => a.Id == requestingAgentId);

        if (!isUser && !isAgent)
        {
            _logger.LogWarning("User/Agent {Id} not found", requestingAgentId);
            await LogInteractionAccessAsync(interactionId, requestingAgentId, false, "Requesting entity not found");
            return false;
        }

        // Get permissions and ownership
        var collaboration = await _contactAccessService.GetCollaborationPermissionsAsync(interaction.ContactId, requestingAgentId);
        var isOwner = await _contactAccessService.IsContactOwnerAsync(interaction.ContactId, requestingAgentId);
        var isAdmin = isUser && await _contactAccessService.IsOrganizationOwnerAsync(requestingAgentId);

        // Check access based on interaction type and permissions
        var canAccess = await CanAccessInteractionInternalAsync(
            interaction, 
            requestingAgentId,
            isUser,
            isAgent,
            isOwner,
            isAdmin,
            collaboration);
        
        var reason = canAccess ? "Access granted" : "Access restricted by privacy settings";
        await LogInteractionAccessAsync(interactionId, requestingAgentId, canAccess, reason);

        _logger.LogDebug("Access {AccessStatus} for {EntityType} {EntityId} to interaction {InteractionId}: {Reason}",
            canAccess ? "granted" : "denied",
            isUser ? "User" : "Agent",
            requestingAgentId,
            interactionId,
            reason);

        return canAccess;
    }

    public async Task<IEnumerable<Interaction>> GetBusinessInteractionsAsync(Guid contactId, Guid requestingAgentId)
    {
        // First check if the requesting entity can access the contact at all
        var canAccessContact = await _contactAccessService.CanAccessContactAsync(contactId, requestingAgentId);
        if (!canAccessContact)
        {
            await LogInteractionAccessAsync(Guid.Empty, requestingAgentId, false, "Contact access denied");
            return Enumerable.Empty<Interaction>();
        }

        // Check if this is a user or an agent
        var isUser = await _context.Users.AnyAsync(u => u.Id == requestingAgentId);
        var isAgent = await _context.Agents.AnyAsync(a => a.Id == requestingAgentId);

        if (!isUser && !isAgent)
        {
            _logger.LogWarning("User/Agent {Id} not found", requestingAgentId);
            return Enumerable.Empty<Interaction>();
        }

        // Get all business interactions (explicitly tagged as business or untagged)
        var businessInteractions = await _context.Interactions
            .Where(i => i.ContactId == contactId && 
                      (i.Tags == null || !i.Tags.Contains("PERSONAL") && !i.Tags.Contains("PRIVATE")))
            .OrderByDescending(i => i.Timestamp)
            .ToListAsync();

        // Filter based on user/agent permissions
        var filteredInteractions = new List<Interaction>();
        var collaboration = await _contactAccessService.GetCollaborationPermissionsAsync(contactId, requestingAgentId);
        var isOwner = await _contactAccessService.IsContactOwnerAsync(contactId, requestingAgentId);
        var isAdmin = isUser && await _contactAccessService.IsOrganizationOwnerAsync(requestingAgentId);

        foreach (var interaction in businessInteractions)
        {
            var canAccess = await CanAccessInteractionInternalAsync(
                interaction, 
                requestingAgentId,
                isUser,
                isAgent,
                isOwner,
                isAdmin,
                collaboration);
                
            if (canAccess)
            {
                filteredInteractions.Add(interaction);
            }
        }

        _logger.LogInformation("{EntityType} {EntityId} accessed {Count} business interactions for contact {ContactId}",
            isUser ? "User" : "Agent",
            requestingAgentId,
            filteredInteractions.Count,
            contactId);

        return filteredInteractions;
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
        if (requestingAgentId == Guid.Empty)
        {
            _logger.LogWarning("Attempted to log interaction access with empty requester ID");
            return;
        }

        try
        {
            // Determine if the requester is a user or an agent
            var isUser = await _context.Users.AnyAsync(u => u.Id == requestingAgentId);
            var isAgent = !isUser && await _context.Agents.AnyAsync(a => a.Id == requestingAgentId);
            
            // If neither user nor agent found, log a warning but still record the access attempt
            if (!isUser && !isAgent)
            {
                _logger.LogWarning("Access attempt by unknown entity {EntityId} (Interaction: {InteractionId})", 
                    requestingAgentId, interactionId);
            }

            var logEntry = new InteractionAccessLog
            {
                Id = Guid.NewGuid(),
                InteractionId = interactionId != Guid.Empty ? interactionId : (Guid?)null,
                RequestingEntityId = requestingAgentId,
                EntityType = isUser ? AccessEntityType.User : 
                                 isAgent ? AccessEntityType.Agent : 
                                 AccessEntityType.Unknown,
                AccessGranted = accessGranted,
                AccessTimestamp = DateTime.UtcNow,
                Reason = reason?.Truncate(500) ?? "No reason provided",
                IpAddress = null, // Would be populated from HTTP context in a real implementation
                UserAgent = null,  // Would be populated from HTTP context in a real implementation
                Metadata = new Dictionary<string, object>
                {
                    ["IsUser"] = isUser,
                    ["IsAgent"] = isAgent,
                    ["EntityType"] = isUser ? "User" : isAgent ? "Agent" : "Unknown"
                },
                IpAddress = null, // Would be populated from HTTP context in a real implementation
                UserAgent = null  // Would be populated from HTTP context in a real implementation
            };
            
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
