using Emma.Core.Models;
using Emma.Core.Agents;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for agent registration and discovery service
    /// Manages the agent catalog and provides discovery capabilities
    /// </summary>
    public interface IAgentRegistryService
    {
        /// <summary>
        /// Register an agent with the registry
        /// </summary>
        /// <param name="agentId">Unique agent identifier</param>
        /// <param name="agent">Agent implementation</param>
        /// <param name="capability">Agent capability definition</param>
        /// <returns>Registration result</returns>
        Task<bool> RegisterAgentAsync(string agentId, ISpecializedAgent agent, AgentCapability capability);
        
        /// <summary>
        /// Unregister an agent from the registry
        /// </summary>
        /// <param name="agentId">Agent identifier to remove</param>
        /// <returns>Unregistration result</returns>
        Task<bool> UnregisterAgentAsync(string agentId);
        
        /// <summary>
        /// Get agent by ID
        /// </summary>
        /// <param name="agentId">Agent identifier</param>
        /// <returns>Agent instance or null if not found</returns>
        Task<ISpecializedAgent?> GetAgentAsync(string agentId);
        
        /// <summary>
        /// Get agent capability by ID
        /// </summary>
        /// <param name="agentId">Agent identifier</param>
        /// <returns>Agent capability or null if not found</returns>
        Task<AgentCapability?> GetAgentCapabilityAsync(string agentId);
        
        /// <summary>
        /// Get all registered agent capabilities
        /// A2A Agent Card compatible
        /// </summary>
        /// <returns>Dictionary of agent capabilities by agent ID</returns>
        Task<Dictionary<string, AgentCapability>> GetAllAgentCapabilitiesAsync();
        
        /// <summary>
        /// Find agents that can handle a specific intent
        /// </summary>
        /// <param name="intent">Intent to match</param>
        /// <param name="industry">Optional industry filter</param>
        /// <returns>List of matching agent capabilities</returns>
        Task<List<AgentCapability>> FindAgentsForIntentAsync(AgentIntent intent, string? industry = null);
        
        /// <summary>
        /// Get agent health status
        /// </summary>
        /// <param name="agentId">Agent identifier</param>
        /// <returns>Health status information</returns>
        Task<AgentHealthStatus> GetAgentHealthAsync(string agentId);
        
        /// <summary>
        /// Get all agent health statuses
        /// </summary>
        /// <returns>Dictionary of health statuses by agent ID</returns>
        Task<Dictionary<string, AgentHealthStatus>> GetAllAgentHealthAsync();
        
        /// <summary>
        /// Update agent performance metrics
        /// </summary>
        /// <param name="agentId">Agent identifier</param>
        /// <param name="responseTimeMs">Response time in milliseconds</param>
        /// <param name="success">Whether the request was successful</param>
        /// <param name="confidence">Confidence score of the response</param>
        /// <returns>Update result</returns>
        Task UpdateAgentMetricsAsync(string agentId, long responseTimeMs, bool success, double confidence);
        
        /// <summary>
        /// Load agent cards from catalog directory
        /// Supports A2A Agent Card JSON format
        /// </summary>
        /// <param name="catalogPath">Path to agent catalog directory</param>
        /// <returns>Number of agents loaded</returns>
        Task<int> LoadAgentCatalogAsync(string catalogPath);
        
        /// <summary>
        /// Validate agent capability definition
        /// </summary>
        /// <param name="capability">Agent capability to validate</param>
        /// <returns>Validation result with errors if any</returns>
        Task<AgentValidationResult> ValidateAgentCapabilityAsync(AgentCapability capability);
    }

    /// <summary>
    /// Agent health status information
    /// </summary>
    public class AgentHealthStatus
    {
        public string AgentId { get; set; } = string.Empty;
        
        public bool IsHealthy { get; set; }
        
        public string Status { get; set; } = "Unknown";
        
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
        
        public long ResponseTimeMs { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    /// <summary>
    /// Agent validation result
    /// </summary>
    public class AgentValidationResult
    {
        public bool IsValid { get; set; }
        
        public List<string> Errors { get; set; } = new();
        
        public List<string> Warnings { get; set; } = new();
        
        public string? ValidationSummary { get; set; }
    }
}
