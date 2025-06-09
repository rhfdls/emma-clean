using Emma.Core.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Mission-critical service for validating whether scheduled actions are still relevant
/// before execution. Prevents automation failures and maintains user trust.
/// </summary>
public interface IActionRelevanceValidator
{
    /// <summary>
    /// Validates whether a scheduled action is still relevant for execution
    /// </summary>
    /// <param name="request">Action relevance validation request</param>
    /// <returns>Relevance validation result with detailed reasoning</returns>
    Task<ActionRelevanceResult> ValidateActionRelevanceAsync(ActionRelevanceRequest request);
    
    /// <summary>
    /// Validates multiple scheduled actions in batch for efficiency
    /// </summary>
    /// <param name="requests">List of action relevance validation requests</param>
    /// <returns>List of relevance validation results</returns>
    Task<List<ActionRelevanceResult>> ValidateBatchActionRelevanceAsync(List<ActionRelevanceRequest> requests);
    
    /// <summary>
    /// Performs a quick relevance check using cached context data
    /// </summary>
    /// <param name="action">Scheduled action to validate</param>
    /// <param name="contactId">Contact ID for context retrieval</param>
    /// <param name="organizationId">Organization ID for context retrieval</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>True if action is still relevant, false otherwise</returns>
    Task<bool> IsActionStillRelevantAsync(
        ScheduledAction action, 
        Guid contactId, 
        Guid organizationId, 
        string? traceId = null);
    
    /// <summary>
    /// Evaluates relevance criteria against current contact context
    /// </summary>
    /// <param name="criteria">Relevance criteria to evaluate</param>
    /// <param name="context">Current contact context</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>Detailed evaluation result</returns>
    Task<ActionRelevanceResult> EvaluateRelevanceCriteriaAsync(
        Dictionary<string, object> criteria, 
        ContactContext context, 
        string? traceId = null);
    
    /// <summary>
    /// Uses LLM for semantic relevance validation when rule-based validation is insufficient
    /// </summary>
    /// <param name="action">Scheduled action to validate</param>
    /// <param name="context">Current contact context</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>LLM-based relevance validation result</returns>
    Task<ActionRelevanceResult> ValidateWithLLMAsync(
        ScheduledAction action, 
        ContactContext context, 
        string? traceId = null);
    
    /// <summary>
    /// Suggests alternative actions when the original action is no longer relevant
    /// </summary>
    /// <param name="originalAction">Original scheduled action that is no longer relevant</param>
    /// <param name="context">Current contact context</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>List of suggested alternative actions</returns>
    Task<List<ScheduledAction>> SuggestAlternativeActionsAsync(
        ScheduledAction originalAction, 
        ContactContext context, 
        string? traceId = null);
    
    /// <summary>
    /// Gets the current configuration for action relevance validation
    /// </summary>
    /// <returns>Current validation configuration</returns>
    ActionRelevanceConfig GetValidationConfig();
    
    /// <summary>
    /// Updates the configuration for action relevance validation
    /// </summary>
    /// <param name="config">New validation configuration</param>
    /// <returns>True if configuration was updated successfully</returns>
    Task<bool> UpdateValidationConfigAsync(ActionRelevanceConfig config);
    
    /// <summary>
    /// Retrieves audit log of relevance validation activities
    /// </summary>
    /// <param name="contactId">Optional contact ID filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="actionType">Optional action type filter</param>
    /// <returns>List of relevance validation audit entries</returns>
    Task<List<ActionRelevanceResult>> GetValidationAuditLogAsync(
        Guid? contactId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? actionType = null);
}
