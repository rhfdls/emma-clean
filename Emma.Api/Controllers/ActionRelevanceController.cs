using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Controllers;

/// <summary>
/// REST API controller for managing scheduled actions and action relevance validation
/// Mission-critical: Provides operational governance and audit capabilities for automation safeguards
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ActionRelevanceController : ControllerBase
{
    private readonly IActionRelevanceValidator _actionRelevanceValidator;
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly ILogger<ActionRelevanceController> _logger;

    public ActionRelevanceController(
        IActionRelevanceValidator actionRelevanceValidator,
        IAgentOrchestrator agentOrchestrator,
        ILogger<ActionRelevanceController> logger)
    {
        _actionRelevanceValidator = actionRelevanceValidator;
        _agentOrchestrator = agentOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Validates whether a scheduled action is still relevant for execution
    /// </summary>
    /// <param name="request">Action relevance validation request</param>
    /// <returns>Detailed relevance validation result</returns>
    [HttpPost("validate")]
    public async Task<ActionResult<ActionRelevanceResult>> ValidateActionRelevance(
        [FromBody] ActionRelevanceRequest request)
    {
        try
        {
            var traceId = request.TraceId ?? Guid.NewGuid().ToString();
            
            _logger.LogInformation(
                "API: Validating action relevance for action {ActionId}, type {ActionType}, TraceId: {TraceId}",
                request.Action.Id, request.Action.ActionType, traceId);

            var result = await _actionRelevanceValidator.ValidateActionRelevanceAsync(request);

            _logger.LogInformation(
                "API: Action relevance validation completed. Action {ActionId} is {Relevance} (confidence: {Confidence}), TraceId: {TraceId}",
                request.Action.Id, result.IsRelevant ? "RELEVANT" : "NOT RELEVANT", result.ConfidenceScore, traceId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error validating action relevance for action {ActionId}", 
                request.Action.Id);

            return Problem(
                title: "Action Relevance Validation Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Validates multiple scheduled actions in batch for efficiency
    /// </summary>
    /// <param name="requests">List of action relevance validation requests</param>
    /// <returns>List of relevance validation results</returns>
    [HttpPost("validate/batch")]
    public async Task<ActionResult<List<ActionRelevanceResult>>> ValidateBatchActionRelevance(
        [FromBody] List<ActionRelevanceRequest> requests)
    {
        try
        {
            var traceId = requests.FirstOrDefault()?.TraceId ?? Guid.NewGuid().ToString();
            
            _logger.LogInformation("API: Starting batch validation for {Count} actions, TraceId: {TraceId}", 
                requests.Count, traceId);

            var results = await _actionRelevanceValidator.ValidateBatchActionRelevanceAsync(requests);

            _logger.LogInformation("API: Batch validation completed. {RelevantCount}/{TotalCount} actions are relevant, TraceId: {TraceId}",
                results.Count(r => r.IsRelevant), results.Count, traceId);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error in batch action relevance validation");

            return Problem(
                title: "Batch Action Relevance Validation Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Performs a quick relevance check for a scheduled action
    /// </summary>
    /// <param name="actionId">Scheduled action ID</param>
    /// <param name="contactId">Contact ID for context retrieval</param>
    /// <param name="organizationId">Organization ID for context retrieval</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>True if action is still relevant, false otherwise</returns>
    [HttpGet("quick-check/{actionId}")]
    public async Task<ActionResult<bool>> QuickRelevanceCheck(
        string actionId,
        [FromQuery] Guid contactId,
        [FromQuery] Guid organizationId,
        [FromQuery] string? traceId = null)
    {
        try
        {
            traceId ??= Guid.NewGuid().ToString();

            _logger.LogInformation("API: Quick relevance check for action {ActionId}, TraceId: {TraceId}", 
                actionId, traceId);

            // Get the scheduled action (this would typically come from a repository)
            var scheduledActions = await _agentOrchestrator.GetScheduledActionsAsync(contactId, null, traceId);
            var action = scheduledActions.FirstOrDefault(a => a.Id == actionId);

            if (action == null)
            {
                _logger.LogWarning("API: Scheduled action {ActionId} not found, TraceId: {TraceId}", 
                    actionId, traceId);
                return NotFound($"Scheduled action {actionId} not found");
            }

            var isRelevant = await _actionRelevanceValidator.IsActionStillRelevantAsync(
                action, contactId, organizationId, traceId);

            _logger.LogInformation("API: Quick relevance check result: {IsRelevant}, TraceId: {TraceId}", 
                isRelevant, traceId);

            return Ok(isRelevant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error in quick relevance check for action {ActionId}", actionId);

            return Problem(
                title: "Quick Relevance Check Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Suggests alternative actions when the original action is no longer relevant
    /// </summary>
    /// <param name="actionId">Original scheduled action ID</param>
    /// <param name="contactId">Contact ID for context retrieval</param>
    /// <param name="organizationId">Organization ID for context retrieval</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>List of suggested alternative actions</returns>
    [HttpGet("alternatives/{actionId}")]
    public async Task<ActionResult<List<ScheduledAction>>> GetAlternativeActions(
        string actionId,
        [FromQuery] Guid contactId,
        [FromQuery] Guid organizationId,
        [FromQuery] string? traceId = null)
    {
        try
        {
            traceId ??= Guid.NewGuid().ToString();

            _logger.LogInformation("API: Getting alternative actions for {ActionId}, TraceId: {TraceId}", 
                actionId, traceId);

            // Get the scheduled action
            var scheduledActions = await _agentOrchestrator.GetScheduledActionsAsync(contactId, null, traceId);
            var originalAction = scheduledActions.FirstOrDefault(a => a.Id == actionId);

            if (originalAction == null)
            {
                _logger.LogWarning("API: Scheduled action {ActionId} not found for alternatives, TraceId: {TraceId}", 
                    actionId, traceId);
                return NotFound($"Scheduled action {actionId} not found");
            }

            // Get current contact context (simplified - would use proper context service)
            var currentContext = new ContactContext
            {
                ContactId = contactId,
                OrganizationId = organizationId,
                LastInteractionDate = DateTime.UtcNow
            };

            var alternatives = await _actionRelevanceValidator.SuggestAlternativeActionsAsync(
                originalAction, currentContext, traceId);

            _logger.LogInformation("API: Generated {Count} alternative actions for {ActionId}, TraceId: {TraceId}",
                alternatives.Count, actionId, traceId);

            return Ok(alternatives);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error getting alternative actions for {ActionId}", actionId);

            return Problem(
                title: "Alternative Actions Retrieval Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Schedules a new action with relevance criteria
    /// </summary>
    /// <param name="request">Schedule action request</param>
    /// <returns>Scheduled action ID</returns>
    [HttpPost("schedule")]
    public async Task<ActionResult<string>> ScheduleAction([FromBody] ScheduleActionRequest request)
    {
        try
        {
            var traceId = request.TraceId ?? Guid.NewGuid().ToString();

            _logger.LogInformation(
                "API: Scheduling action {ActionType} for contact {ContactId} at {ExecuteAt}, TraceId: {TraceId}",
                request.ActionType, request.ContactId, request.ExecuteAt, traceId);

            var actionId = await _agentOrchestrator.ScheduleActionAsync(
                request.ActionType,
                request.Description,
                request.ContactId,
                request.OrganizationId,
                request.AgentId,
                request.ExecuteAt,
                request.Parameters,
                request.RelevanceCriteria,
                request.Priority,
                traceId);

            _logger.LogInformation("API: Action scheduled successfully with ID {ActionId}, TraceId: {TraceId}",
                actionId, traceId);

            return Ok(actionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error scheduling action {ActionType}", request.ActionType);

            return Problem(
                title: "Action Scheduling Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Cancels a scheduled action
    /// </summary>
    /// <param name="actionId">Scheduled action ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>True if cancelled successfully</returns>
    [HttpDelete("{actionId}")]
    public async Task<ActionResult<bool>> CancelScheduledAction(
        string actionId,
        [FromQuery] string reason,
        [FromQuery] string? traceId = null)
    {
        try
        {
            traceId ??= Guid.NewGuid().ToString();

            _logger.LogInformation("API: Cancelling scheduled action {ActionId}, reason: {Reason}, TraceId: {TraceId}",
                actionId, reason, traceId);

            var cancelled = await _agentOrchestrator.CancelScheduledActionAsync(actionId, reason, traceId);

            if (cancelled)
            {
                _logger.LogInformation("API: Scheduled action {ActionId} cancelled successfully, TraceId: {TraceId}",
                    actionId, traceId);
            }
            else
            {
                _logger.LogWarning("API: Failed to cancel scheduled action {ActionId}, TraceId: {TraceId}",
                    actionId, traceId);
            }

            return Ok(cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error cancelling scheduled action {ActionId}", actionId);

            return Problem(
                title: "Action Cancellation Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Gets all scheduled actions for a contact
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="traceId">Optional trace ID for correlation</param>
    /// <returns>List of scheduled actions</returns>
    [HttpGet("contact/{contactId}")]
    public async Task<ActionResult<List<ScheduledAction>>> GetScheduledActions(
        Guid contactId,
        [FromQuery] ScheduledActionStatus? status = null,
        [FromQuery] string? traceId = null)
    {
        try
        {
            traceId ??= Guid.NewGuid().ToString();

            _logger.LogInformation("API: Getting scheduled actions for contact {ContactId}, status: {Status}, TraceId: {TraceId}",
                contactId, status?.ToString() ?? "All", traceId);

            var actions = await _agentOrchestrator.GetScheduledActionsAsync(contactId, status, traceId);

            _logger.LogInformation("API: Retrieved {Count} scheduled actions for contact {ContactId}, TraceId: {TraceId}",
                actions.Count, contactId, traceId);

            return Ok(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error getting scheduled actions for contact {ContactId}", contactId);

            return Problem(
                title: "Scheduled Actions Retrieval Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Gets the current configuration for action relevance validation
    /// </summary>
    /// <returns>Current validation configuration</returns>
    [HttpGet("config")]
    public ActionResult<ActionRelevanceConfig> GetValidationConfig()
    {
        try
        {
            _logger.LogInformation("API: Getting action relevance validation configuration");

            var config = _actionRelevanceValidator.GetValidationConfig();

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error getting validation configuration");

            return Problem(
                title: "Configuration Retrieval Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Updates the configuration for action relevance validation
    /// </summary>
    /// <param name="config">New validation configuration</param>
    /// <returns>True if configuration was updated successfully</returns>
    [HttpPut("config")]
    public async Task<ActionResult<bool>> UpdateValidationConfig([FromBody] ActionRelevanceConfig config)
    {
        try
        {
            _logger.LogInformation("API: Updating action relevance validation configuration");

            var updated = await _actionRelevanceValidator.UpdateValidationConfigAsync(config);

            if (updated)
            {
                _logger.LogInformation("API: Validation configuration updated successfully");
            }
            else
            {
                _logger.LogWarning("API: Failed to update validation configuration");
            }

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error updating validation configuration");

            return Problem(
                title: "Configuration Update Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Retrieves audit log of relevance validation activities
    /// </summary>
    /// <param name="contactId">Optional contact ID filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="actionType">Optional action type filter</param>
    /// <returns>List of relevance validation audit entries</returns>
    [HttpGet("audit-log")]
    public async Task<ActionResult<List<ActionRelevanceResult>>> GetValidationAuditLog(
        [FromQuery] Guid? contactId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? actionType = null)
    {
        try
        {
            _logger.LogInformation("API: Getting validation audit log with filters - ContactId: {ContactId}, StartDate: {StartDate}, EndDate: {EndDate}, ActionType: {ActionType}",
                contactId, startDate, endDate, actionType);

            var auditLog = await _actionRelevanceValidator.GetValidationAuditLogAsync(
                contactId, startDate, endDate, actionType);

            _logger.LogInformation("API: Retrieved {Count} audit log entries", auditLog.Count);

            return Ok(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error getting validation audit log");

            return Problem(
                title: "Audit Log Retrieval Failed",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}

/// <summary>
/// Request model for scheduling actions with relevance criteria
/// </summary>
public class ScheduleActionRequest
{
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public DateTime ExecuteAt { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> RelevanceCriteria { get; set; } = new();
    public UrgencyLevel Priority { get; set; } = UrgencyLevel.Medium;
    public string? TraceId { get; set; }
}
