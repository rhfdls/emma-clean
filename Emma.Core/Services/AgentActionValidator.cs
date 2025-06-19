using Emma.Models.Interfaces;
using Microsoft.Extensions.Logging;
using Emma.Core.Models;

namespace Emma.Core.Services;

/// <summary>
/// Standardized implementation for validating agent actions across all agents
/// Provides consistent action relevance validation, approval workflows, and explainability
/// </summary>
public class AgentActionValidator : IAgentActionValidator
{
    private readonly IActionRelevanceValidator _actionRelevanceValidator;
    private readonly ILogger<AgentActionValidator> _logger;

    public AgentActionValidator(
        IActionRelevanceValidator actionRelevanceValidator,
        ILogger<AgentActionValidator> logger)
    {
        _actionRelevanceValidator = actionRelevanceValidator;
        _logger = logger;
    }

    public async Task<List<T>> ValidateAgentActionsAsync<T>(
        List<T> actions, 
        AgentActionValidationContext context,
        Dictionary<string, object> userOverrides,
        string traceId) where T : IAgentAction
    {
        var validatedActions = new List<T>();

        _logger.LogInformation("🔍 Starting validation of {Count} {AgentType} actions with userOverrides, TraceId: {TraceId}",
            actions.Count, context.AgentType, traceId);

        foreach (var action in actions)
        {
            try
            {
                var validatedAction = await ValidateSingleActionAsync(action, context, userOverrides, traceId);
                if (validatedAction != null)
                {
                    validatedActions.Add(validatedAction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validating {AgentType} action {ActionType}, TraceId: {TraceId}",
                    context.AgentType, action.ActionType, traceId);
                
                // Include action but mark as requiring approval for safety
                action.RequiresApproval = true;
                action.ValidationReason = "Validation error - requires manual review";
                action.ConfidenceScore = 0.0;
                validatedActions.Add(action);
            }
        }

        _logger.LogInformation("✅ Validated {Count} {AgentType} actions, {ApprovalCount} require approval, {FilteredCount} filtered out, TraceId: {TraceId}",
            validatedActions.Count, 
            context.AgentType,
            validatedActions.Count(a => a.RequiresApproval), 
            actions.Count - validatedActions.Count,
            traceId);

        return validatedActions;
    }

    public async Task<T?> ValidateSingleActionAsync<T>(
        T action, 
        AgentActionValidationContext context,
        Dictionary<string, object> userOverrides,
        string traceId) where T : IAgentAction
    {
        try
        {
            _logger.LogDebug("🔍 Validating {AgentType} action: {ActionType} (Scope: {ActionScope}) with userOverrides, TraceId: {TraceId}",
                context.AgentType, action.ActionType, action.ActionScope, traceId);

            // Step 1: Convert action to scheduled action for validation
            var scheduledAction = ConvertToScheduledAction(action, context);

            // Step 2: Apply scope-aware validation logic
            ActionRelevanceResult relevanceResult;
            bool requiresApproval;
            string? approvalRequestId = null;

            switch (action.ActionScope)
            {
                case ActionScope.InnerWorld:
                    // Minimal validation for high-frequency internal actions
                    relevanceResult = await ValidateInnerWorldAction(action, scheduledAction, context, userOverrides, traceId);
                    requiresApproval = false; // Auto-approve inner-world actions
                    break;

                case ActionScope.Hybrid:
                    // Moderate validation for actions that impact downstream external actions
                    relevanceResult = await ValidateHybridAction(action, scheduledAction, context, userOverrides, traceId);
                    requiresApproval = await ShouldRequireApprovalForHybrid(scheduledAction, relevanceResult, context, traceId);
                    break;

                case ActionScope.RealWorld:
                    // Full validation for external actions with real-world impact
                    relevanceResult = await ValidateRealWorldAction(action, scheduledAction, context, userOverrides, traceId);
                    requiresApproval = true; // Always require approval for real-world actions
                    break;

                default:
                    throw new ArgumentException($"Unknown ActionScope: {action.ActionScope}");
            }

            // Step 3: Create approval request if needed
            if (requiresApproval && relevanceResult.IsRelevant)
            {
                var approvalRequest = await _actionRelevanceValidator.CreateApprovalRequestAsync(
                    scheduledAction, 
                    relevanceResult, 
                    context.UserId, 
                    "Action requires approval based on validation results",
                    userOverrides, // Pass userOverrides to approval request
                    traceId);
                
                approvalRequestId = approvalRequest.RequestId;
                
                _logger.LogInformation("📋 Created approval request {RequestId} for {ActionType}, TraceId: {TraceId}",
                    approvalRequestId, action.ActionType, traceId);
            }

            // Step 4: Apply validation results to original action
            var validatedAction = ApplyValidationResults(action, relevanceResult, requiresApproval, approvalRequestId);

            // Step 5: Filter out irrelevant actions
            if (!relevanceResult.IsRelevant)
            {
                _logger.LogDebug("🚫 Filtered out irrelevant {ActionType}: {Reason}, TraceId: {TraceId}",
                    action.ActionType, relevanceResult.Reason, traceId);
                return default(T);
            }

            _logger.LogDebug("✅ Validated {ActionType}: Relevant={IsRelevant}, Confidence={Confidence:F2}, RequiresApproval={RequiresApproval}, TraceId: {TraceId}",
                action.ActionType, relevanceResult.IsRelevant, relevanceResult.ConfidenceScore, requiresApproval, traceId);

            return validatedAction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in single action validation for {ActionType}, TraceId: {TraceId}",
                action.ActionType, traceId);
            throw;
        }
    }

