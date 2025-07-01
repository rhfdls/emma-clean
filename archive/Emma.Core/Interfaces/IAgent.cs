using Emma.Core.Models;
using Emma.Models.Enums;

namespace Emma.Core.Interfaces;

/// <summary>
/// Base interface that all agents in the EMMA platform must implement.
/// Provides common functionality and properties for all agent types.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Gets the unique identifier for this agent instance.
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// Gets the type identifier for this agent (e.g., "NBA", "ContextIntelligence").
    /// </summary>
    string AgentType { get; }
    
    /// <summary>
    /// Gets the display name of the agent for UI purposes.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Gets the description of what this agent does.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Gets the version of the agent implementation.
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Gets the agent's capabilities and permissions.
    /// </summary>
    AgentCapability Capability { get; }
    
    /// <summary>
    /// Processes a request within the specified context and returns a response.
    /// </summary>
    /// <param name="request">The agent request to process.</param>
    /// <param name="context">The context in which the request is being processed.</param>
    /// <returns>An AgentResponse containing the result of processing the request.</returns>
    Task<AgentResponse> ProcessRequestAsync(AgentRequest request, AgentContext context);
    
    /// <summary>
    /// Gets the current health status of the agent.
    /// </summary>
    /// <returns>An AgentHealthStatus value indicating the agent's health.</returns>
    Task<AgentHealthStatus> GetHealthStatusAsync();
    
    /// <summary>
    /// Validates that the agent can process the specified request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="context">The context in which the request will be processed.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the agent is not authorized to process the request.</exception>
    /// <exception cref="ArgumentException">Thrown if the request is invalid.</exception>
    Task ValidateRequestAsync(AgentRequest request, AgentContext context);
    
    /// <summary>
    /// Gets the agent's configuration as a dictionary of key-value pairs.
    /// </summary>
    /// <returns>A dictionary containing the agent's configuration.</returns>
    IReadOnlyDictionary<string, object> GetConfiguration();
    
    /// <summary>
    /// Updates the agent's configuration with the specified values.
    /// </summary>
    /// <param name="configuration">A dictionary containing the new configuration values.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the configuration is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the configuration is invalid.</exception>
    Task UpdateConfigurationAsync(IReadOnlyDictionary<string, object> configuration);
    
    /// <summary>
    /// Initializes the agent with any required resources.
    /// </summary>
    /// <returns>A task that completes when initialization is done</returns>
    Task InitializeAsync();
    
    /// <summary>
    /// Cleans up resources used by the agent.
    /// </summary>
    /// <returns>A task that completes when cleanup is done</returns>
    Task ShutdownAsync();
}
