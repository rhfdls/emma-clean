namespace Emma.Models.Enums;

/// <summary>
/// Represents the priority levels that can be assigned to a task.
/// </summary>
public enum TaskPriority
{
    /// <summary>
    /// Low priority task (can be deferred if necessary).
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Normal priority task (default).
    /// </summary>
    Medium = 1,
    
    /// <summary>
    /// High priority task (should be addressed soon).
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Critical priority task (requires immediate attention).
    /// </summary>
    Urgent = 3,
    
    /// <summary>
    /// The task is blocked by another task or dependency.
    /// </summary>
    Blocked = 4
}
