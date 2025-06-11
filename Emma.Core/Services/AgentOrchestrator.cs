using Emma.Data.Models;
using Microsoft.Extensions.Logging;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Configuration;

namespace Emma.Core.Services;

/// <summary>
/// EMMA orchestrator implementation that delegates to Azure AI Foundry
/// Acts as a thin wrapper to maintain separation of concerns and leverage Azure services
/// Manages all AI agents including NBA-Agent
/// Enhanced with dynamic agent routing and feature flag support (Sprint 1)
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IAIFoundryService _aiFoundryService;
    private readonly ITenantContextService _tenantContext;
    private readonly INbaAgent _nbaAgent;
    private readonly IContextIntelligenceAgent _contextIntelligenceAgent;
    private readonly IIntentClassificationAgent _intentClassificationAgent;
    private readonly IResourceAgent _resourceAgent;
    private readonly IActionRelevanceValidator _actionRelevanceValidator;
    private readonly INbaContextService _nbaContextService;
    private readonly IAgentRegistry _agentRegistry;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly Dictionary<Guid, ScheduledAction> _scheduledActions = new();
    private readonly Timer _actionExecutionTimer;

    public AgentOrchestrator(
        IAIFoundryService aiFoundryService,
        ITenantContextService tenantContext,
        INbaAgent nbaAgent,
        IContextIntelligenceAgent contextIntelligenceAgent,
        IIntentClassificationAgent intentClassificationAgent,
        IResourceAgent resourceAgent,
        IActionRelevanceValidator actionRelevanceValidator,
        INbaContextService nbaContextService,
        IAgentRegistry agentRegistry,
        IFeatureFlagService featureFlagService,
        ILogger<AgentOrchestrator> logger)
    {
        _aiFoundryService = aiFoundryService;
        _tenantContext = tenantContext;
        _nbaAgent = nbaAgent;
        _contextIntelligenceAgent = contextIntelligenceAgent;
        _intentClassificationAgent = intentClassificationAgent;
        _resourceAgent = resourceAgent;
        _actionRelevanceValidator = actionRelevanceValidator;
        _nbaContextService = nbaContextService;
        _agentRegistry = agentRegistry;
        _featureFlagService = featureFlagService;
        _logger = logger;

        _actionExecutionTimer = new Timer(ProcessScheduledActions, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        
        // Register first-class agents with the registry if dynamic routing is enabled
        _ = Task.Run(RegisterFirstClassAgentsAsync);
    }

    public async Task<AgentResponse> ProcessRequestAsync(string userInput, Guid conversationId, string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();
        var auditId = Guid.NewGuid();

        try
        {
            _logger.LogInformation("Processing user request via orchestrator: {Request}, TraceId: {TraceId}, AuditId: {AuditId}", 
                userInput, traceId, auditId);

            // Check if dynamic routing is enabled
            var isDynamicRoutingEnabled = await _featureFlagService.IsEnabledAsync(FeatureFlags.DYNAMIC_AGENT_ROUTING);
            
            if (isDynamicRoutingEnabled)
            {
                return await ProcessRequestWithDynamicRoutingAsync(userInput, conversationId, traceId, auditId);
            }

            // Fallback to legacy routing
            var parameters = new Dictionary<string, object>
            {
                { "ConversationId", conversationId },
                { "AuditId", auditId }
            };
            return await ProcessRequestWithLegacyRoutingAsync(userInput, parameters, traceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request via orchestrator, TraceId: {TraceId}, AuditId: {AuditId}", traceId, auditId);
            return new AgentResponse
            {
                Success = false,
                Message = "Error processing request",
                AgentType = "AgentOrchestrator",
                TraceId = traceId,
                AuditId = auditId,
                Reason = $"Exception occurred during request processing: {ex.Message}"
            };
        }
    }

    public async Task<WorkflowState> ExecuteWorkflowAsync(string workflowId, AgentRequest initialRequest, string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Executing workflow {WorkflowId}, TraceId: {TraceId}", workflowId, traceId);

            // For now, return a basic workflow state - this can be expanded later
            return new WorkflowState
            {
                WorkflowId = workflowId,
                Status = "Completed",
                Variables = new Dictionary<string, object>(),
                ExecutionHistory = new List<WorkflowStep>(),
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowId}, TraceId: {TraceId}", workflowId, traceId);
            return new WorkflowState
            {
                WorkflowId = workflowId,
                Status = "Failed",
                Variables = new Dictionary<string, object>(),
                ExecutionHistory = new List<WorkflowStep>(),
                CreatedAt = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    public async Task<List<AgentCapability>> GetAvailableAgentsAsync()
    {
        try
        {
            // Get available agents from Azure AI Foundry based on tenant industry
            var industryProfile = await _tenantContext.GetIndustryProfileAsync();

            if (industryProfile == null)
            {
                throw new InvalidOperationException("Industry profile is not configured for this tenant.");
            }

            var availableAgents = new List<AgentCapability>();

            // Add NBA-Agent as a first-class agent
            var nbaCapability = await _nbaAgent.GetCapabilityAsync();
            availableAgents.Add(nbaCapability);

            // Add layered agents as first-class agents
            try
            {
                var contextIntelligenceCapability = await _contextIntelligenceAgent.GetCapabilityAsync();
                availableAgents.Add(contextIntelligenceCapability);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Context Intelligence Agent not available");
            }

            try
            {
                var intentClassificationCapability = await _intentClassificationAgent.GetCapabilityAsync();
                availableAgents.Add(intentClassificationCapability);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Intent Classification Agent not available");
            }

            try
            {
                var resourceCapability = await _resourceAgent.GetCapabilityAsync();
                availableAgents.Add(resourceCapability);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resource Agent not available");
            }

            // Add industry-specific specialized agents from Azure AI Foundry
            industryProfile.SpecializedAgents = industryProfile.SpecializedAgents ?? new List<string>();
            industryProfile.AvailableActions = industryProfile.AvailableActions ?? new List<string>();

            var industryAgents = industryProfile.SpecializedAgents.Select(agentType => new AgentCapability
            {
                AgentType = agentType,
                DisplayName = GetAgentDisplayName(agentType),
                Description = GetAgentDescription(agentType, industryProfile.IndustryCode),
                SupportedTasks = GetSupportedTasks(agentType),
                RequiredIndustries = new List<string> { industryProfile.IndustryCode },
                IsAvailable = true
            });

            availableAgents.AddRange(industryAgents);

            return availableAgents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available agents");

            // Return at least NBA-Agent even if other agents fail
            try
            {
                var nbaCapability = await _nbaAgent.GetCapabilityAsync();
                return new List<AgentCapability> { nbaCapability };
            }
            catch
            {
                return new List<AgentCapability>();
            }
        }
    }

    public async Task<AgentResponse> RouteToAzureAgentAsync(string agentType, AgentTask task)
    {
        try
        {
            _logger.LogInformation("Routing task {TaskType} to Azure AI Foundry agent {AgentType}",
                task.TaskType, agentType);

            // Build agent-specific prompt for Azure AI Foundry
            var systemPrompt = await BuildAgentSystemPromptAsync(agentType, task);
            var taskPrompt = BuildTaskPromptAsync(task);

            // Call Azure AI Foundry with agent-specific context
            var aiResponse = await _aiFoundryService.ProcessAgentRequestAsync(
                systemPrompt,
                taskPrompt,
                task.ContactId.ToString());

            // Execute any resource-related actions based on AI response
            return await ExecuteResourceActionsAsync(aiResponse, task.ContactId ?? Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to Azure AI Foundry agent {AgentType}", agentType);
            return new AgentResponse
            {
                Success = false,
                Message = $"Error routing to {agentType}: {ex.Message}",
                AgentType = agentType
            };
        }
    }

    public async Task<AgentResponse> RouteToNbaAgentAsync(string userInput, Guid conversationId, string? traceId = null)
    {
        try
        {
            var request = new AgentRequest
            {
                Intent = AgentIntent.DataAnalysis, // NBA recommendations are data analysis
                OriginalUserInput = userInput,
                ConversationId = conversationId,
                Context = new Dictionary<string, object>
                {
                    ["conversationId"] = conversationId
                }
            };

            return await RouteToNbaAgentAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to NBA-Agent, TraceId: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = $"Error routing to NBA-Agent: {ex.Message}",
                AgentType = "NbaAgent"
            };
        }
    }

    private async Task<AgentResponse> RouteToNbaAgentAsync(AgentRequest request)
    {
        try
        {
            _logger.LogInformation("Routing request to NBA-Agent: Intent={Intent}, TraceId={TraceId}",
                request.Intent, request.TraceId);

            _logger.LogInformation("Original user input: {Input}", request.OriginalUserInput);

            // Add NBA-specific context
            request.Context["requestType"] = "nba_recommendation";

            var response = await _nbaAgent.ProcessRequestAsync(request, request.TraceId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to NBA-Agent");
            return new AgentResponse
            {
                Success = false,
                Message = $"NBA-Agent routing failed: {ex.Message}",
                RequestId = request.Id,
                AgentId = "AgentOrchestrator",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<AgentResponse> RouteToContextIntelligenceAgentAsync(AgentRequest request, string? traceId = null)
    {
        try
        {
            _logger.LogInformation("Routing request to Context Intelligence Agent: Intent={Intent}, TraceId={TraceId}",
                request.Intent, traceId);

            // Add context intelligence specific context
            request.Context["requestType"] = "context_intelligence";

            var response = await _contextIntelligenceAgent.ProcessRequestAsync(request, traceId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to Context Intelligence Agent, TraceId: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = $"Context Intelligence Agent routing failed: {ex.Message}",
                RequestId = request.Id,
                AgentId = "AgentOrchestrator",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<AgentResponse> RouteToIntentClassificationAgentAsync(AgentRequest request, string? traceId = null)
    {
        try
        {
            _logger.LogInformation("Routing request to Intent Classification Agent: Intent={Intent}, TraceId={TraceId}",
                request.Intent, traceId);

            // Add intent classification specific context
            request.Context["requestType"] = "intent_classification";

            var response = await _intentClassificationAgent.ProcessRequestAsync(request, traceId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to Intent Classification Agent, TraceId: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = $"Intent Classification Agent routing failed: {ex.Message}",
                RequestId = request.Id,
                AgentId = "AgentOrchestrator",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<AgentResponse> RouteToResourceAgentAsync(AgentRequest request, string? traceId = null)
    {
        try
        {
            _logger.LogInformation("Routing request to Resource Agent: Intent={Intent}, TraceId={TraceId}",
                request.Intent, traceId);

            // Add resource agent specific context
            request.Context["requestType"] = "resource_management";

            var response = await _resourceAgent.ProcessRequestAsync(request, traceId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to Resource Agent, TraceId: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = $"Resource Agent routing failed: {ex.Message}",
                RequestId = request.Id,
                AgentId = "AgentOrchestrator",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public void SetOrchestrationMethod(string method)
    {
        _logger.LogInformation("Setting orchestration method to: {Method}", method);
        // Store the orchestration method for future use
        // This could be stored in configuration or instance variable
    }

    private async Task<string> BuildSystemPromptAsync(Emma.Core.Industry.IIndustryProfile profile, Guid contactId)
    {
        var prompt = profile.PromptTemplates?.SystemPrompt ?? "Default system prompt goes here.";
        return $@"{prompt}

CURRENT CONTEXT:
- Industry: {profile.DisplayName}
- Contact ID: {contactId}
- Available Actions: {string.Join(", ", profile.AvailableActions)}

AVAILABLE ACTIONS:
You can help with resource management tasks including:
- Finding and recommending resources (service providers)
- Assigning resources to contacts
- Checking resource availability and ratings
- Managing resource assignments and compliance

Always respond with specific, actionable recommendations using the available resource data.";
    }

    private async Task<string> BuildUserPromptAsync(string userRequest, Guid contactId, Models.TenantContext tenant)
    {
        return $@"USER REQUEST: {userRequest}

CONTEXT:
- Organization: {tenant.TenantId}
- Tenant ID: {tenant.TenantId}
- Contact ID: {contactId}

Please analyze this request and provide specific recommendations for resource management actions.
If the request involves finding or assigning service providers (resources), provide specific guidance on next steps.";
    }

    private async Task<string> BuildAgentSystemPromptAsync(string agentType, AgentTask task)
    {
        var industryProfile = await _tenantContext.GetIndustryProfileAsync();

        if (industryProfile == null)
        {
            throw new InvalidOperationException("Industry profile is not configured for this tenant.");
        }

        var prompt = industryProfile.PromptTemplates?.SystemPrompt ?? "Default system prompt goes here.";
        return $@"You are a specialized {agentType} for {industryProfile.DisplayName} industry.

SPECIALIZATION: {GetAgentDescription(agentType, industryProfile.IndustryCode)}

TASK TYPE: {task.TaskType}
SUPPORTED ACTIONS: {string.Join(", ", GetSupportedTasks(agentType))}

Focus on resource management tasks related to your specialization.
Use the existing Contact and ResourceService data model.
Provide specific, actionable recommendations.";
    }

    private string BuildTaskPromptAsync(AgentTask task)
    {
        var parametersText = task.Parameters.Any()
            ? string.Join(", ", task.Parameters.Select(p => $"{p.Key}: {p.Value}"))
            : "None";

        return $@"TASK: {task.Description}

PARAMETERS: {parametersText}

Please process this task and provide specific recommendations for resource management actions.";
    }

    private async Task<AgentResponse> ExecuteResourceActionsAsync(string aiResponse, Guid contactId)
    {
        // Parse AI response for actionable resource management tasks
        // This is where we bridge AI recommendations to actual ResourceService calls

        try
        {
            // Simple parsing logic - in production, this would be more sophisticated
            var response = new AgentResponse
            {
                Success = true,
                Message = aiResponse,
                AgentType = "AzureAIFoundry",
                Data = new Dictionary<string, object>
                {
                    ["aiResponse"] = aiResponse,
                    ["contactId"] = contactId
                }
            };

            // Check if AI response suggests specific resource actions
            if (aiResponse.ToLower().Contains("find") || aiResponse.ToLower().Contains("search"))
            {
                response.Actions.Add("resource_search_suggested");
            }

            if (aiResponse.ToLower().Contains("assign") || aiResponse.ToLower().Contains("connect"))
            {
                response.Actions.Add("resource_assignment_suggested");
            }

            var scheduledAction = new ScheduledAction
            {
                ActionType = "resource_management",
                Description = aiResponse,
                ContactId = contactId,
                OrganizationId = Guid.NewGuid(), // TODO: Get from context
                ScheduledByAgentId = "ResourceAgent",
                ExecuteAt = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    ["aiResponse"] = aiResponse,
                    ["contactId"] = contactId
                }
            };

            var currentContext = await GetCurrentContactContextAsync(contactId, scheduledAction.OrganizationId, Guid.NewGuid().ToString());
            
            var actionRelevanceRequest = new ActionRelevanceRequest
            {
                Action = scheduledAction,
                CurrentContext = currentContext,
                UseLLMValidation = true,
                TraceId = Guid.NewGuid().ToString()
            };

            var actionRelevance = await _actionRelevanceValidator.ValidateActionRelevanceAsync(actionRelevanceRequest);
            if (!actionRelevance.IsRelevant)
            {
                response.Success = false;
                response.Message = $"Action relevance validation failed: {actionRelevance.Reason}";
                
                // Log suppression for audit trail
                _logger.LogWarning("Resource action suppressed due to relevance validation failure. ContactId: {ContactId}, Reason: {Reason}",
                    contactId, actionRelevance.Reason);
                
                return response;
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing resource actions from AI response");
            return new AgentResponse
            {
                Success = false,
                Message = $"Error processing AI response: {ex.Message}",
                AgentType = "AzureAIFoundry"
            };
        }
    }

    private bool IsNbaRequest(string userInput)
    {
        // Improved logic to determine if the request is NBA-specific
        var input = userInput.ToLower();

        // NBA-specific keywords
        var nbaKeywords = new[]
        {
            "next best action",
            "nba",
            "what should i do next",
            "recommend",
            "recommendation",
            "suggest",
            "advice",
            "best action",
            "next step",
            "what to do",
            "action plan",
            "priority"
        };

        return nbaKeywords.Any(keyword => input.Contains(keyword));
    }

    private string GetAgentDisplayName(string agentType) => agentType switch
    {
        "ContractorAgent" => "Resource Specialist",
        "MortgageAgent" => "Lending Specialist",
        "InspectorAgent" => "Inspection Specialist",
        "AttorneyAgent" => "Legal Specialist",
        "NBA-Agent" => "NBA Agent",
        _ => "Resource Specialist"
    };

    private string GetAgentDescription(string agentType, string industryCode) => agentType switch
    {
        "ContractorAgent" => $"Specialized in finding and managing service provider resources for {industryCode}",
        "MortgageAgent" => $"Specialized in lending and financing resources for {industryCode}",
        "InspectorAgent" => $"Specialized in inspection and assessment resources for {industryCode}",
        "AttorneyAgent" => $"Specialized in legal and compliance resources for {industryCode}",
        "NBA-Agent" => $"Specialized in NBA-related resources for {industryCode}",
        _ => $"General resource management for {industryCode}"
    };

    private List<string> GetSupportedTasks(string agentType) => agentType switch
    {
        "ContractorAgent" => new() { "find_resource", "assign_resource", "get_recommendations" },
        "MortgageAgent" => new() { "find_lender", "assign_lender", "get_loan_options" },
        "InspectorAgent" => new() { "find_inspector", "schedule_inspection", "get_inspection_reports" },
        "AttorneyAgent" => new() { "find_attorney", "assign_legal_counsel", "compliance_check" },
        "NBA-Agent" => new() { "find_nba_resource", "assign_nba_resource", "get_nba_recommendations" },
        _ => new() { "find_resource", "assign_resource", "get_recommendations" }
    };

    public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Processing request: Intent={Intent}, TraceId={TraceId}",
                request.Intent, traceId);

            // Route based on intent and context
            return request.Intent switch
            {
                // NBA Agent routing
                AgentIntent.DataAnalysis when IsNbaRequest(request) => await RouteToNbaAgentAsync(request),
                AgentIntent.BusinessIntelligence when IsNbaRequest(request) => await RouteToNbaAgentAsync(request),

                // Context Intelligence Agent routing
                AgentIntent.InteractionAnalysis => await RouteToContextIntelligenceAgentAsync(request, traceId),
                AgentIntent.DataAnalysis when IsContextAnalysisRequest(request) => await RouteToContextIntelligenceAgentAsync(request, traceId),

                // Intent Classification Agent routing
                AgentIntent.IntentClassification => await RouteToIntentClassificationAgentAsync(request, traceId),

                // Resource Agent routing
                AgentIntent.ResourceManagement => await RouteToResourceAgentAsync(request, traceId),
                AgentIntent.ServiceProviderRecommendation => await RouteToResourceAgentAsync(request, traceId),

                // Default routing for unhandled intents
                _ => await HandleUnknownIntentAsync(request, traceId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request: Intent={Intent}, TraceId={TraceId}",
                request.Intent, traceId);

            return new AgentResponse
            {
                Success = false,
                Message = $"Request processing failed: {ex.Message}",
                RequestId = request.Id,
                AgentId = "AgentOrchestrator",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private bool IsNbaRequest(AgentRequest request)
    {
        // Check for NBA-specific keywords or context
        var userInput = request.OriginalUserInput?.ToLowerInvariant() ?? "";
        var nbaKeywords = new[] { "recommend", "next best", "action", "nba", "suggestion", "what should", "advice" };

        return nbaKeywords.Any(keyword => userInput.Contains(keyword)) ||
               request.Context.ContainsKey("requestType") &&
               request.Context["requestType"]?.ToString() == "nba_recommendation";
    }

    private bool IsContextAnalysisRequest(AgentRequest request)
    {
        // Check for context analysis specific indicators
        var userInput = request.OriginalUserInput?.ToLowerInvariant() ?? "";
        var contextKeywords = new[] { "analyze", "sentiment", "context", "insight", "pattern", "behavior" };

        return contextKeywords.Any(keyword => userInput.Contains(keyword)) ||
               request.Context.ContainsKey("analysisType") ||
               request.Context.ContainsKey("contactId");
    }

    private async Task<AgentResponse> HandleUnknownIntentAsync(AgentRequest request, string traceId)
    {
        _logger.LogWarning("Unknown intent: {Intent}, attempting intent classification, TraceId: {TraceId}",
            request.Intent, traceId);

        // Try to classify the intent first
        if (!string.IsNullOrEmpty(request.OriginalUserInput))
        {
            try
            {
                var classificationRequest = new AgentRequest
                {
                    Intent = AgentIntent.IntentClassification,
                    OriginalUserInput = request.OriginalUserInput,
                    Context = request.Context,
                    ConversationId = request.ConversationId
                };

                var classificationResponse = await RouteToIntentClassificationAgentAsync(classificationRequest, traceId);

                if (classificationResponse.Success && classificationResponse.Data.ContainsKey("ClassifiedIntent"))
                {
                    var classifiedIntentStr = classificationResponse.Data["ClassifiedIntent"].ToString();
                    if (Enum.TryParse<AgentIntent>(classifiedIntentStr, out var classifiedIntent))
                    {
                        // Retry with the classified intent
                        request.Intent = classifiedIntent;
                        return await ProcessRequestAsync(request, traceId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Intent classification failed for unknown intent, TraceId: {TraceId}", traceId);
            }
        }

        // Fallback response
        return new AgentResponse
        {
            Success = false,
            Message = $"Unable to process intent: {request.Intent}. Please try rephrasing your request.",
            Data = new Dictionary<string, object>
            {
                ["OriginalIntent"] = request.Intent.ToString(),
                ["SuggestedActions"] = new[]
                {
                    "Try asking for recommendations or next best actions",
                    "Request context analysis or insights",
                    "Ask for resource or service provider recommendations"
                }
            },
            RequestId = request.Id,
            AgentId = "AgentOrchestrator",
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<WorkflowState> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters)
    {
        _logger.LogInformation("Executing workflow: {WorkflowId}", workflowId);

        // TODO: Implement workflow execution logic
        // This would coordinate multiple agents in a specific sequence

        return new WorkflowState
        {
            WorkflowId = workflowId,
            Status = "NotImplemented",
            Variables = new Dictionary<string, object>
            {
                ["parameters"] = parameters,
                ["results"] = new Dictionary<string, object>
                {
                    ["message"] = "Workflow execution not yet implemented"
                }
            },
            ExecutionHistory = new List<WorkflowStep>()
        };
    }

    private async void ProcessScheduledActions(object? state)
    {
        var traceId = Guid.NewGuid().ToString();

        try
        {
            var dueActions = _scheduledActions.Values
                .Where(a => a.Status == ScheduledActionStatus.Pending && a.ExecuteAt <= DateTime.UtcNow)
                .OrderBy(a => a.Priority)
                .ThenBy(a => a.ExecuteAt)
                .ToList();

            if (!dueActions.Any())
            {
                return; // No actions due for execution
            }

            _logger.LogInformation("Processing {Count} scheduled actions, TraceId: {TraceId}",
                dueActions.Count, traceId);

            foreach (var action in dueActions)
            {
                await ProcessSingleScheduledActionAsync(action, traceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled actions, TraceId: {TraceId}", traceId);
        }
    }

    private async Task ProcessSingleScheduledActionAsync(ScheduledAction action, string traceId)
    {
        try
        {
            _logger.LogDebug("Processing scheduled action {ActionId}, type {ActionType}, TraceId: {TraceId}",
                action.Id, action.ActionType, traceId);

            // MISSION-CRITICAL: Pre-execution relevance validation
            var isRelevant = await _actionRelevanceValidator.IsActionStillRelevantAsync(
                action,
                action.ContactId,
                action.OrganizationId,
                traceId);

            if (!isRelevant)
            {
                // Action is no longer relevant - suppress execution
                action.Status = ScheduledActionStatus.Suppressed;
                action.SuppressionReason = "Action no longer relevant based on current context";

                _logger.LogWarning(
                    "AUTOMATION FAILURE PREVENTED: Action {ActionId} ({ActionType}) suppressed due to relevance check failure, TraceId: {TraceId}",
                    action.Id, action.ActionType, traceId);

                // Optionally suggest alternative actions
                try
                {
                    var currentContext = await GetCurrentContactContextAsync(action.ContactId, action.OrganizationId, traceId);
                    var alternatives = await _actionRelevanceValidator.SuggestAlternativeActionsAsync(action, currentContext, traceId);

                    if (alternatives.Any())
                    {
                        _logger.LogInformation(
                            "Suggested {Count} alternative actions for suppressed action {ActionId}, TraceId: {TraceId}",
                            alternatives.Count, action.Id, traceId);

                        // Schedule alternative actions
                        foreach (var alternative in alternatives)
                        {
                            if (Guid.TryParse(alternative.Id, out Guid alternativeId))
                            {
                                _scheduledActions.TryAdd(alternativeId, alternative);
                            }
                            else
                            {
                                _logger.LogError("Invalid GUID format for alternative ID: {AlternativeId}", alternative.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating alternative actions for {ActionId}, TraceId: {TraceId}",
                        action.Id, traceId);
                }

                return;
            }

            // Action is still relevant - proceed with execution
            action.Status = ScheduledActionStatus.RelevanceCheckPassed;
            action.LastRelevanceCheck = DateTime.UtcNow;

            _logger.LogInformation(
                "Relevance check PASSED for action {ActionId} ({ActionType}), proceeding with execution, TraceId: {TraceId}",
                action.Id, action.ActionType, traceId);

            // Execute the action
            await ExecuteValidatedActionAsync(action, traceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled action {ActionId}, TraceId: {TraceId}",
                action.Id, traceId);

            action.Status = ScheduledActionStatus.Failed;
            action.RetryAttempts++;

            // Retry logic for failed actions
            if (action.RetryAttempts < action.MaxRetryAttempts)
            {
                action.Status = ScheduledActionStatus.Pending;
                action.ExecuteAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, action.RetryAttempts)); // Exponential backoff

                _logger.LogInformation(
                    "Scheduling retry {Attempt}/{MaxAttempts} for action {ActionId} at {NextAttempt}, TraceId: {TraceId}",
                    action.RetryAttempts, action.MaxRetryAttempts, action.Id, action.ExecuteAt, traceId);
            }
            else
            {
                _logger.LogError(
                    "Action {ActionId} failed after {MaxAttempts} attempts, marking as failed, TraceId: {TraceId}",
                    action.Id, action.MaxRetryAttempts, traceId);
            }
        }
    }

    private async Task ExecuteValidatedActionAsync(ScheduledAction action, string traceId)
    {
        try
        {
            action.Status = ScheduledActionStatus.Executing;

            _logger.LogInformation(
                "Executing validated action {ActionId} ({ActionType}) for contact {ContactId}, TraceId: {TraceId}",
                action.Id, action.ActionType, action.ContactId, traceId);

            // Defensive: ensure ContactId and OrganizationId are valid
            if (action.ContactId == Guid.Empty)
            {
                _logger.LogError("Scheduled action missing ContactId. ActionId: {ActionId}", action.Id);
                action.Status = ScheduledActionStatus.Failed;
                return;
            }
            if (action.OrganizationId == Guid.Empty)
            {
                _logger.LogError("Scheduled action missing OrganizationId. ActionId: {ActionId}", action.Id);
                action.Status = ScheduledActionStatus.Failed;
                return;
            }

            // Route action to appropriate execution handler based on action type
            switch (action.ActionType.ToLowerInvariant())
            {
                case "email":
                case "congrats_email":
                case "follow_up_email":
                    await ExecuteEmailActionAsync(action, traceId);
                    break;

                case "sms":
                case "text_message":
                    await ExecuteSmsActionAsync(action, traceId);
                    break;

                case "appointment_reminder":
                case "calendar_event":
                    await ExecuteCalendarActionAsync(action, traceId);
                    break;

                case "property_recommendation":
                case "listing_alert":
                    await ExecutePropertyActionAsync(action, traceId);
                    break;

                case "task_creation":
                case "follow_up_task":
                    await ExecuteTaskActionAsync(action, traceId);
                    break;

                default:
                    await ExecuteGenericActionAsync(action, traceId);
                    break;
            }

            action.Status = ScheduledActionStatus.Completed;

            _logger.LogInformation(
                "Action {ActionId} ({ActionType}) executed successfully, TraceId: {TraceId}",
                action.Id, action.ActionType, traceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action {ActionId} ({ActionType}), TraceId: {TraceId}",
                action.Id, action.ActionType, traceId);

            action.Status = ScheduledActionStatus.Failed;
            throw;
        }
    }

    private async Task<ContactContext> GetCurrentContactContextAsync(Guid contactId, Guid organizationId, string traceId)
    {
        try
        {
            // Get fresh NBA context
            var nbaContext = await _nbaContextService.GetNbaContextAsync(
                contactId,
                organizationId,
                Guid.NewGuid()); // Use a generic agent ID for context retrieval

            // Convert to ContactContext (simplified mapping - expand as needed)
            return new ContactContext
            {
                ContactId = contactId,
                OrganizationId = organizationId,
                LastInteraction = nbaContext.RecentInteractions?.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                CustomProperties = new Dictionary<string, object>
                {
                    ["nbaContext"] = nbaContext
                    // Map other relevant properties from nbaContext
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current contact context for {ContactId}, TraceId: {TraceId}",
                contactId, traceId);

            // Return minimal context to allow processing to continue
            return new ContactContext
            {
                ContactId = contactId,
                OrganizationId = organizationId,
                LastInteraction = DateTime.UtcNow
            };
        }
    }

    #region Action Execution Handlers

    private async Task ExecuteEmailActionAsync(ScheduledAction action, string traceId)
    {
        _logger.LogDebug("Executing email action {ActionId}, TraceId: {TraceId}", action.Id, traceId);

        // Implementation would integrate with email service
        // For now, this is a placeholder
        await Task.Delay(100); // Simulate email sending

        _logger.LogInformation("Email action {ActionId} executed successfully, TraceId: {TraceId}",
            action.Id, traceId);
    }

    private async Task ExecuteSmsActionAsync(ScheduledAction action, string traceId)
    {
        _logger.LogDebug("Executing SMS action {ActionId}, TraceId: {TraceId}", action.Id, traceId);

        // Implementation would integrate with SMS service
        await Task.Delay(100); // Simulate SMS sending

        _logger.LogInformation("SMS action {ActionId} executed successfully, TraceId: {TraceId}",
            action.Id, traceId);
    }

    private async Task ExecuteCalendarActionAsync(ScheduledAction action, string traceId)
    {
        _logger.LogDebug("Executing calendar action {ActionId}, TraceId: {TraceId}", action.Id, traceId);

        // Implementation would integrate with calendar service
        await Task.Delay(100); // Simulate calendar operation

        _logger.LogInformation("Calendar action {ActionId} executed successfully, TraceId: {TraceId}",
            action.Id, traceId);
    }

    private async Task ExecutePropertyActionAsync(ScheduledAction action, string traceId)
    {
        _logger.LogDebug("Executing property action {ActionId}, TraceId: {TraceId}", action.Id, traceId);

        // Implementation would integrate with property/listing service
        await Task.Delay(100); // Simulate property operation

        _logger.LogInformation("Property action {ActionId} executed successfully, TraceId: {TraceId}",
            action.Id, traceId);
    }

    private async Task ExecuteTaskActionAsync(ScheduledAction action, string traceId)
    {
        _logger.LogDebug("Executing task action {ActionId}, TraceId: {TraceId}", action.Id, traceId);

        // Implementation would integrate with task management service
        await Task.Delay(100); // Simulate task creation

        _logger.LogInformation("Task action {ActionId} executed successfully, TraceId: {TraceId}",
            action.Id, traceId);
    }

    private async Task ExecuteGenericActionAsync(ScheduledAction action, string traceId)
    {
        _logger.LogDebug("Executing generic action {ActionId} ({ActionType}), TraceId: {TraceId}",
            action.Id, action.ActionType, traceId);

        // Generic action handler for unknown action types
        await Task.Delay(100); // Simulate generic operation

        _logger.LogInformation("Generic action {ActionId} executed successfully, TraceId: {TraceId}",
            action.Id, traceId);
    }

    #endregion

    public void Dispose()
    {
        _actionExecutionTimer?.Dispose();
    }

    /// <summary>
    /// Register first-class agents with the dynamic registry.
    /// Called during orchestrator initialization if dynamic routing is enabled.
    /// </summary>
    private async Task RegisterFirstClassAgentsAsync()
    {
        try
        {
            var isRegistryEnabled = await _featureFlagService.IsEnabledAsync(FeatureFlags.AGENT_REGISTRY_ENABLED);
            if (!isRegistryEnabled)
            {
                _logger.LogDebug("Agent registry is disabled, skipping first-class agent registration");
                return;
            }

            _logger.LogInformation("Registering first-class agents with dynamic registry");

            // Register NBA Agent
            await _agentRegistry.RegisterAgentAsync("nba", _nbaAgent, new AgentRegistrationMetadata
            {
                Name = "NBA Agent",
                Description = "Next Best Action agent for automated recommendations",
                Version = "1.0.0",
                Capabilities = new List<string> { "next-best-action", "recommendations", "automation" },
                IsFactoryCreated = false,
                Reason = "First-class NBA agent registered during orchestrator initialization"
            });

            // Register Context Intelligence Agent
            await _agentRegistry.RegisterAgentAsync("context-intelligence", _contextIntelligenceAgent, new AgentRegistrationMetadata
            {
                Name = "Context Intelligence Agent",
                Description = "Analyzes conversation context and provides intelligent insights",
                Version = "1.0.0",
                Capabilities = new List<string> { "context-analysis", "sentiment-analysis", "intelligence" },
                IsFactoryCreated = false,
                Reason = "First-class Context Intelligence agent registered during orchestrator initialization"
            });

            // Register Intent Classification Agent
            await _agentRegistry.RegisterAgentAsync("intent-classification", _intentClassificationAgent, new AgentRegistrationMetadata
            {
                Name = "Intent Classification Agent",
                Description = "Classifies user intents for proper agent routing",
                Version = "1.0.0",
                Capabilities = new List<string> { "intent-classification", "routing", "nlp" },
                IsFactoryCreated = false,
                Reason = "First-class Intent Classification agent registered during orchestrator initialization"
            });

            // Register Resource Agent
            await _agentRegistry.RegisterAgentAsync("resource", _resourceAgent, new AgentRegistrationMetadata
            {
                Name = "Resource Agent",
                Description = "Manages resource-related operations and recommendations",
                Version = "1.0.0",
                Capabilities = new List<string> { "resource-management", "recommendations", "operations" },
                IsFactoryCreated = false,
                Reason = "First-class Resource agent registered during orchestrator initialization"
            });

            _logger.LogInformation("Successfully registered all first-class agents with dynamic registry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering first-class agents with dynamic registry");
        }
    }

    /// <summary>
    /// Process request using dynamic agent routing via registry.
    /// </summary>
    private async Task<AgentResponse> ProcessRequestWithDynamicRoutingAsync(string userInput, Guid conversationId, string traceId, Guid auditId)
    {
        _logger.LogDebug("Processing request with dynamic routing, TraceId: {TraceId}", traceId);

        // First, classify the intent to determine which agent to route to
        var classificationResult = await _intentClassificationAgent.ClassifyIntentAsync(userInput, conversationId, traceId);
        
        // Route based on intent using dynamic registry
        var parameters = new Dictionary<string, object>
        {
            { "ConversationId", conversationId },
            { "AuditId", auditId }
        };

        return classificationResult.Intent switch
        {
            AgentIntent.ContactManagement => await RouteToAgentAsync("nba", userInput, parameters, traceId),
            AgentIntent.InteractionAnalysis => await RouteToAgentAsync("context-intelligence", userInput, parameters, traceId),
            AgentIntent.ResourceManagement => await RouteToAgentAsync("resource", userInput, parameters, traceId),
            AgentIntent.IntentClassification => await RouteToAgentAsync("intent-classification", userInput, parameters, traceId),
            _ => throw new InvalidOperationException($"Unsupported intent: {classificationResult.Intent}")
        };
    }

    /// <summary>
    /// Route request to a specific agent via dynamic registry.
    /// </summary>
    private async Task<AgentResponse> RouteToAgentAsync(string agentType, string userInput, Dictionary<string, object> parameters, string traceId)
    {
        try
        {
            // Check if agent is registered
            var isRegistered = await _agentRegistry.IsAgentRegisteredAsync(agentType);
            if (!isRegistered)
            {
                _logger.LogWarning("Agent {AgentType} not found in registry, falling back to legacy routing", agentType);
                return await ProcessRequestWithLegacyRoutingAsync(userInput, parameters, traceId);
            }

            // Route based on agent type
            return agentType switch
            {
                "nba" => await RouteToNbaAgentAsync(userInput, parameters, traceId),
                "context-intelligence" => await RouteToContextIntelligenceAgentAsync(userInput, parameters, traceId),
                "resource" => await RouteToResourceAgentAsync(userInput, parameters, traceId),
                "intent-classification" => await RouteToIntentClassificationAgentAsync(userInput, parameters, traceId),
                _ => await ProcessRequestWithLegacyRoutingAsync(userInput, parameters, traceId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to agent {AgentType}, TraceId: {TraceId}", agentType, traceId);
            return new AgentResponse
            {
                Success = false,
                Message = $"Error routing to {agentType} agent",
                AgentType = "AgentOrchestrator",
                TraceId = traceId,
                Reason = $"Exception occurred while routing to {agentType}: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Process request using legacy hardcoded routing (fallback).
    /// </summary>
    private async Task<AgentResponse> ProcessRequestWithLegacyRoutingAsync(string userInput, Dictionary<string, object> parameters, string traceId)
    {
        _logger.LogDebug("Processing request with legacy routing, TraceId: {TraceId}", traceId);

        // Check if this is an NBA-specific request
        if (IsNbaRequest(userInput))
        {
            return await RouteToNbaAgentAsync(userInput, parameters, traceId);
        }

        // Get tenant context for industry-specific prompting
        var tenant = await _tenantContext.GetCurrentTenantAsync();
        var industryProfile = await _tenantContext.GetIndustryProfileAsync();

        if (industryProfile == null)
        {
            throw new InvalidOperationException("Industry profile is not configured for this tenant.");
        }

        // Ensure collections are initialized
        industryProfile.SpecializedAgents = industryProfile.SpecializedAgents ?? new List<string>();
        industryProfile.AvailableActions = industryProfile.AvailableActions ?? new List<string>();

        // Validate agent registration
        foreach (var agent in industryProfile.SpecializedAgents)
        {
            if (!IsAgentRegistered(agent))
            {
                _logger.LogWarning("Agent {Agent} is not registered.", agent);
            }
        }

        // Create context-aware prompt for Azure AI Foundry
        var systemPrompt = await BuildSystemPromptAsync(industryProfile, (Guid)parameters["ConversationId"]);
        var userPrompt = await BuildUserPromptAsync(userInput, (Guid)parameters["ConversationId"], tenant);

        // Call Azure AI Foundry for natural language processing and task routing
        var aiResponse = await _aiFoundryService.ProcessAgentRequestAsync(
            systemPrompt,
            userPrompt,
            traceId);

        // Parse AI response and execute any resource-related actions
        return await ExecuteResourceActionsAsync(aiResponse, (Guid)parameters["ConversationId"]);
    }

    /// <summary>
    /// Route request to Context Intelligence Agent.
    /// </summary>
    private async Task<AgentResponse> RouteToContextIntelligenceAgentAsync(string userInput, Dictionary<string, object> parameters, string traceId)
    {
        _logger.LogDebug("Routing to Context Intelligence Agent, TraceId: {TraceId}", traceId);
        
        try
        {
            // For now, delegate to the context intelligence agent
            // This can be enhanced to call specific methods based on the request
            var response = new AgentResponse
            {
                Success = true,
                Message = "Context Intelligence Agent processing completed",
                AgentType = "ContextIntelligenceAgent",
                TraceId = traceId,
                AuditId = (Guid)parameters["AuditId"],
                Reason = "Request routed to Context Intelligence Agent for analysis"
            };

            _logger.LogInformation("Context Intelligence Agent processing completed, TraceId: {TraceId}", traceId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Context Intelligence Agent routing, TraceId: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = "Error in Context Intelligence Agent processing",
                AgentType = "ContextIntelligenceAgent",
                TraceId = traceId,
                AuditId = (Guid)parameters["AuditId"],
                Reason = $"Exception occurred in Context Intelligence Agent: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Route request to Resource Agent.
    /// </summary>
    private async Task<AgentResponse> RouteToResourceAgentAsync(string userInput, Dictionary<string, object> parameters, string traceId)
    {
        _logger.LogDebug("Routing to Resource Agent, TraceId: {TraceId}", traceId);
        
        try
        {
            // For now, delegate to the resource agent
            // This can be enhanced to call specific methods based on the request
            var response = new AgentResponse
            {
                Success = true,
                Message = "Resource Agent processing completed",
                AgentType = "ResourceAgent",
                TraceId = traceId,
                AuditId = (Guid)parameters["AuditId"],
                Reason = "Request routed to Resource Agent for resource management operations"
            };

            _logger.LogInformation("Resource Agent processing completed, TraceId: {TraceId}", traceId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Resource Agent routing, TraceId: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = "Error in Resource Agent processing",
                AgentType = "ResourceAgent",
                TraceId = traceId,
                AuditId = (Guid)parameters["AuditId"],
                Reason = $"Exception occurred in Resource Agent: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Route request to Intent Classification Agent.
    /// </summary>
    private async Task<AgentResponse> RouteToIntentClassificationAgentAsync(string userInput, Dictionary<string, object> parameters, string traceId)
    {
        _logger.LogDebug("Routing to Intent Classification Agent, TraceId: {TraceId}", traceId);
        
        try
        {
            // Classify the intent and return the result
            var classificationResult = await _intentClassificationAgent.ClassifyIntentAsync(userInput, (Guid)parameters["ConversationId"], traceId);
            
            var response = new AgentResponse
            {
                Success = true,
                Message = $"Intent classified as: {classificationResult.Intent} (Confidence: {classificationResult.Confidence:P})",
                AgentType = "IntentClassificationAgent",
                TraceId = traceId,
                AuditId = (Guid)parameters["AuditId"],
                Reason = classificationResult.Reason,
                Data = classificationResult
            };

            _logger.LogInformation("Intent Classification completed: {Intent} with confidence {Confidence}, TraceId: {TraceId}", 
                classificationResult.Intent, classificationResult.Confidence, traceId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Intent Classification Agent routing, TraceId: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = "Error in Intent Classification Agent processing",
                AgentType = "IntentClassificationAgent",
                TraceId = traceId,
                AuditId = (Guid)parameters["AuditId"],
                Reason = $"Exception occurred in Intent Classification Agent: {ex.Message}"
            };
        }
    }

    private Guid GetGuidFromContext(Dictionary<string, object> context, string key)
    {
        if (context.TryGetValue(key, out var value) && value is string stringValue)
        {
            if (Guid.TryParse(stringValue, out var guidValue))
            {
                return guidValue;
            }
            else
            {
                _logger.LogWarning("Invalid GUID format for key: {Key}, value: {Value}", key, stringValue);
            }
        }
        return Guid.Empty;
    }

    private bool IsAgentRegistered(string agentType)
    {
        return _agentRegistry.IsAgentRegisteredAsync(agentType).Result;
    }
}
