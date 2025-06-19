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
    /// <param name="userOverrides">User override preferences to include in LLM decision-making</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>LLM-based relevance validation result</returns>
    Task<ActionRelevanceResult> ValidateWithLLMAsync(
        ScheduledAction action, 
        ContactContext context,
        Dictionary<string, object>? userOverrides = null,
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

    /// <summary>
    /// Determines if an action requires user approval based on operating mode and context
    /// </summary>
    /// <param name="action">Scheduled action to evaluate</param>
    /// <param name="relevanceResult">Result of relevance validation</param>
    /// <param name="userId">User ID for personalized approval rules</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>True if user approval is required, false if action can proceed automatically</returns>
    Task<bool> RequiresUserApprovalAsync(
        ScheduledAction action, 
        ActionRelevanceResult relevanceResult, 
        string userId, 
        string? traceId = null);

    /// <summary>
    /// Creates a user approval request for actions that require manual review
    /// </summary>
    /// <param name="action">Scheduled action requiring approval</param>
    /// <param name="relevanceResult">Result of relevance validation</param>
    /// <param name="userId">User ID who needs to provide approval</param>
    /// <param name="reason">Reason why approval is needed</param>
    /// <param name="userOverrides">Original user override preferences that led to this approval request</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>User approval request</returns>
    Task<UserApprovalRequest> CreateApprovalRequestAsync(
        ScheduledAction action, 
        ActionRelevanceResult relevanceResult, 
        string userId, 
        string reason,
        Dictionary<string, object> userOverrides,
        string? traceId = null);

    /// <summary>
    /// Processes a user approval response
    /// </summary>
    /// <param name="response">User's approval response</param>
    /// <returns>Updated scheduled action based on user decision</returns>
    Task<ScheduledAction?> ProcessApprovalResponseAsync(UserApprovalResponse response);

    /// <summary>
    /// Gets pending approval requests for a user
    /// </summary>
    /// <param name="userId">User ID to get requests for</param>
    /// <param name="includeExpired">Whether to include expired requests</param>
    /// <returns>List of pending approval requests</returns>
    Task<List<UserApprovalRequest>> GetPendingApprovalsAsync(string userId, bool includeExpired = false);

    /// <summary>
    /// Uses LLM to determine if user approval is needed based on context and risk assessment
    /// </summary>
    /// <param name="action">Scheduled action to evaluate</param>
    /// <param name="relevanceResult">Result of relevance validation</param>
    /// <param name="context">Current contact context</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>True if LLM recommends user approval, false otherwise</returns>
    Task<bool> LLMRecommendsApprovalAsync(
        ScheduledAction action, 
        ActionRelevanceResult relevanceResult, 
        ContactContext context, 
        string? traceId = null);

    /// <summary>
    /// Applies bulk approval decision to similar pending actions
    /// </summary>
    /// <param name="response">User approval response with bulk flag</param>
    /// <param name="userId">User ID for filtering similar actions</param>
    /// <returns>Number of actions affected by bulk approval</returns>
    Task<int> ApplyBulkApprovalAsync(UserApprovalResponse response, string userId);
}