    public ScheduledAction ConvertToScheduledAction<T>(T action, AgentActionValidationContext context) where T : IAgentAction
    {
        return new ScheduledAction
        {
            Id = Guid.NewGuid().ToString(),
            ActionType = action.ActionType,
            Description = action.Description,
            ContactId = context.ContactId,
            OrganizationId = context.OrganizationId,
            ScheduledByAgentId = context.AgentId,
            ExecuteAt = action.SuggestedTiming ?? DateTime.UtcNow.AddHours(1),
            Parameters = action.Parameters ?? new Dictionary<string, object>(),
            RelevanceCriteria = new Dictionary<string, object>
            {
                ["priority"] = action.Priority,
                ["actionType"] = action.ActionType,
                ["confidence"] = action.ConfidenceScore,
                ["agentType"] = context.AgentType
            },
            Priority = (UrgencyLevel)action.Priority,
            TraceId = action.TraceId
        };
    }

    public T ApplyValidationResults<T>(
        T action, 
        ActionRelevanceResult relevanceResult, 
        bool requiresApproval, 
        string? approvalRequestId = null) where T : IAgentAction
    {
        // Update action with validation results
        action.ConfidenceScore = relevanceResult.ConfidenceScore;
        action.ValidationReason = relevanceResult.Reason;
        action.RequiresApproval = requiresApproval;
        action.ApprovalRequestId = approvalRequestId ?? string.Empty;

        return action;
    }

    private async Task<ActionRelevanceResult> ValidateInnerWorldAction<T>(
        T action, 
        ScheduledAction scheduledAction, 
        AgentActionValidationContext context,
        Dictionary<string, object> userOverrides,
        string traceId) where T : IAgentAction
    {
        // Minimal validation for high-frequency internal actions
        // Fast schema-based validation without LLM overhead
        _logger.LogDebug("🔍 InnerWorld validation: {ActionType}, TraceId: {TraceId}", action.ActionType, traceId);

        // Basic schema validation without LLM
        var relevanceRequest = new ActionRelevanceRequest
        {
            Action = scheduledAction,
            UseLLMValidation = false, // Skip LLM for performance
            AdditionalContext = context.AdditionalContext,
            UserOverrides = userOverrides, // Include userOverrides for audit trail
            TraceId = traceId
        };

        var result = await _actionRelevanceValidator.ValidateActionRelevanceAsync(relevanceRequest);

        // Apply basic confidence threshold
        if (result.ConfidenceScore < 0.5)
        {
            result.IsRelevant = false;
            result.Reason = $"InnerWorld: Confidence {result.ConfidenceScore:F2} below threshold 0.5. {result.Reason}";
        }
        else
        {
            result.Reason = $"InnerWorld: {result.Reason}";
        }

        return result;
    }

