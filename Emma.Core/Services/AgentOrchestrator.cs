using Emma.Data.Models;
using Microsoft.Extensions.Logging;
using Emma.Core.Interfaces;
using Emma.Core.Models;

namespace Emma.Core.Services;

/// <summary>
/// EMMA orchestrator implementation that delegates to Azure AI Foundry
/// Acts as a thin wrapper to maintain separation of concerns and leverage Azure services
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IAIFoundryService _aiFoundryService;
    private readonly ITenantContextService _tenantContext;
    private readonly ILogger<AgentOrchestrator> _logger;
    
    public AgentOrchestrator(
        IAIFoundryService aiFoundryService,
        ITenantContextService tenantContext,
        ILogger<AgentOrchestrator> logger)
    {
        _aiFoundryService = aiFoundryService;
        _tenantContext = tenantContext;
        _logger = logger;
    }
    
    public async Task<AgentResponse> ProcessRequestAsync(string userInput, Guid conversationId, string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Processing user request via Azure AI Foundry: {Request}, TraceId: {TraceId}", userInput, traceId);
            
            // Get tenant context for industry-specific prompting
            var tenant = await _tenantContext.GetCurrentTenantAsync();
            var industryProfile = await _tenantContext.GetIndustryProfileAsync();
            
            // Create context-aware prompt for Azure AI Foundry
            var systemPrompt = await BuildSystemPromptAsync(industryProfile, conversationId);
            var userPrompt = await BuildUserPromptAsync(userInput, conversationId, tenant);
            
            // Call Azure AI Foundry for natural language processing and task routing
            var aiResponse = await _aiFoundryService.ProcessAgentRequestAsync(
                systemPrompt, 
                userPrompt, 
                traceId);
            
            // Parse AI response and execute any resource-related actions
            return await ExecuteResourceActionsAsync(aiResponse, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request via Azure AI Foundry, TraceId: {TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                Message = "Error processing request",
                AgentType = "AgentOrchestrator"
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
                Steps = new List<WorkflowStep>(),
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
                Steps = new List<WorkflowStep>(),
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
            
            // Map industry-specific capabilities from Azure AI Foundry
            return industryProfile.SpecializedAgents.Select(agentType => new AgentCapability
            {
                AgentType = agentType,
                DisplayName = GetAgentDisplayName(agentType),
                Description = GetAgentDescription(agentType, industryProfile.IndustryCode),
                SupportedTasks = GetSupportedTasks(agentType),
                RequiredIndustries = new List<string> { industryProfile.IndustryCode },
                IsAvailable = true
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available agents from Azure AI Foundry");
            return new List<AgentCapability>();
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
    
    public void SetOrchestrationMethod(string method)
    {
        _logger.LogInformation("Setting orchestration method to: {Method}", method);
        // Store the orchestration method for future use
        // This could be stored in configuration or instance variable
    }
    
    private async Task<string> BuildSystemPromptAsync(IndustryProfile profile, Guid contactId)
    {
        return $@"{profile.SystemPrompt}

CURRENT CONTEXT:
- Industry: {profile.DisplayName}
- Contact ID: {contactId}
- Available Resource Types: {string.Join(", ", profile.ResourceTypes)}
- Compliance Requirements: {string.Join(", ", profile.ComplianceRequirements)}

AVAILABLE ACTIONS:
You can help with resource management tasks including:
- Finding and recommending resources (service providers)
- Assigning resources to contacts
- Checking resource availability and ratings
- Managing resource assignments and compliance

Always respond with specific, actionable recommendations using the available resource data.";
    }
    
    private async Task<string> BuildUserPromptAsync(string userRequest, Guid contactId, Organization tenant)
    {
        return $@"USER REQUEST: {userRequest}

CONTEXT:
- Organization: {tenant.Email}
- Industry: {tenant.IndustryCode}
- Contact ID: {contactId}

Please analyze this request and provide specific recommendations for resource management actions.
If the request involves finding or assigning service providers (resources), provide specific guidance on next steps.";
    }
    
    private async Task<string> BuildAgentSystemPromptAsync(string agentType, AgentTask task)
    {
        var industryProfile = await _tenantContext.GetIndustryProfileAsync();
        
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
    
    private string GetAgentDisplayName(string agentType) => agentType switch
    {
        "ContractorAgent" => "Resource Specialist",
        "MortgageAgent" => "Lending Specialist", 
        "InspectorAgent" => "Inspection Specialist",
        "AttorneyAgent" => "Legal Specialist",
        _ => "Resource Specialist"
    };
    
    private string GetAgentDescription(string agentType, string industryCode) => agentType switch
    {
        "ContractorAgent" => $"Specialized in finding and managing service provider resources for {industryCode}",
        "MortgageAgent" => $"Specialized in lending and financing resources for {industryCode}",
        "InspectorAgent" => $"Specialized in inspection and assessment resources for {industryCode}",
        "AttorneyAgent" => $"Specialized in legal and compliance resources for {industryCode}",
        _ => $"General resource management for {industryCode}"
    };
    
    private List<string> GetSupportedTasks(string agentType) => agentType switch
    {
        "ContractorAgent" => new() { "find_resource", "assign_resource", "get_recommendations" },
        "MortgageAgent" => new() { "find_lender", "assign_lender", "get_loan_options" },
        "InspectorAgent" => new() { "find_inspector", "schedule_inspection", "get_inspection_reports" },
        "AttorneyAgent" => new() { "find_attorney", "assign_legal_counsel", "compliance_check" },
        _ => new() { "find_resource", "assign_resource", "get_recommendations" }
    };
}
