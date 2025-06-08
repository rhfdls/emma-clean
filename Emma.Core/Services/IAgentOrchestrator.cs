using Emma.Data.Models;
using Emma.Core.Models;

namespace Emma.Core.Services;

/// <summary>
/// Orchestrator interface for managing agent interactions and workflows
/// Supports both custom orchestration and Azure AI Foundry workflows
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Process a user request through the agent orchestration system
    /// </summary>
    /// <param name="userInput">User's natural language input</param>
    /// <param name="conversationId">Conversation identifier for context</param>
    /// <param name="traceId">Trace ID for observability</param>
    /// <returns>Orchestrated response from appropriate agents</returns>
    Task<AgentResponse> ProcessRequestAsync(string userInput, Guid conversationId, string? traceId = null);
    
    /// <summary>
    /// Execute a complex workflow involving multiple agents
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="initialRequest">Initial request to start the workflow</param>
    /// <param name="traceId">Trace ID for observability</param>
    /// <returns>Workflow execution result</returns>
    Task<WorkflowState> ExecuteWorkflowAsync(string workflowId, AgentRequest initialRequest, string? traceId = null);
    
    /// <summary>
    /// Get available agent capabilities
    /// </summary>
    /// <returns>List of available agent capabilities</returns>
    Task<List<AgentCapability>> GetAvailableAgentsAsync();
    
    /// <summary>
    /// Set the orchestration method (custom, foundry_workflow, connected_agent)
    /// </summary>
    /// <param name="method">Orchestration method</param>
    void SetOrchestrationMethod(string method);
}
