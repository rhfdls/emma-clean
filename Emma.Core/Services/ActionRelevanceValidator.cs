using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Emma.Core.Services;

/// <summary>
/// Mission-critical service for validating whether scheduled actions are still relevant
/// before execution. Prevents automation failures and maintains user trust.
/// </summary>
public class ActionRelevanceValidator : IActionRelevanceValidator
{
    private readonly INbaContextService _nbaContextService;
    private readonly IAIFoundryService _aiFoundryService;
    private readonly IPromptProvider _promptProvider;
    private readonly ILogger<ActionRelevanceValidator> _logger;
    private ActionRelevanceConfig _config;
    private readonly List<ActionRelevanceResult> _auditLog = new();
    private readonly object _auditLock = new();

    public ActionRelevanceValidator(
        INbaContextService nbaContextService,
        IAIFoundryService aiFoundryService,
        IPromptProvider promptProvider,
        ILogger<ActionRelevanceValidator> logger)
    {
        _nbaContextService = nbaContextService;
        _aiFoundryService = aiFoundryService;
        _promptProvider = promptProvider;
        _logger = logger;
        _config = new ActionRelevanceConfig(); // Default configuration
    }

    public async Task<ActionRelevanceResult> ValidateActionRelevanceAsync(ActionRelevanceRequest request)
    {
        var traceId = request.TraceId ?? Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation(
                "Starting action relevance validation for action {ActionId}, type {ActionType}, TraceId: {TraceId}",
                request.Action.Id, request.Action.ActionType, traceId);

            // Step 1: Get fresh context if not provided
            var currentContext = request.CurrentContext;
            if (currentContext == null)
            {
                _logger.LogDebug("Retrieving fresh contact context for validation, TraceId: {TraceId}", traceId);
                
                // Get fresh NBA context for the contact
                var nbaContext = await _nbaContextService.GetNbaContextAsync(
                    request.Action.ContactId,
                    request.Action.OrganizationId,
                    Guid.Parse(request.Action.ScheduledByAgentId),
                    traceId: traceId);

                // Convert NBA context to ContactContext (simplified mapping)
                currentContext = new ContactContext
                {
                    ContactId = request.Action.ContactId,
                    // Map other properties as needed from nbaContext
                };
            }

            // Step 2: Evaluate relevance criteria
            var result = await EvaluateRelevanceCriteriaAsync(
                request.Action.RelevanceCriteria, 
                currentContext, 
                traceId);

            // Step 3: Use LLM validation if enabled and rule-based validation is uncertain
            if (request.UseLLMValidation && _config.EnableLLMValidation && 
                result.ConfidenceScore < _config.MinimumConfidenceScore)
            {
                _logger.LogDebug("Using LLM validation for uncertain result, TraceId: {TraceId}", traceId);
                var llmResult = await ValidateWithLLMAsync(request.Action, currentContext, traceId);
                
                // Combine results, giving preference to LLM if confidence is higher
                if (llmResult.ConfidenceScore > result.ConfidenceScore)
                {
                    result = llmResult;
                }
            }

            // Step 4: Update action status and audit log
            result.ActionId = request.Action.Id;
            result.TraceId = traceId;
            result.CheckedBy = nameof(ActionRelevanceValidator);

            if (_config.EnableAuditLogging)
            {
                await LogValidationResultAsync(result);
            }

            _logger.LogInformation(
                "Action relevance validation completed. Action {ActionId} is {Relevance} (confidence: {Confidence}), TraceId: {TraceId}",
                request.Action.Id, result.IsRelevant ? "RELEVANT" : "NOT RELEVANT", result.ConfidenceScore, traceId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error validating action relevance for action {ActionId}, TraceId: {TraceId}", 
                request.Action.Id, traceId);

            // Return safe default based on configuration
            return new ActionRelevanceResult
            {
                ActionId = request.Action.Id,
                IsRelevant = _config.DefaultActionOnUncertainty != "suppress",
                ConfidenceScore = 0.0,
                Reason = $"Validation failed: {ex.Message}",
                CheckedAt = DateTime.UtcNow,
                CheckedBy = nameof(ActionRelevanceValidator),
                TraceId = traceId
            };
        }
    }

    public async Task<List<ActionRelevanceResult>> ValidateBatchActionRelevanceAsync(List<ActionRelevanceRequest> requests)
    {
        var results = new List<ActionRelevanceResult>();
        var traceId = requests.FirstOrDefault()?.TraceId ?? Guid.NewGuid().ToString();

        _logger.LogInformation("Starting batch validation for {Count} actions, TraceId: {TraceId}", 
            requests.Count, traceId);

        // Process in parallel for efficiency, but limit concurrency
        var semaphore = new SemaphoreSlim(5); // Max 5 concurrent validations
        var tasks = requests.Select(async request =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await ValidateActionRelevanceAsync(request);
            }
            finally
            {
                semaphore.Release();
            }
        });

        results.AddRange(await Task.WhenAll(tasks));

        _logger.LogInformation("Batch validation completed. {RelevantCount}/{TotalCount} actions are relevant, TraceId: {TraceId}",
            results.Count(r => r.IsRelevant), results.Count, traceId);

        return results;
    }

    public async Task<bool> IsActionStillRelevantAsync(
        ScheduledAction action, 
        Guid contactId, 
        Guid organizationId, 
        string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            var request = new ActionRelevanceRequest
            {
                Action = action,
                TraceId = traceId
            };

            var result = await ValidateActionRelevanceAsync(request);
            return result.IsRelevant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error in quick relevance check for action {ActionId}, TraceId: {TraceId}", 
                action.Id, traceId);

            // Safe default based on configuration
            return _config.DefaultActionOnUncertainty != "suppress";
        }
    }

    public async Task<ActionRelevanceResult> EvaluateRelevanceCriteriaAsync(
        Dictionary<string, object> criteria, 
        ContactContext context, 
        string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        var result = new ActionRelevanceResult
        {
            IsRelevant = true,
            ConfidenceScore = 1.0,
            Reason = "All relevance criteria passed",
            CheckedAt = DateTime.UtcNow,
            CheckedBy = nameof(ActionRelevanceValidator),
            TraceId = traceId,
            ContextData = new Dictionary<string, object>
            {
                ["contactId"] = context.ContactId,
                ["evaluatedCriteria"] = criteria.Keys.ToList()
            }
        };

        var failedCriteria = new List<string>();

        foreach (var criterion in criteria)
        {
            var passed = await EvaluateSingleCriterionAsync(criterion.Key, criterion.Value, context, traceId);
            if (!passed)
            {
                failedCriteria.Add(criterion.Key);
                _logger.LogDebug("Relevance criterion failed: {Criterion} = {Value}, TraceId: {TraceId}",
                    criterion.Key, criterion.Value, traceId);
            }
        }

        if (failedCriteria.Any())
        {
            result.IsRelevant = false;
            result.ConfidenceScore = Math.Max(0.0, 1.0 - (failedCriteria.Count / (double)criteria.Count));
            result.Reason = $"Failed criteria: {string.Join(", ", failedCriteria)}";
            result.FailedCriteria = failedCriteria;
        }

        return result;
    }

    public async Task<ActionRelevanceResult> ValidateWithLLMAsync(
        ScheduledAction action, 
        ContactContext context, 
        string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            _logger.LogDebug("Starting LLM-based relevance validation for action {ActionId}, TraceId: {TraceId}",
                action.Id, traceId);

            // Build prompt for LLM validation
            var systemPrompt = await _promptProvider.GetSystemPromptAsync(
                "ActionRelevanceValidator", 
                null); // Use default industry profile

            var validationPrompt = $@"
Analyze whether the following scheduled action is still relevant given the current contact context.

SCHEDULED ACTION:
- Type: {action.ActionType}
- Description: {action.Description}
- Scheduled At: {action.ScheduledAt:yyyy-MM-dd HH:mm}
- Execute At: {action.ExecuteAt:yyyy-MM-dd HH:mm}
- Relevance Criteria: {JsonSerializer.Serialize(action.RelevanceCriteria)}

CURRENT CONTACT CONTEXT:
{JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true })}

