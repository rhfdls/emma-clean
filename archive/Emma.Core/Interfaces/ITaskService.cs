using Emma.Models.Models;
using Emma.Models.Enums;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Service for managing tasks with strict User/Agent separation.
    /// Enforces business rules, access control, and audit logging.
    /// </summary>
    public interface ITaskService
    {
        // ===== CRUD Operations =====
        
        /// <summary>
        /// Gets a task by ID with proper access control
        /// </summary>
        /// <param name="taskId">ID of the task to retrieve</param>
        /// <param name="requestingUserId">ID of the user making the request (for access control)</param>
        /// <returns>The task if found and accessible, null if not found, throws if access denied</returns>
        Task<TaskItem> GetTaskByIdAsync(string taskId, Guid requestingUserId);
        
        /// <summary>
        /// Gets tasks based on filter criteria with proper access control
        /// </summary>
        Task<IEnumerable<TaskItem>> GetTasksAsync(TaskFilter filter, Guid requestingUserId);
        
        /// <summary>
        /// Creates a new task with audit trail
        /// </summary>
        Task<TaskItem> CreateTaskAsync(TaskItem task, Guid createdByUserId);
        
        /// <summary>
        /// Updates an existing task with audit trail
        /// </summary>
        Task<TaskItem> UpdateTaskAsync(TaskItem task, Guid updatedByUserId);
        
        /// <summary>
        /// Soft deletes a task (marks as deleted but keeps in database)
        /// </summary>
        Task<bool> DeleteTaskAsync(string taskId, Guid deletedByUserId, string? reason = null);
        
        // ===== Task Assignment =====
        
        /// <summary>
        /// Assigns a task to a user
        /// </summary>
        Task<TaskItem> AssignTaskAsync(string taskId, Guid assigneeUserId, Guid assignedByUserId, string? notes = null);
        
        /// <summary>
        /// Unassigns a task from the current assignee
        /// </summary>
        Task<TaskItem> UnassignTaskAsync(string taskId, Guid unassignedByUserId, string? reason = null);
        
        // ===== Task Status Management =====
        
        /// <summary>
        /// Updates the status of a task
        /// </summary>
        Task<TaskItem> UpdateEmma.Models.Enums.TaskStatusAsync(string taskId, Emma.Models.Enums.TaskStatus newStatus, Guid updatedByUserId, string? notes = null);
        
        /// <summary>
        /// Marks a task as complete
        /// </summary>
        Task<TaskItem> CompleteTaskAsync(string taskId, Guid completedByUserId, string? notes = null);
        
        // ===== NBA Integration =====
        
        /// <summary>
        /// Creates a task from an NBA recommendation
        /// </summary>
        Task<TaskItem> CreateTaskFromNbaRecommendationAsync(
            string recommendationId, 
            string contactId, 
            string title, 
            string description, 
            DateTime dueDate, 
            double confidenceScore,
            bool requiresApproval = false);
            
        /// <summary>
        /// Validates an NBA-recommended task
        /// </summary>
        Task<bool> ValidateNbaTaskAsync(string taskId, bool isValid, string? reason, Guid validatedByUserId);
        
        // ===== Reporting =====
        
        /// <summary>
        /// Gets task statistics for reporting
        /// </summary>
        Task<TaskStatistics> GetTaskStatisticsAsync(TaskStatisticsFilter filter, Guid requestingUserId);
        
        /// <summary>
        /// Gets overdue tasks for a user
        /// </summary>
        Task<IEnumerable<TaskItem>> GetOverdueTasksAsync(Guid userId, int limit = 50);
        
        /// <summary>
        /// Gets upcoming tasks for a user
        /// </summary>
        Task<IEnumerable<TaskItem>> GetUpcomingTasksAsync(Guid userId, DateTime? endDate = null, int limit = 50);
    }
    
    /// <summary>
    /// Filter criteria for task queries
    /// </summary>
    public class TaskFilter
    {
        public string? ContactId { get; set; }
        public string? OrganizationId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public Emma.Models.Enums.TaskStatus? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public string? TaskType { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public bool IncludeCompleted { get; set; } = false;
        public bool IncludeDeleted { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }
    
    /// <summary>
    /// Filter criteria for task statistics
    /// </summary>
    public class TaskStatisticsFilter
    {
        public string? OrganizationId { get; set; }
        public Guid? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? TaskType { get; set; }
    }
    
    /// <summary>
    /// Task statistics for reporting
    /// </summary>
    public class TaskStatistics
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int DueSoonTasks { get; set; }
        public int NotStartedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public Dictionary<string, int> TasksByStatus { get; set; } = new();
        public Dictionary<string, int> TasksByPriority { get; set; } = new();
        public Dictionary<string, int> TasksByType { get; set; } = new();
        public Dictionary<string, int> TasksByAssignee { get; set; } = new();
        public double AverageCompletionTimeHours { get; set; }
        public Dictionary<DateTime, int> CompletionTrend { get; set; } = new();
    }
}
