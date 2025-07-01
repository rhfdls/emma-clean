using Emma.Core.Interfaces;
using Emma.Core.Services;
using Emma.Models.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Emma.Core.Extensions;

/// <summary>
/// Privacy-aware logging extensions that automatically mask sensitive data.
/// Enables effective debugging while maintaining privacy compliance.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs contact information with privacy-aware masking.
    /// </summary>
    public static void LogContactAccess(this ILogger logger, Contact contact, string action, string? agentId = null, 
        IDataMaskingService? maskingService = null)
    {
        if (maskingService != null)
        {
            var level = maskingService.GetMaskingLevel(agentId);
            var maskedContact = maskingService.MaskContact(contact, level);
            logger.LogInformation("Contact {Action}: {Contact}", action, maskingService.ToMaskedJson(maskedContact, level));
        }
        else
        {
            logger.LogInformation("Contact {Action}: ID={ContactId}, Name={FirstName} {LastName}", 
                action, contact.Id, contact.FirstName, contact.LastName);
        }
    }

    /// <summary>
    /// Logs interaction information with privacy-aware masking.
    /// </summary>
    public static void LogInteractionAccess(this ILogger logger, Interaction interaction, string action, 
        string? agentId = null, IDataMaskingService? maskingService = null)
    {
        if (maskingService != null)
        {
            var level = maskingService.GetMaskingLevel(agentId);
            var maskedInteraction = maskingService.MaskInteraction(interaction, level);
            logger.LogInformation("Interaction {Action}: {Interaction}", action, maskingService.ToMaskedJson(maskedInteraction, level));
        }
        else
        {
            logger.LogInformation("Interaction {Action}: ID={InteractionId}, Type={Type}, Tags={Tags}", 
                action, interaction.Id, interaction.Type, string.Join(",", interaction.Tags));
        }
    }

    /// <summary>
    /// Logs privacy enforcement decisions with context.
    /// </summary>
    public static void LogPrivacyDecision(this ILogger logger, string decision, string reason, 
        Guid? contactId = null, Guid? interactionId = null, string? agentId = null, 
        IEnumerable<string>? privacyTags = null)
    {
        logger.LogInformation("Privacy Decision: {Decision} - {Reason} | Agent: {AgentId} | Contact: {ContactId} | Interaction: {InteractionId} | Tags: {Tags}",
            decision, reason, agentId ?? "Unknown", contactId, interactionId, 
            privacyTags != null ? string.Join(",", privacyTags) : "None");
    }

    /// <summary>
    /// Logs access control violations for security monitoring.
    /// </summary>
    public static void LogAccessViolation(this ILogger logger, string violationType, string details, 
        string? agentId = null, Guid? resourceId = null, string? ipAddress = null)
    {
        logger.LogWarning("ACCESS VIOLATION: {ViolationType} - {Details} | Agent: {AgentId} | Resource: {ResourceId} | IP: {IPAddress}",
            violationType, details, agentId ?? "Unknown", resourceId, ipAddress ?? "Unknown");
    }

    /// <summary>
    /// Logs debugging information with automatic masking.
    /// </summary>
    public static void LogDebugMasked<T>(this ILogger logger, string message, T data, 
        string? agentId = null, IDataMaskingService? maskingService = null)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;

        if (maskingService != null)
        {
            var level = maskingService.GetMaskingLevel(agentId, isProduction: false); // Debug logs use dev masking
            var maskedJson = maskingService.ToMaskedJson(data, level);
            logger.LogDebug("{Message}: {Data}", message, maskedJson);
        }
        else
        {
            logger.LogDebug("{Message}: {Data}", message, JsonSerializer.Serialize(data));
        }
    }

    /// <summary>
    /// Logs performance metrics with privacy-safe identifiers.
    /// </summary>
    public static void LogPerformanceMetric(this ILogger logger, string operation, TimeSpan duration, 
        int recordCount = 0, string? agentId = null)
    {
        logger.LogInformation("Performance: {Operation} completed in {Duration}ms | Records: {RecordCount} | Agent: {AgentId}",
            operation, duration.TotalMilliseconds, recordCount, agentId ?? "Unknown");
    }

    /// <summary>
    /// Logs collaboration events with privacy context.
    /// </summary>
    public static void LogCollaborationEvent(this ILogger logger, string eventType, Guid contactId, 
        string? ownerAgentId = null, string? collaboratorAgentId = null, 
        CollaboratorRole? role = null, bool canAccessPersonal = false)
    {
        logger.LogInformation("Collaboration {EventType}: Contact={ContactId} | Owner={OwnerAgentId} | Collaborator={CollaboratorAgentId} | Role={Role} | PersonalAccess={PersonalAccess}",
            eventType, contactId, ownerAgentId ?? "Unknown", collaboratorAgentId ?? "Unknown", 
            role?.ToString() ?? "Unknown", canAccessPersonal);
    }
}
