using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emma.Models.Interfaces;

/// <summary>
/// Standardized interface for validating agent actions across all agents
/// Ensures consistent action relevance validation, approval workflows, and explainability
/// </summary>
public interface IAgentActionValidator
{
    /// <summary>
    /// Validates a collection of agent actions using the standardized validation pipeline
    /// </summary>
    /// <typeparam name="T">Type of agent action (NbaRecommendation, ResourceAction, etc.)</typeparam>
    /// <param name="actions">Actions to validate</param>
    /// <param name="context">Validation context including contact, organization, and user info</param>
    /// <param name="userOverrides">User override preferences that influence validation decisions</param>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <returns>Validated actions with approval metadata</returns>
    Task<List<T>> ValidateAgentActionsAsync<T>(
        List<T> actions, 
        AgentActionValidationContext context,
        Dictionary<string, object> userOverrides,
        string traceId) where T : IAgentAction;

    /// <summary>
    /// Validates a single agent action
    /// </summary>
    /// <typeparam name="T">Type of agent action</typeparam>
    /// <param name="action">Action to validate</param>
    /// <param name="context">Validation context</param>
    /// <param name="userOverrides">User override preferences that influence validation decisions</param>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <returns>Validated action with approval metadata</returns>
    Task<T?> ValidateSingleActionAsync<T>(
        T action, 
        AgentActionValidationContext context,
        Dictionary<string, object> userOverrides,
        string traceId) where T : IAgentAction;
}

/// <summary>
/// Context information for agent action validation
/// </summary>
public class AgentActionValidationContext
{
    public string ContactId { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
    
    /// <summary>
    /// User override preferences that must flow through the entire validation pipeline
    /// MANDATORY for audit trail, explainability, and regulatory compliance
    /// </summary>
    public Dictionary<string, object> UserOverrides { get; set; } = new();
}