EVALUATION INSTRUCTIONS:
1. Determine if the action is still appropriate given the current context
2. Consider if the contact's situation has changed since the action was scheduled
3. Evaluate if executing this action would be helpful or potentially harmful
4. Provide a confidence score between 0.0 and 1.0
5. Explain your reasoning clearly

Respond in JSON format:
{{
    ""isRelevant"": true/false,
    ""confidenceScore"": 0.0-1.0,
    ""reason"": ""detailed explanation"",
    ""recommendedAction"": ""proceed/reschedule/modify/cancel"",
    ""alternativeActions"": [""action1"", ""action2""]
}}";

            var aiResponse = await _aiFoundryService.ProcessAgentRequestAsync(
                systemPrompt, 
                validationPrompt, 
                traceId);

            // Parse LLM response
            var llmResult = ParseLLMValidationResponse(aiResponse.Content ?? "", action.Id, traceId);
            
            _logger.LogDebug("LLM validation completed with confidence {Confidence}, TraceId: {TraceId}",
                llmResult.ConfidenceScore, traceId);

            return llmResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LLM validation for action {ActionId}, TraceId: {TraceId}",
                action.Id, traceId);

            return new ActionRelevanceResult
            {
                ActionId = action.Id,
                IsRelevant = _config.DefaultActionOnUncertainty != "suppress",
                ConfidenceScore = 0.0,
                Reason = $"LLM validation failed: {ex.Message}",
                CheckedAt = DateTime.UtcNow,
                CheckedBy = "LLM-" + nameof(ActionRelevanceValidator),
                TraceId = traceId
            };
        }
    }

    public async Task<List<ScheduledAction>> SuggestAlternativeActionsAsync(
        ScheduledAction originalAction, 
        ContactContext context, 
        string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            _logger.LogDebug("Generating alternative actions for {ActionType}, TraceId: {TraceId}",
                originalAction.ActionType, traceId);

            // This is a simplified implementation - in production, you might use
            // more sophisticated logic or LLM-based suggestions
            var alternatives = new List<ScheduledAction>();

            // Example alternative action generation based on action type
            switch (originalAction.ActionType.ToLowerInvariant())
            {
                case "congrats_email":
                    alternatives.Add(CreateAlternativeAction(originalAction, "follow_up_email", 
                        "Follow up on recent activity", traceId));
                    break;
                
                case "appointment_reminder":
                    alternatives.Add(CreateAlternativeAction(originalAction, "reschedule_request", 
                        "Request to reschedule appointment", traceId));
                    break;
                
                case "property_recommendation":
                    alternatives.Add(CreateAlternativeAction(originalAction, "market_update", 
                        "Send market update instead", traceId));
                    break;
            }

            return alternatives;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating alternative actions, TraceId: {TraceId}", traceId);
            return new List<ScheduledAction>();
        }
    }

    public ActionRelevanceConfig GetValidationConfig()
    {
        return _config;
    }

    public async Task<bool> UpdateValidationConfigAsync(ActionRelevanceConfig config)
    {
        try
        {
            _config = config;
            _logger.LogInformation("Action relevance validation configuration updated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating validation configuration");
            return false;
        }
    }

    public async Task<List<ActionRelevanceResult>> GetValidationAuditLogAsync(
        Guid? contactId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? actionType = null)
    {
        lock (_auditLock)
        {
            var query = _auditLog.AsEnumerable();

            if (contactId.HasValue)
            {
                query = query.Where(r => r.ContextData.ContainsKey("contactId") && 
                    r.ContextData["contactId"].ToString() == contactId.ToString());
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.CheckedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.CheckedAt <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(r => r.ContextData.ContainsKey("actionType") && 
                    r.ContextData["actionType"].ToString()?.Equals(actionType, StringComparison.OrdinalIgnoreCase) == true);
            }

            return query.OrderByDescending(r => r.CheckedAt).ToList();
        }
    }

    #region Private Helper Methods

    private async Task<bool> EvaluateSingleCriterionAsync(
        string criterionKey, 
        object criterionValue, 
        ContactContext context, 
        string traceId)
    {
        try
        {
            // This is a simplified implementation - in production, you would have
            // more sophisticated criterion evaluation logic
            switch (criterionKey.ToLowerInvariant())
            {
                case "dealstatus":
                    // Example: Check if deal status matches expected value
                    var expectedStatus = criterionValue.ToString();
                    var currentStatus = context.AdditionalData?.GetValueOrDefault("dealStatus")?.ToString();
                    return string.Equals(expectedStatus, currentStatus, StringComparison.OrdinalIgnoreCase);

                case "contactengagement":
                    // Example: Check engagement level
                    var expectedEngagement = criterionValue.ToString();
                    var currentEngagement = context.AdditionalData?.GetValueOrDefault("engagementLevel")?.ToString();
                    return string.Equals(expectedEngagement, currentEngagement, StringComparison.OrdinalIgnoreCase);

                case "lastinteractionage":
                    // Example: Check if last interaction is within specified timeframe
                    if (int.TryParse(criterionValue.ToString(), out var maxDays))
                    {
                        var lastInteraction = context.LastInteractionDate ?? DateTime.MinValue;
                        return (DateTime.UtcNow - lastInteraction).TotalDays <= maxDays;
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown relevance criterion: {Criterion}, TraceId: {TraceId}", 
                        criterionKey, traceId);
                    return true; // Default to passing for unknown criteria
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating criterion {Criterion}, TraceId: {TraceId}", 
                criterionKey, traceId);
            return false; // Fail safe
        }
    }

    private ActionRelevanceResult ParseLLMValidationResponse(string llmResponse, string actionId, string traceId)
    {
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(llmResponse);
            
            return new ActionRelevanceResult
            {
                ActionId = actionId,
                IsRelevant = jsonResponse.GetProperty("isRelevant").GetBoolean(),
                ConfidenceScore = jsonResponse.GetProperty("confidenceScore").GetDouble(),
                Reason = jsonResponse.GetProperty("reason").GetString() ?? "LLM validation",
                RecommendedAction = jsonResponse.TryGetProperty("recommendedAction", out var recAction) 
                    ? recAction.GetString() : null,
                AlternativeActions = jsonResponse.TryGetProperty("alternativeActions", out var altActions)
                    ? altActions.EnumerateArray().Select(a => a.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                    : new List<string>(),
                CheckedAt = DateTime.UtcNow,
                CheckedBy = "LLM-" + nameof(ActionRelevanceValidator),
                TraceId = traceId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing LLM validation response, TraceId: {TraceId}", traceId);
            
            return new ActionRelevanceResult
            {
                ActionId = actionId,
                IsRelevant = false,
                ConfidenceScore = 0.0,
                Reason = "Failed to parse LLM response",
                CheckedAt = DateTime.UtcNow,
                CheckedBy = "LLM-" + nameof(ActionRelevanceValidator),
                TraceId = traceId
            };
        }
    }

    private ScheduledAction CreateAlternativeAction(
        ScheduledAction originalAction, 
        string newActionType, 
        string newDescription, 
        string traceId)
    {
        return new ScheduledAction
        {
            ActionType = newActionType,
            Description = newDescription,
            ContactId = originalAction.ContactId,
            OrganizationId = originalAction.OrganizationId,
            ScheduledByAgentId = originalAction.ScheduledByAgentId,
            ExecuteAt = DateTime.UtcNow.AddHours(1), // Schedule for 1 hour from now
            Parameters = new Dictionary<string, object>(originalAction.Parameters),
            RelevanceCriteria = new Dictionary<string, object>(originalAction.RelevanceCriteria),
            Priority = originalAction.Priority,
            TraceId = traceId
        };
    }

    private async Task LogValidationResultAsync(ActionRelevanceResult result)
    {
        lock (_auditLock)
        {
            _auditLog.Add(result);
            
            // Keep only the last 10,000 entries to prevent memory issues
            if (_auditLog.Count > 10000)
            {
                _auditLog.RemoveRange(0, _auditLog.Count - 10000);
            }
        }
    }

    #endregion
}