    private async Task<ActionRelevanceResult> ValidateHybridAction<T>(
        T action, 
        ScheduledAction scheduledAction, 
        AgentActionValidationContext context,
        Dictionary<string, object> userOverrides,
        string traceId) where T : IAgentAction
    {
        // Moderate validation for actions that impact downstream external actions
        // Balanced approach with selective LLM usage
        _logger.LogDebug("🔍 Hybrid validation: {ActionType}, TraceId: {TraceId}", action.ActionType, traceId);

        // Enhanced validation with moderate LLM usage
        var relevanceRequest = new ActionRelevanceRequest
        {
            Action = scheduledAction,
            UseLLMValidation = true, // Use LLM but with lighter prompts
            AdditionalContext = context.AdditionalContext,
            UserOverrides = userOverrides, // Include userOverrides for LLM decision-making
            TraceId = traceId
        };

        var result = await _actionRelevanceValidator.ValidateActionRelevanceAsync(relevanceRequest);

        // Apply moderate confidence threshold
        if (result.ConfidenceScore < 0.7)
        {
            result.IsRelevant = false;
            result.Reason = $"Hybrid: Confidence {result.ConfidenceScore:F2} below threshold 0.7. {result.Reason}";
        }
        else
        {
            result.Reason = $"Hybrid: {result.Reason}";
        }

        return result;
    }

    private async Task<ActionRelevanceResult> ValidateRealWorldAction<T>(
        T action, 
        ScheduledAction scheduledAction, 
        AgentActionValidationContext context,
        Dictionary<string, object> userOverrides,
        string traceId) where T : IAgentAction
    {
        // Full validation for external actions with real-world impact
        // Comprehensive LLM assessment with strict confidence requirements
        _logger.LogDebug("🔍 RealWorld validation: {ActionType}, TraceId: {TraceId}", action.ActionType, traceId);

        // Full LLM validation with comprehensive context
        var relevanceRequest = new ActionRelevanceRequest
        {
            Action = scheduledAction,
            UseLLMValidation = true, // Full LLM validation
            AdditionalContext = context.AdditionalContext,
            UserOverrides = userOverrides, // Critical for LLM decision-making and explainability
            TraceId = traceId
        };

        var result = await _actionRelevanceValidator.ValidateActionRelevanceAsync(relevanceRequest);

        // Apply strict confidence threshold for real-world actions
        if (result.ConfidenceScore < 0.8)
        {
            result.IsRelevant = false;
            result.Reason = $"RealWorld: Confidence {result.ConfidenceScore:F2} below threshold 0.8. {result.Reason}";
        }
        else
        {
            result.Reason = $"RealWorld: {result.Reason}";
        }

        return result;
    }

    private async Task<bool> ShouldRequireApprovalForHybrid(
        ScheduledAction scheduledAction, 
        ActionRelevanceResult relevanceResult, 
        AgentActionValidationContext context, 
        string traceId)
    {
        // Conditional approval logic for Hybrid actions
        // Require approval for high-risk or low-confidence hybrid actions
        
        // Auto-approve if confidence is high and action is low-risk
        if (relevanceResult.ConfidenceScore >= 0.9)
        {
            _logger.LogDebug("🟢 Hybrid action auto-approved: High confidence {Confidence:F2}, TraceId: {TraceId}", 
                relevanceResult.ConfidenceScore, traceId);
            return false;
        }

        // Require approval for moderate confidence or potentially risky actions
        if (relevanceResult.ConfidenceScore < 0.8)
        {
            _logger.LogDebug("🟡 Hybrid action requires approval: Low confidence {Confidence:F2}, TraceId: {TraceId}", 
                relevanceResult.ConfidenceScore, traceId);
            return true;
        }

        // Check for high-risk action types that should require approval
        var highRiskActionTypes = new[] { "risk_assessment", "compliance_check", "orchestration_decision", "intent_classification" };
        if (highRiskActionTypes.Contains(scheduledAction.ActionType.ToLowerInvariant()))
        {
            _logger.LogDebug("🟡 Hybrid action requires approval: High-risk type {ActionType}, TraceId: {TraceId}", 
                scheduledAction.ActionType, traceId);
            return true;
        }

        // Default to auto-approve for moderate confidence hybrid actions
        _logger.LogDebug("🟢 Hybrid action auto-approved: Moderate confidence {Confidence:F2}, TraceId: {TraceId}", 
            relevanceResult.ConfidenceScore, traceId);
        return false;
    }
}
