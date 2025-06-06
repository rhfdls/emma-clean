using Emma.Core.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Orchestrates multiple specialized AI agents for contact management
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Process an interaction through the appropriate agent workflow
    /// </summary>
    Task<AgentOrchestratorResponse> ProcessInteractionAsync(InteractionTrigger trigger);
    
    /// <summary>
    /// Execute a specific agent workflow by name
    /// </summary>
    Task<AgentOrchestratorResponse> ExecuteWorkflowAsync(string workflowName, Dictionary<string, object> context);
    
    /// <summary>
    /// Get available workflows for a given trigger type
    /// </summary>
    Task<List<string>> GetAvailableWorkflowsAsync(TriggerType triggerType);
}

/// <summary>
/// Trigger that initiates agent workflow processing
/// </summary>
public class InteractionTrigger
{
    public TriggerType Type { get; set; }
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of triggers that can initiate agent workflows
/// </summary>
public enum TriggerType
{
    InboundCall,
    OutboundCall,
    SmsReceived,
    EmailReceived,
    CalendarEvent,
    ManualNote,
    SentimentFlag,
    UnknownContact
}

/// <summary>
/// Response from agent orchestrator processing
/// </summary>
public class AgentOrchestratorResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<AgentResult> AgentResults { get; set; } = new();
    public List<EmmaAction> RecommendedActions { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result from individual agent execution
/// </summary>
public class AgentResult
{
    public string AgentName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Output { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
}
