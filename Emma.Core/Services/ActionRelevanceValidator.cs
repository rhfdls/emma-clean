using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Timers;

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

    // User approval management
    private readonly Dictionary<string, UserApprovalRequest> _pendingApprovals = new();
    private readonly object _approvalLock = new();
    private readonly System.Timers.Timer _approvalCleanupTimer;

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

        // Setup cleanup timer for expired approvals (runs every 5 minutes)
        _approvalCleanupTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        _approvalCleanupTimer.Elapsed += CleanupExpiredApprovals;
        _approvalCleanupTimer.Start();
    }

    public async Task<ActionRelevanceResult> ValidateActionRelevanceAsync(ActionRelevanceRequest request)
    {
        var traceId = request.TraceId ?? Guid.NewGuid().ToString();
        
        try
        {
            // Log userOverrides for audit trail
            var userOverridesLog = request.UserOverrides?.SerializeForAuditLog() ?? "{}";
            _logger.LogInformation(
                "🔍 Starting action relevance validation for action {ActionId}, type {ActionType}, userOverrides: {UserOverrides}, TraceId: {TraceId}",
                request.Action.Id, request.Action.ActionType, userOverridesLog, traceId);

            // Step 1: Get fresh context if not provided
            var currentContext = request.CurrentContext;
            if (currentContext == null)
            {
                _logger.LogDebug("Retrieving fresh contact context for validation, TraceId: {TraceId}", traceId);
                
                // Get fresh NBA context for the contact
                var nbaContext = await _nbaContextService.GetNbaContextAsync(
                    request.Action.ContactId,
                    request.Action.OrganizationId,
                    Guid.Parse(request.Action.ScheduledByAgentId));

                // Convert NBA context to ContactContext (simplified mapping)
                currentContext = new ContactContext
                {
                    ContactId = request.Action.ContactId,
                    OrganizationId = request.Action.OrganizationId,
                    LastInteraction = nbaContext.RecentInteractions?.FirstOrDefault()?.Timestamp,
                    InteractionSummary = nbaContext.RollingSummary?.SummaryText ?? "No summary available"
                    // Map other properties as needed from nbaContext
                };
            }

            // Step 2: Evaluate relevance criteria
            var result = await EvaluateRelevanceCriteriaAsync(
                request.Action.RelevanceCriteria, 
                currentContext, 
                traceId);
            
            // Set initial validation method
            result.ValidationMethod = "RuleBased";

            // Step 3: Use LLM validation if enabled and rule-based validation is uncertain
            if (request.UseLLMValidation && _config.EnableLLMValidation && 
                result.ConfidenceScore < _config.MinimumConfidenceScore)
            {
                _logger.LogDebug("Using LLM validation for uncertain result, TraceId: {TraceId}", traceId);
                var llmResult = await ValidateWithLLMAsync(request.Action, currentContext, request.UserOverrides, traceId);
                
                // Combine results, giving preference to LLM if confidence is higher
                if (llmResult.ConfidenceScore > result.ConfidenceScore)
                {
                    result = llmResult;
                    result.ValidationMethod = "LLM";
                }
                else
                {
                    result.ValidationMethod = "RuleBased+LLM";
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
                "✅ Action relevance validation completed. Action {ActionId} is {Relevance} (confidence: {Confidence}, method: {ValidationMethod}), TraceId: {TraceId}",
                request.Action.Id, result.IsRelevant ? "RELEVANT" : "NOT RELEVANT", result.ConfidenceScore, result.ValidationMethod, traceId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "❌ Error validating action relevance for action {ActionId}, TraceId: {TraceId}", 
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
                ValidationMethod = "Error",
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
                ["evaluatedCriteria"] = criteria?.Keys.ToList() ?? new List<string>()
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
        Dictionary<string, object>? userOverrides = null,
        string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            _logger.LogDebug("🤖 Starting LLM-based relevance validation for action {ActionId}, TraceId: {TraceId}",
                action.Id, traceId);

            // Build prompt for LLM validation
            var systemPrompt = await _promptProvider.GetSystemPromptAsync(
                "ActionRelevanceValidator", 
                null); // Use default industry profile

            // Include userOverrides in the prompt for better decision-making
            var userOverridesSection = userOverrides?.SerializeForLLMPrompt() ?? "No user overrides specified.";

            var validationPrompt = $@"
Analyze whether the following scheduled action is still relevant given the current contact context and user preferences.

SCHEDULED ACTION:
- Type: {action.ActionType}
- Description: {action.Description}
- Scheduled At: {action.ScheduledAt:yyyy-MM-dd HH:mm}
- Execute At: {action.ExecuteAt:yyyy-MM-dd HH:mm}
- Relevance Criteria: {JsonSerializer.Serialize(action.RelevanceCriteria)}

CURRENT CONTACT CONTEXT:
{JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true })}

USER OVERRIDE PREFERENCES:
{userOverridesSection}

EVALUATION INSTRUCTIONS:
1. Determine if the action is still appropriate given the current context
2. Consider if the contact's situation has changed since the action was scheduled
3. Take into account the user's override preferences and constraints
4. Evaluate if executing this action would be helpful or potentially harmful
5. Provide a confidence score between 0.0 and 1.0
6. Explain your reasoning clearly, referencing user overrides where applicable

Respond in JSON format:
{{
""isRelevant"": true/false,
""confidenceScore"": 0.0-1.0,
""reason"": ""detailed explanation including how user overrides influenced the decision"",
""recommendedAction"": ""proceed/reschedule/modify/cancel"",
""alternativeActions"": [""action1"", ""action2""]
}}";

            // Log truncated prompt for debugging
            var promptPreview = validationPrompt.Length > 500 ? validationPrompt.Substring(0, 500) + "..." : validationPrompt;
            _logger.LogDebug("🔍 LLM prompt preview: {PromptPreview}, TraceId: {TraceId}", promptPreview, traceId);

            var aiResponse = await _aiFoundryService.ProcessAgentRequestAsync(
                systemPrompt, 
                validationPrompt, 
                traceId);

            // Parse LLM response
            var llmResult = ParseLLMValidationResponse(aiResponse ?? "", action.Id, traceId);
            llmResult.ValidationMethod = "LLM";
            
            _logger.LogDebug("✅ LLM validation completed with confidence {Confidence}, method: {ValidationMethod}, TraceId: {TraceId}",
                llmResult.ConfidenceScore, llmResult.ValidationMethod, traceId);

            return llmResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in LLM validation for action {ActionId}, TraceId: {TraceId}",
                action.Id, traceId);

            return new ActionRelevanceResult
            {
                ActionId = action.Id,
                IsRelevant = _config.DefaultActionOnUncertainty != "suppress",
                ConfidenceScore = 0.0,
                Reason = $"LLM validation failed: {ex.Message}",
                CheckedAt = DateTime.UtcNow,
                CheckedBy = "LLM-" + nameof(ActionRelevanceValidator),
                ValidationMethod = "LLM-Error",
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

    private void CleanupExpiredApprovals(object? sender, ElapsedEventArgs e)
    {
        lock (_approvalLock)
        {
            var now = DateTime.UtcNow;
            var expiredApprovals = _pendingApprovals.Where(kvp => kvp.Value.ExpiresAt < now).ToList();

            foreach (var expiredApproval in expiredApprovals)
            {
                expiredApproval.Value.Status = ApprovalStatus.Expired;
                _pendingApprovals.Remove(expiredApproval.Key);
                
                _logger.LogWarning("🕒 User approval request expired: {RequestId}, Action: {ActionType}, TraceId: {TraceId}",
                    expiredApproval.Key, expiredApproval.Value.Action.ActionType, expiredApproval.Value.TraceId);
            }

            _logger.LogDebug("🧹 Cleaned up {Count} expired approval requests", expiredApprovals.Count);
        }
    }

    public async Task<bool> RequiresUserApprovalAsync(
        ScheduledAction action, 
        ActionRelevanceResult relevanceResult, 
        string userId, 
        string? traceId = null)
    {
        try
        {
            traceId ??= Guid.NewGuid().ToString();

            _logger.LogDebug("🔍 Evaluating user approval requirement for action {ActionId}, type {ActionType}, TraceId: {TraceId}",
                action.Id, action.ActionType, traceId);

            // Check operating mode
            switch (_config.OverrideMode)
            {
                case UserOverrideMode.AlwaysAsk:
                    _logger.LogDebug("✋ Always ask mode - approval required, TraceId: {TraceId}", traceId);
                    return true;

                case UserOverrideMode.NeverAsk:
                    _logger.LogDebug("🚀 Never ask mode - no approval required, TraceId: {TraceId}", traceId);
                    return false;

                case UserOverrideMode.RiskBased:
                    // Check if action type requires approval
                    if (_config.AlwaysRequireApprovalActions.Contains(action.ActionType))
                    {
                        _logger.LogDebug("⚠️ High-risk action type requires approval: {ActionType}, TraceId: {TraceId}",
                            action.ActionType, traceId);
                        return true;
                    }

                    if (_config.NeverRequireApprovalActions.Contains(action.ActionType))
                    {
                        _logger.LogDebug("✅ Safe action type - no approval required: {ActionType}, TraceId: {TraceId}",
                            action.ActionType, traceId);
                        return false;
                    }

                    // Check confidence threshold
                    if (relevanceResult.ConfidenceScore < _config.UserApprovalThreshold)
                    {
                        _logger.LogDebug("📊 Low confidence score requires approval: {Score} < {Threshold}, TraceId: {TraceId}",
                            relevanceResult.ConfidenceScore, _config.UserApprovalThreshold, traceId);
                        return true;
                    }

                    return false;

                case UserOverrideMode.LLMDecision:
                    // Get fresh context for LLM decision
                    var nbaContext = await _nbaContextService.GetNbaContextAsync(
                        action.ContactId, action.OrganizationId, Guid.Parse(action.ScheduledByAgentId));

                    var contactContext = new ContactContext
                    {
                        ContactId = action.ContactId,
                        OrganizationId = action.OrganizationId,
                        LastInteraction = nbaContext.RecentInteractions?.FirstOrDefault()?.Timestamp,
                        InteractionSummary = nbaContext.RollingSummary?.SummaryText ?? "No summary available"
                    };

                    return await LLMRecommendsApprovalAsync(action, relevanceResult, contactContext, traceId);

                default:
                    _logger.LogWarning("🤔 Unknown override mode: {Mode}, defaulting to require approval, TraceId: {TraceId}",
                        _config.OverrideMode, traceId);
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error evaluating user approval requirement for action {ActionId}, TraceId: {TraceId}",
                action.Id, traceId);
            return true; // Fail safe - require approval on error
        }
    }

    public async Task<UserApprovalRequest> CreateApprovalRequestAsync(
        ScheduledAction action, 
        ActionRelevanceResult relevanceResult, 
        string userId, 
        string reason,
        Dictionary<string, object> userOverrides,
        string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            var approvalRequest = new UserApprovalRequest
            {
                Action = action,
                RelevanceResult = relevanceResult,
                ApprovalReason = reason,
                UserId = userId,
                OriginalUserOverrides = userOverrides, // Store original userOverrides for audit trail
                ExpiresAt = DateTime.UtcNow.AddMinutes(_config.UserApprovalTimeoutMinutes),
                TraceId = traceId
            };

            // Get alternative actions
            var nbaContext = await _nbaContextService.GetNbaContextAsync(
                action.ContactId, action.OrganizationId, Guid.Parse(action.ScheduledByAgentId));

            var contactContext = new ContactContext
            {
                ContactId = action.ContactId,
                OrganizationId = action.OrganizationId,
                LastInteraction = nbaContext.RecentInteractions?.FirstOrDefault()?.Timestamp,
                InteractionSummary = nbaContext.RollingSummary?.SummaryText ?? "No summary available"
            };

            approvalRequest.AlternativeActions = await SuggestAlternativeActionsAsync(action, contactContext, traceId);

            // Store the approval request
            lock (_approvalLock)
            {
                _pendingApprovals[approvalRequest.RequestId] = approvalRequest;
            }

            _logger.LogInformation("📝 Created user approval request {RequestId} for action {ActionId} with userOverrides, expires at {ExpiresAt}, TraceId: {TraceId}",
                approvalRequest.RequestId, action.Id, approvalRequest.ExpiresAt, traceId);

            return approvalRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating approval request for action {ActionId}, TraceId: {TraceId}",
                action.Id, traceId);
            throw;
        }
    }

    public async Task<ScheduledAction?> ProcessApprovalResponseAsync(UserApprovalResponse response)
    {
        try
        {
            UserApprovalRequest? approvalRequest;
            
            lock (_approvalLock)
            {
                if (!_pendingApprovals.TryGetValue(response.RequestId, out approvalRequest))
                {
                    _logger.LogWarning("⚠️ Approval request not found: {RequestId}", response.RequestId);
                    return null;
                }

                // Remove from pending list
                _pendingApprovals.Remove(response.RequestId);
            }

            // Update approval request status
            approvalRequest.Status = response.Decision switch
            {
                ApprovalDecision.Approve => ApprovalStatus.Approved,
                ApprovalDecision.Reject => ApprovalStatus.Rejected,
                ApprovalDecision.Modify => ApprovalStatus.Modified,
                _ => ApprovalStatus.Pending
            };

            _logger.LogInformation("✅ Processed approval response {RequestId}: {Decision}, TraceId: {TraceId}",
                response.RequestId, response.Decision, approvalRequest.TraceId);

            // Handle the decision
            switch (response.Decision)
            {
                case ApprovalDecision.Approve:
                    // Apply bulk approval if requested
                    if (response.ApplyToSimilarActions && _config.EnableBulkApproval)
                    {
                        var bulkCount = await ApplyBulkApprovalAsync(response, response.UserId);
                        _logger.LogInformation("📋 Applied bulk approval to {Count} similar actions", bulkCount);
                    }
                    return approvalRequest.Action;

                case ApprovalDecision.Reject:
                    _logger.LogInformation("❌ Action rejected by user: {ActionId}, Reason: {Reason}",
                        approvalRequest.Action.Id, response.Reason);
                    return null;

                case ApprovalDecision.Modify:
                    if (response.SuggestedModifications != null)
                    {
                        // Apply modifications to the action
                        var modifiedAction = ApplyModifications(approvalRequest.Action, response.SuggestedModifications);
                        _logger.LogInformation("🔧 Action modified by user: {ActionId}", approvalRequest.Action.Id);
                        return modifiedAction;
                    }
                    return approvalRequest.Action;

                case ApprovalDecision.Defer:
                    // Reschedule for later
                    approvalRequest.Action.ExecuteAt = DateTime.UtcNow.AddHours(1);
                    _logger.LogInformation("⏰ Action deferred by user: {ActionId}", approvalRequest.Action.Id);
                    return approvalRequest.Action;

                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing approval response {RequestId}", response.RequestId);
            return null;
        }
    }

    public async Task<List<UserApprovalRequest>> GetPendingApprovalsAsync(string userId, bool includeExpired = false)
    {
        var now = DateTime.UtcNow;
        
        lock (_approvalLock)
        {
            return _pendingApprovals.Values
                .Where(request => request.UserId == userId)
                .Where(request => includeExpired || request.ExpiresAt > now)
                .Where(request => request.Status == ApprovalStatus.Pending)
                .OrderBy(request => request.RequestedAt)
                .ToList();
        }
    }

    public async Task<bool> LLMRecommendsApprovalAsync(
        ScheduledAction action, 
        ActionRelevanceResult relevanceResult, 
        ContactContext context, 
        string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            var systemPrompt = @"You are an AI assistant helping determine if a scheduled action requires human approval.
Consider factors like:
- Action sensitivity and potential impact
- Confidence level of the relevance assessment
- Contact context and relationship status
- Risk of automation errors
- Industry compliance requirements

Respond with JSON: { ""requiresApproval"": true/false, ""reason"": ""explanation"" }";

            var userPrompt = $@"
Evaluate if this action requires human approval:

ACTION:
- Type: {action.ActionType}
- Description: {action.Description}
- Priority: {action.Priority}

RELEVANCE ASSESSMENT:
- Is Relevant: {relevanceResult.IsRelevant}
- Confidence: {relevanceResult.ConfidenceScore:F2}
- Reason: {relevanceResult.Reason}

CONTACT CONTEXT:
- Last Interaction: {context.LastInteraction}
- Summary: {context.InteractionSummary}

Should this action require human approval before execution?";

            var llmResponse = await _aiFoundryService.ProcessAgentRequestAsync(systemPrompt, userPrompt, traceId);
            var result = ParseLLMApprovalDecision(llmResponse, traceId);

            _logger.LogDebug("🤖 LLM approval recommendation: {RequiresApproval}, Reason: {Reason}, TraceId: {TraceId}",
                result.requiresApproval, result.reason, traceId);

            return result.requiresApproval;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting LLM approval recommendation for action {ActionId}, TraceId: {TraceId}",
                action.Id, traceId);
            return true; // Fail safe - require approval on error
        }
    }

    public async Task<int> ApplyBulkApprovalAsync(UserApprovalResponse response, string userId)
    {
        try
        {
            var approvedCount = 0;
            var similarActions = new List<UserApprovalRequest>();

            lock (_approvalLock)
            {
                // Find similar pending actions for the same user
                var originalRequest = _pendingApprovals.Values.FirstOrDefault(r => r.RequestId == response.RequestId);
                if (originalRequest == null) return 0;

                similarActions = _pendingApprovals.Values
                    .Where(request => request.UserId == userId)
                    .Where(request => request.RequestId != response.RequestId)
                    .Where(request => request.Status == ApprovalStatus.Pending)
                    .Where(request => IsSimilarAction(request.Action, originalRequest.Action))
                    .ToList();

                // Apply the same decision to similar actions
                foreach (var similarRequest in similarActions)
                {
                    similarRequest.Status = response.Decision switch
                    {
                        ApprovalDecision.Approve => ApprovalStatus.Approved,
                        ApprovalDecision.Reject => ApprovalStatus.Rejected,
                        _ => ApprovalStatus.Pending
                    };

                    if (response.Decision == ApprovalDecision.Approve || response.Decision == ApprovalDecision.Reject)
                    {
                        _pendingApprovals.Remove(similarRequest.RequestId);
                        approvedCount++;
                    }
                }
            }

            _logger.LogInformation("📋 Bulk approval applied to {Count} similar actions for user {UserId}",
                approvedCount, userId);

            return approvedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error applying bulk approval for user {UserId}", userId);
            return 0;
        }
    }

    #region Private Helper Methods

    private (bool requiresApproval, string reason) ParseLLMApprovalDecision(string llmResponse, string traceId)
    {
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(llmResponse);
            
            return (
                jsonResponse.GetProperty("requiresApproval").GetBoolean(),
                jsonResponse.GetProperty("reason").GetString() ?? "LLM recommendation"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error parsing LLM approval decision, TraceId: {TraceId}", traceId);
            return (true, "Failed to parse LLM response - requiring approval for safety");
        }
    }

    private ScheduledAction ApplyModifications(ScheduledAction originalAction, Dictionary<string, object> modifications)
    {
        var modifiedAction = new ScheduledAction
        {
            Id = originalAction.Id,
            ActionType = originalAction.ActionType,
            Description = originalAction.Description,
            ContactId = originalAction.ContactId,
            OrganizationId = originalAction.OrganizationId,
            ScheduledByAgentId = originalAction.ScheduledByAgentId,
            ExecuteAt = originalAction.ExecuteAt,
            Parameters = new Dictionary<string, object>(originalAction.Parameters),
            RelevanceCriteria = new Dictionary<string, object>(originalAction.RelevanceCriteria),
            Priority = originalAction.Priority,
            TraceId = originalAction.TraceId
        };

        // Apply modifications
        foreach (var modification in modifications)
        {
            switch (modification.Key.ToLower())
            {
                case "description":
                    modifiedAction.Description = modification.Value.ToString() ?? modifiedAction.Description;
                    break;
                case "executeat":
                    if (DateTime.TryParse(modification.Value.ToString(), out var newExecuteAt))
                        modifiedAction.ExecuteAt = newExecuteAt;
                    break;
                case "priority":
                    if (int.TryParse(modification.Value.ToString(), out var newPriority) && 
                        Enum.IsDefined(typeof(UrgencyLevel), newPriority))
                        modifiedAction.Priority = (UrgencyLevel)newPriority;
                    break;
                default:
                    // Add to parameters
                    modifiedAction.Parameters[modification.Key] = modification.Value;
                    break;
            }
        }

        return modifiedAction;
    }

    private bool IsSimilarAction(ScheduledAction action1, ScheduledAction action2)
    {
        return action1.ActionType == action2.ActionType &&
               action1.ContactId == action2.ContactId &&
               Math.Abs((action1.ExecuteAt - action2.ExecuteAt).TotalHours) < 24; // Within 24 hours
    }

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
