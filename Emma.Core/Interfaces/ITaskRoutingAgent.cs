using Emma.Core.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Routes tasks and determines next best actions based on context
/// </summary>
public interface ITaskRoutingAgent
{
    /// <summary>
    /// Determine next best actions based on interaction and context
    /// </summary>
    Task<TaskRoutingResult> RouteTaskAsync(TaskRoutingRequest request);
    
    /// <summary>
    /// Get available action types for a given context
    /// </summary>
    Task<List<EmmaActionType>> GetAvailableActionsAsync(Guid contactId, string? interactionContent = null);
    
    /// <summary>
    /// Evaluate urgency and priority of a task
    /// </summary>
    Task<TaskPriority> EvaluateTaskPriorityAsync(string content, ContactContext context);
}

/// <summary>
/// Request for task routing
/// </summary>
public class TaskRoutingRequest
{
    public Guid ContactId { get; set; }
    public string InteractionContent { get; set; } = string.Empty;
    public ContactContext? Context { get; set; }
    public TriggerType TriggerType { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of task routing
/// </summary>
public class TaskRoutingResult
{
    public List<EmmaAction> RecommendedActions { get; set; } = new();
    public TaskPriority Priority { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public bool RequiresImmediateAttention { get; set; }
    public DateTime SuggestedFollowUpDate { get; set; }
}

/// <summary>
/// Task priority levels
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent,
    Critical
}

/// <summary>
/// Types of triggers that can initiate task routing
/// </summary>
public enum TriggerType
{
    IncomingCall,
    IncomingText,
    IncomingEmail,
    ScheduledFollowUp,
    PropertyUpdate,
    MarketChange,
    UserAction,
    SystemEvent
}
