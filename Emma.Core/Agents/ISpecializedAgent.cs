using Emma.Core.Services;
using Emma.Data.Models;
using Emma.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emma.Core.Agents;

/// <summary>
/// Interface for specialized AI agents that can handle specific tasks
/// Supports both custom agents and Azure AI Foundry agent integration
/// </summary>
public interface ISpecializedAgent
{
    /// <summary>
    /// Agent type identifier for routing and discovery
    /// </summary>
    string AgentType { get; }
    
    /// <summary>
    /// Industries this agent specializes in
    /// </summary>
    List<string> SupportedIndustries { get; }
    
    /// <summary>
    /// Execute a task assigned to this agent
    /// </summary>
    Task<AgentResponse> ExecuteTaskAsync(AgentTask task);
    
    /// <summary>
    /// Check if this agent can handle the given task
    /// </summary>
    bool CanHandleTask(string taskType);
    
    /// <summary>
    /// Get agent capabilities for registration
    /// </summary>
    AgentCapability GetCapabilities();
}
