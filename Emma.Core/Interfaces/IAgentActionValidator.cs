using Emma.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces;

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

    /// <summary>
    /// Converts an agent action to a scheduled action for validation
    /// </summary>
    /// <typeparam name="T">Type of agent action</typeparam>
    /// <param name="action">Action to convert</param>
    /// <param name="context">Validation context</param>
    /// <returns>Scheduled action for validation</returns>
    ScheduledAction ConvertToScheduledAction<T>(T action, AgentActionValidationContext context) where T : IAgentAction;

    /// <summary>
    /// Applies validation results back to the original action
    /// </summary>
    /// <typeparam name="T">Type of agent action</typeparam>
    /// <param name="action">Original action</param>
    /// <param name="relevanceResult">Validation result</param>
    /// <param name="requiresApproval">Whether approval is required</param>
    /// <param name="approvalRequestId">Approval request ID if created</param>
    /// <returns>Updated action with validation metadata</returns>
    T ApplyValidationResults<T>(
        T action, 
        ActionRelevanceResult relevanceResult, 
        bool requiresApproval, 
        string? approvalRequestId = null) where T : IAgentAction;
}

/// <summary>
/// Context information for agent action validation
/// </summary>
public class AgentActionValidationContext
{
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
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

/// <summary>
/// Interface that all agent actions must implement for standardized validation
/// </summary>
public interface IAgentAction
{
    string ActionType { get; set; }
    string Description { get; set; }
    int Priority { get; set; }
    double ConfidenceScore { get; set; }
    string ValidationReason { get; set; }
    bool RequiresApproval { get; set; }
    string ApprovalRequestId { get; set; }
    Dictionary<string, object> Parameters { get; set; }
    DateTime? SuggestedTiming { get; set; }
    string TraceId { get; set; }
    
    /// <summary>
    /// Scope of the action to determine validation intensity
    /// Essential for three-tier validation framework performance optimization
    /// </summary>
    ActionScope ActionScope { get; set; }
}
