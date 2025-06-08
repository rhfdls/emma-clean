using Emma.Core.Models;
using Emma.Core.Agents;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for agent-to-agent communication bus
    /// Hot-swappable orchestration following Microsoft best practices
    /// </summary>
    public interface IAgentCommunicationBus
    {
        /// <summary>
        /// Route request to appropriate agent based on intent
        /// Supports both custom orchestration and Azure AI Foundry workflows
        /// </summary>
        /// <param name="request">Agent request with intent and context</param>
        /// <returns>Agent response from the target agent</returns>
        Task<AgentResponse> RouteRequestAsync(AgentRequest request);
        
        /// <summary>
        /// Register a specialized agent with the communication bus
        /// </summary>
        /// <param name="agentId">Unique agent identifier</param>
        /// <param name="agent">Agent implementation</param>
        /// <param name="capability">Agent capability definition</param>
        Task<bool> RegisterAgentAsync(string agentId, ISpecializedAgent agent, AgentCapability capability);
        
        /// <summary>
        /// Unregister an agent from the communication bus
        /// </summary>
        /// <param name="agentId">Agent identifier to remove</param>
        Task UnregisterAgentAsync(string agentId);
        
        /// <summary>
        /// Get all registered agent capabilities
        /// A2A Agent Card compatible
        /// </summary>
        /// <returns>Dictionary of agent capabilities by agent ID</returns>
        Task<Dictionary<string, AgentCapability>> GetAgentCapabilitiesAsync();
        
        /// <summary>
        /// Execute a multi-agent workflow
        /// Supports both custom state management and future Azure workflows
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <param name="initialRequest">Initial request to start the workflow</param>
        /// <returns>Workflow state with execution results</returns>
        Task<WorkflowState> ExecuteWorkflowAsync(string workflowId, AgentRequest initialRequest);
        
        /// <summary>
        /// Get workflow state for monitoring and debugging
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <returns>Current workflow state</returns>
        Task<WorkflowState?> GetWorkflowStateAsync(string workflowId);
        
        /// <summary>
        /// Update orchestration method for migration tracking
        /// "custom" | "foundry_workflow" | "connected_agent"
        /// </summary>
        /// <param name="method">Orchestration method identifier</param>
        void SetOrchestrationMethod(string method);
    }
}
