using Emma.Core.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for the Agent Orchestrator that manages routing and coordination of AI agents
    /// </summary>
    public interface IAgentOrchestrator
    {
        /// <summary>
        /// Get all available agents and their capabilities
        /// </summary>
        Task<List<AgentCapability>> GetAvailableAgentsAsync();

        /// <summary>
        /// Process a request by routing it to the appropriate agent
        /// </summary>
        Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null);

        /// <summary>
        /// Route to Azure AI Foundry agent
        /// </summary>
        Task<AgentResponse> RouteToAzureAgentAsync(string agentType, AgentTask task);

        /// <summary>
        /// Route to NBA Agent
        /// </summary>
        Task<AgentResponse> RouteToNbaAgentAsync(string userInput, Guid conversationId, string? traceId = null);

        /// <summary>
        /// Route to Context Intelligence Agent
        /// </summary>
        Task<AgentResponse> RouteToContextIntelligenceAgentAsync(AgentRequest request, string? traceId = null);

        /// <summary>
        /// Route to Intent Classification Agent
        /// </summary>
        Task<AgentResponse> RouteToIntentClassificationAgentAsync(AgentRequest request, string? traceId = null);

        /// <summary>
        /// Route to Resource Agent
        /// </summary>
        Task<AgentResponse> RouteToResourceAgentAsync(AgentRequest request, string? traceId = null);

        /// <summary>
        /// Execute a workflow with multiple agents
        /// </summary>
        Task<WorkflowState> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters);

        /// <summary>
        /// Set the orchestration method
        /// </summary>
        void SetOrchestrationMethod(string method);
    }
}
