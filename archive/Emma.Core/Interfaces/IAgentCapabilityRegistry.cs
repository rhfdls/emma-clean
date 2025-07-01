using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emma.Core.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Manages agent capabilities and permissions in the system.
    /// Ensures that agents can only perform actions they are explicitly authorized for.
    /// </summary>
    public interface IAgentCapabilityRegistry
    {
        /// <summary>
        /// Registers a capability set for an agent type.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <param name="capability">The capability set to register.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task RegisterCapabilityAsync(string agentType, AgentCapability capability);

        /// <summary>
        /// Gets the capability set for an agent type.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the capability set, or null if not found.</returns>
        Task<AgentCapability?> GetCapabilityAsync(string agentType);

        /// <summary>
        /// Checks if an agent is allowed to perform an action.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <param name="action">The action to check, in "resource:action" format (e.g., "contact:read").</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the action is allowed, false otherwise.</returns>
        Task<bool> IsActionAllowedAsync(string agentType, string action);

        /// <summary>
        /// Checks if an agent is allowed to use a tool.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <param name="toolName">The name of the tool.</param>
        /// <param name="action">The action to perform with the tool.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the tool usage is allowed, false otherwise.</returns>
        Task<bool> IsToolAllowedAsync(string agentType, string toolName, string action);

        /// <summary>
        /// Checks if an agent can delegate to another agent type.
        /// </summary>
        /// <param name="sourceAgentType">The type of the source agent.</param>
        /// <param name="targetAgentType">The type of the target agent.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if delegation is allowed, false otherwise.</returns>
        Task<bool> CanDelegateToAsync(string sourceAgentType, string targetAgentType);

        /// <summary>
        /// Gets all registered agent types with their capabilities.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of agent types to their capabilities.</returns>
        Task<IReadOnlyDictionary<string, AgentCapability>> GetAllCapabilitiesAsync();

        /// <summary>
        /// Validates that an agent can perform an action, throwing an exception if not.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <param name="action">The action to validate.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the agent is not authorized to perform the action.</exception>
        Task ValidateActionAsync(string agentType, string action);

        /// <summary>
        /// Validates that an agent can use a tool, throwing an exception if not.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <param name="toolName">The name of the tool.</param>
        /// <param name="action">The action to perform with the tool.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the agent is not authorized to use the tool.</exception>
        Task ValidateToolUsageAsync(string agentType, string toolName, string action);

        /// <summary>
        /// Validates that an agent can delegate to another agent type, throwing an exception if not.
        /// </summary>
        /// <param name="sourceAgentType">The type of the source agent.</param>
        /// <param name="targetAgentType">The type of the target agent.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if delegation is not allowed.</exception>
        Task ValidateDelegationAsync(string sourceAgentType, string targetAgentType);

        /// <summary>
        /// Gets the default capability set for an agent type if not explicitly defined.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the default capability set, or null if none is defined.</returns>
        Task<AgentCapability?> GetDefaultCapabilityAsync(string agentType);

        /// <summary>
        /// Sets the default capability set for an agent type if not explicitly defined.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <param name="capability">The default capability set to use.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SetDefaultCapabilityAsync(string agentType, AgentCapability capability);

        /// <summary>
        /// Gets the effective capability set for an agent, combining explicit and default capabilities.
        /// </summary>
        /// <param name="agentType">The type of the agent.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the effective capability set.</returns>
        Task<AgentCapability> GetEffectiveCapabilityAsync(string agentType);
    }
}
