using Microsoft.Extensions.Logging;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Agent Registry for dynamic agent registration and discovery.
    /// Enables factory-created agents to be registered at runtime without code changes.
    /// Follows Microsoft DI best practices - no service locator pattern.
    /// </summary>
    public interface IAgentRegistry
    {
        /// <summary>
        /// Register an agent instance with the registry.
        /// </summary>
        /// <typeparam name="T">Agent interface type</typeparam>
        /// <param name="agentType">Unique agent type identifier</param>
        /// <param name="agent">Agent instance to register</param>
        /// <param name="metadata">Optional metadata for the agent</param>
        /// <returns>Task for async registration</returns>
        Task RegisterAgentAsync<T>(string agentType, T agent, AgentRegistrationMetadata? metadata = null) where T : class;

        /// <summary>
        /// Get a registered agent by type.
        /// </summary>
        /// <typeparam name="T">Expected agent interface type</typeparam>
        /// <param name="agentType">Agent type identifier</param>
        /// <returns>Agent instance or null if not found</returns>
        Task<T?> GetAgentAsync<T>(string agentType) where T : class;

        /// <summary>
        /// Check if an agent type is registered.
        /// </summary>
        /// <param name="agentType">Agent type identifier</param>
        /// <returns>True if agent is registered</returns>
        Task<bool> IsAgentRegisteredAsync(string agentType);

        /// <summary>
        /// Get all registered agent types.
        /// </summary>
        /// <returns>Collection of registered agent type identifiers</returns>
        Task<IEnumerable<string>> GetRegisteredAgentTypesAsync();

        /// <summary>
        /// Get agent metadata for a registered agent.
        /// </summary>
        /// <param name="agentType">Agent type identifier</param>
        /// <returns>Agent metadata or null if not found</returns>
        Task<AgentRegistrationMetadata?> GetAgentMetadataAsync(string agentType);

        /// <summary>
        /// Unregister an agent from the registry.
        /// </summary>
        /// <param name="agentType">Agent type identifier</param>
        /// <returns>True if agent was unregistered, false if not found</returns>
        Task<bool> UnregisterAgentAsync(string agentType);

        /// <summary>
        /// Get health status for all registered agents.
        /// </summary>
        /// <returns>Dictionary of agent type to health status</returns>
        Task<IDictionary<string, AgentHealthStatus>> GetAgentHealthStatusAsync();
    }

    /// <summary>
    /// Metadata associated with agent registration.
    /// </summary>
    public class AgentRegistrationMetadata
    {
        /// <summary>
        /// Human-readable agent name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Agent version information.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Agent description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Agent capabilities or tags.
        /// </summary>
        public IList<string> Capabilities { get; set; } = new List<string>();

        /// <summary>
        /// Whether this agent was created by the factory.
        /// </summary>
        public bool IsFactoryCreated { get; set; } = false;

        /// <summary>
        /// Timestamp when agent was registered.
        /// </summary>
        public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Unique identifier for audit trail.
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Reason for registration (explainability).
        /// </summary>
        public string Reason { get; set; } = "Agent registered via IAgentRegistry";
    }

    /// <summary>
    /// Health status for a registered agent.
    /// </summary>
    public enum AgentHealthStatus
    {
        /// <summary>
        /// Agent is healthy and operational.
        /// </summary>
        Healthy,

        /// <summary>
        /// Agent is experiencing issues but still functional.
        /// </summary>
        Degraded,

        /// <summary>
        /// Agent is not responding or failed health check.
        /// </summary>
        Unhealthy,

        /// <summary>
        /// Agent health status is unknown.
        /// </summary>
        Unknown
    }
}
