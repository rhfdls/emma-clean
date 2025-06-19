namespace Emma.Models.Enums;

/// <summary>
/// Represents the possible statuses of a task.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// The task has been created but not yet started.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// The task is currently being worked on.
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// The task has been completed.
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// The task is past its due date and not completed.
    /// </summary>
    Overdue = 3,
    
    /// <summary>
    /// The task has been canceled and will not be completed.
    /// </summary>
    Canceled = 4,
    
    /// <summary>
    /// The task is on hold and not currently being worked on.
    /// </summary>
    OnHold = 5,
    
    /// <summary>
    /// The task is waiting for approval before proceeding.
    /// </summary>
    PendingApproval = 6
}
