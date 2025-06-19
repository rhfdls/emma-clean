using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Emma.Models.Models;
using Emma.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Emma.Core.Exceptions;

namespace Emma.Core.Services
{
    /// <summary>
    /// Service for managing tasks with strict User/Agent separation.
    /// Enforces business rules, access control, and audit logging.
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly IAppDbContext _context;
        private readonly IContactAccessService _contactAccessService;
        private readonly ILogger<TaskService> _logger;
        private readonly IUserContext _userContext;

        public TaskService(
            IAppDbContext context,
            IContactAccessService contactAccessService,
            ILogger<TaskService> logger,
            IUserContext userContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _contactAccessService = contactAccessService ?? throw new ArgumentNullException(nameof(contactAccessService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        // ===== CRUD Operations =====

        public async Task<TaskItem> GetTaskByIdAsync(string taskId, Guid requestingUserId)
        {
            if (string.IsNullOrEmpty(taskId))
                throw new ArgumentException("Task ID is required", nameof(taskId));

            var task = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.Contact)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new NotFoundException($"Task with ID {taskId} not found");

            // Check if user has access to the task's contact
            if (!await _contactAccessService.CanAccessContactAsync(task.ContactId, requestingUserId))
                throw new UnauthorizedAccessException("You do not have permission to access this task");

            return task;
        }

        public async Task<IEnumerable<TaskItem>> GetTasksAsync(TaskFilter filter, Guid requestingUserId)
        {
            // Start with base query
            var query = _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.Contact)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.ContactId))
                query = query.Where(t => t.ContactId == filter.ContactId);

            if (!string.IsNullOrEmpty(filter.OrganizationId))
                query = query.Where(t => t.OrganizationId == filter.OrganizationId);

            if (filter.AssignedToUserId.HasValue)
                query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId);

            if (filter.CreatedByUserId.HasValue)
                query = query.Where(t => t.CreatedByUserId == filter.CreatedByUserId);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status);

            if (filter.Priority.HasValue)
                query = query.Where(t => t.Priority == filter.Priority);

            if (!string.IsNullOrEmpty(filter.TaskType))
                query = query.Where(t => t.TaskType == filter.TaskType);

            if (filter.DueDateFrom.HasValue)
                query = query.Where(t => t.DueDate >= filter.DueDateFrom.Value);

            if (filter.DueDateTo.HasValue)
                query = query.Where(t => t.DueDate <= filter.DueDateTo.Value);

            if (!filter.IncludeCompleted)
                query = query.Where(t => t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled);

            if (!filter.IncludeDeleted)
                query = query.Where(t => !t.IsDeleted);

            // Apply access control - user can only see tasks for contacts they have access to
            var accessibleContactIds = await _contactAccessService.GetAccessibleContactIdsAsync(requestingUserId);
            query = query.Where(t => accessibleContactIds.Contains(t.ContactId));

            // Apply sorting
            query = ApplySorting(query, filter.SortBy, filter.SortDescending);

            // Apply pagination
            if (filter.PageNumber > 0 && filter.PageSize > 0)
            {
                query = query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize);
            }

            return await query.ToListAsync();
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task, Guid createdByUserId)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // Verify the user has access to the contact
            if (!await _contactAccessService.CanAccessContactAsync(task.ContactId, createdByUserId))
                throw new UnauthorizedAccessException("You do not have permission to create tasks for this contact");

            // Set audit fields
            task.Id = Guid.NewGuid().ToString();
            task.CreatedByUserId = createdByUserId;
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;

            // Set default status if not provided
            if (task.Status == default)
                task.Status = TaskStatus.Pending;

            // Validate the task
            var (isValid, errorMessage) = task.Validate();
            if (!isValid)
                throw new ValidationException(errorMessage ?? "Task validation failed");

            // Add to database
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} created by user {UserId}", task.Id, createdByUserId);

            return task;
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskItem task, Guid updatedByUserId)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var existingTask = await _context.TaskItems.FindAsync(task.Id);
            if (existingTask == null)
                throw new NotFoundException($"Task with ID {task.Id} not found");

            // Verify the user has access to the task's contact
            if (!await _contactAccessService.CanAccessContactAsync(existingTask.ContactId, updatedByUserId))
                throw new UnauthorizedAccessException("You do not have permission to update this task");

            // Update fields
            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.Priority = task.Priority;
            existingTask.TaskType = task.TaskType;
            existingTask.UpdatedByUserId = updatedByUserId;
            existingTask.UpdatedAt = DateTime.UtcNow;

            // Validate the task
            var (isValid, errorMessage) = existingTask.Validate();
            if (!isValid)
                throw new ValidationException(errorMessage ?? "Task validation failed");

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} updated by user {UserId}", existingTask.Id, updatedByUserId);

            return existingTask;
        }

        public async Task<bool> DeleteTaskAsync(string taskId, Guid deletedByUserId, string? reason = null)
        {
            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException($"Task with ID {taskId} not found");

            // Verify the user has access to the task's contact
            if (!await _contactAccessService.CanAccessContactAsync(task.ContactId, deletedByUserId))
                throw new UnauthorizedAccessException("You do not have permission to delete this task");

            // Soft delete
            task.IsDeleted = true;
            task.UpdatedByUserId = deletedByUserId;
            task.UpdatedAt = DateTime.UtcNow;

            // Add deletion reason to metadata
            if (!string.IsNullOrEmpty(reason))
            {
                task.Metadata["DeletionReason"] = reason;
                task.Metadata["DeletedBy"] = deletedByUserId;
                task.Metadata["DeletedAt"] = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} soft-deleted by user {UserId}", taskId, deletedByUserId);
            return true;
        }

        // ===== Task Assignment =====

        public async Task<TaskItem> AssignTaskAsync(string taskId, Guid assigneeUserId, Guid assignedByUserId, string? notes = null)
        {
            var task = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new NotFoundException($"Task with ID {taskId} not found");

            // Verify the assigning user has permission to assign this task
            if (!await _contactAccessService.CanAccessContactAsync(task.ContactId, assignedByUserId))
                throw new UnauthorizedAccessException("You do not have permission to assign this task");

            // Verify the assignee exists and is in the same organization
            var assignee = await _context.Users.FindAsync(assigneeUserId);
            if (assignee == null)
                throw new NotFoundException($"User with ID {assigneeUserId} not found");

            if (assignee.OrganizationId != task.OrganizationId)
                throw new ValidationException("Cannot assign task to a user in a different organization");

            // Update assignment
            task.AssignedToUserId = assigneeUserId;
            task.UpdatedByUserId = assignedByUserId;
            task.UpdatedAt = DateTime.UtcNow;

            // Add to assignment history
            var assignmentHistory = task.Metadata.TryGetValue("AssignmentHistory", out var history) && history is List<object> historyList
                ? historyList
                : new List<object>();

            assignmentHistory.Add(new 
            { 
                AssignedToUserId = assigneeUserId,
                AssignedByUserId = assignedByUserId,
                AssignedAt = DateTime.UtcNow,
                Notes = notes
            });

            task.Metadata["AssignmentHistory"] = assignmentHistory;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} assigned to user {AssigneeId} by user {AssignedById}", 
                taskId, assigneeUserId, assignedByUserId);

            return task;
        }

        public async Task<TaskItem> UnassignTaskAsync(string taskId, Guid unassignedByUserId, string? reason = null)
        {
            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException($"Task with ID {taskId} not found");

            // Verify the user has permission to unassign this task
            if (!await _contactAccessService.CanAccessContactAsync(task.ContactId, unassignedByUserId))
                throw new UnauthorizedAccessException("You do not have permission to unassign this task");

            // Only unassign if the task is assigned to someone
            if (!task.AssignedToUserId.HasValue)
                return task; // Already unassigned

            // Add to assignment history before unassigning
            var assignmentHistory = task.Metadata.TryGetValue("AssignmentHistory", out var history) && history is List<object> historyList
                ? historyList
                : new List<object>();

            assignmentHistory.Add(new 
            { 
                UnassignedAt = DateTime.UtcNow,
                UnassignedByUserId = unassignedByUserId,
                PreviousAssigneeId = task.AssignedToUserId,
                Reason = reason
            });

            task.Metadata["AssignmentHistory"] = assignmentHistory;

            // Unassign the task
            var previousAssigneeId = task.AssignedToUserId;
            task.AssignedToUserId = null;
            task.UpdatedByUserId = unassignedByUserId;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} unassigned from user {PreviousAssigneeId} by user {UnassignedByUserId}", 
                taskId, previousAssigneeId, unassignedByUserId);

            return task;
        }

        // ===== Task Status Management =====

        public async Task<TaskItem> UpdateTaskStatusAsync(string taskId, TaskStatus newStatus, Guid updatedByUserId, string? notes = null)
        {
            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException($"Task with ID {taskId} not found");

            // Verify the user has permission to update this task
            if (!await _contactAccessService.CanAccessContactAsync(task.ContactId, updatedByUserId))
                throw new UnauthorizedAccessException("You do not have permission to update this task");

            // Update status
            task.Status = newStatus;
            task.UpdatedByUserId = updatedByUserId;
            task.UpdatedAt = DateTime.UtcNow;

            // Add status change to history
            var statusHistory = task.Metadata.TryGetValue("StatusHistory", out var history) && history is List<object> historyList
                ? historyList
                : new List<object>();

            statusHistory.Add(new 
            { 
                Status = newStatus.ToString(),
                ChangedAt = DateTime.UtcNow,
                ChangedBy = updatedByUserId,
                Notes = notes
            });

            task.Metadata["StatusHistory"] = statusHistory;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} status updated to {NewStatus} by user {UserId}", 
                taskId, newStatus, updatedByUserId);

            return task;
        }

        public async Task<TaskItem> CompleteTaskAsync(string taskId, Guid completedByUserId, string? notes = null)
        {
            return await UpdateTaskStatusAsync(taskId, TaskStatus.Completed, completedByUserId, notes);
        }

        // ===== NBA Integration =====

        public async Task<TaskItem> CreateTaskFromNbaRecommendationAsync(
            string recommendationId, 
            string contactId, 
            string title, 
            string description, 
            DateTime dueDate, 
            double confidenceScore,
            bool requiresApproval = false)
        {
            // This method is typically called by an agent, so we use a system user ID
            var systemUserId = _userContext.SystemUserId;

            // Create the task
            var task = new TaskItem
            {
                Title = title,
                Description = description,
                DueDate = dueDate,
                ContactId = contactId,
                Status = requiresApproval ? TaskStatus.PendingApproval : TaskStatus.Pending,
                Priority = TaskPriority.Medium,
                TaskType = "NBA_Recommendation",
                NbaRecommendationId = recommendationId,
                ConfidenceScore = confidenceScore,
                RequiresApproval = requiresApproval,
                CreatedByUserId = systemUserId
            };

            // Add NBA-specific metadata
            task.Metadata["NbaRecommendation"] = new
            {
                RecommendationId = recommendationId,
                GeneratedAt = DateTime.UtcNow,
                ConfidenceScore = confidenceScore,
                RequiresApproval = requiresApproval
            };

            return await CreateTaskAsync(task, systemUserId);
        }

        public async Task<bool> ValidateNbaTaskAsync(string taskId, bool isValid, string? reason, Guid validatedByUserId)
        {
            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException($"Task with ID {taskId} not found");

            // Verify the user has permission to validate this task
            if (!await _contactAccessService.CanAccessContactAsync(task.ContactId, validatedByUserId))
                throw new UnauthorizedAccessException("You do not have permission to validate this task");

            // Update validation status
            task.ValidationReason = reason;
            task.UpdatedByUserId = validatedByUserId;
            task.UpdatedAt = DateTime.UtcNow;

            // Add to validation history
            var validationHistory = task.Metadata.TryGetValue("ValidationHistory", out var history) && history is List<object> historyList
                ? historyList
                : new List<object>();

            validationHistory.Add(new 
            { 
                IsValid = isValid,
                ValidatedAt = DateTime.UtcNow,
                ValidatedBy = validatedByUserId,
                Reason = reason
            });

            task.Metadata["ValidationHistory"] = validationHistory;

            // If task is invalid, mark it as rejected
            if (!isValid)
            {
                task.Status = TaskStatus.Rejected;
            }
            // If task is valid and was pending approval, move to pending
            else if (task.Status == TaskStatus.PendingApproval)
            {
                task.Status = TaskStatus.Pending;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} validation status set to {IsValid} by user {UserId}", 
                taskId, isValid, validatedByUserId);

            return true;
        }

        // ===== Reporting =====

        public async Task<TaskStatistics> GetTaskStatisticsAsync(TaskStatisticsFilter filter, Guid requestingUserId)
        {
            var query = _context.TaskItems.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.OrganizationId))
                query = query.Where(t => t.OrganizationId == filter.OrganizationId);

            if (filter.UserId.HasValue)
                query = query.Where(t => t.AssignedToUserId == filter.UserId);

            if (filter.StartDate.HasValue)
                query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(t => t.CreatedAt <= filter.EndDate.Value);

            if (!string.IsNullOrEmpty(filter.TaskType))
                query = query.Where(t => t.TaskType == filter.TaskType);

            // Apply access control - user can only see stats for contacts they have access to
            var accessibleContactIds = await _contactAccessService.GetAccessibleContactIdsAsync(requestingUserId);
            query = query.Where(t => accessibleContactIds.Contains(t.ContactId));

            // Get all tasks that match the filter
            var tasks = await query.ToListAsync();

            // Calculate statistics
            var stats = new TaskStatistics
            {
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
                OverdueTasks = tasks.Count(t => t.DueDate < DateTime.UtcNow && 
                                              t.Status != TaskStatus.Completed && 
                                              t.Status != TaskStatus.Cancelled),
                DueSoonTasks = tasks.Count(t => t.DueDate > DateTime.UtcNow && 
                                              t.DueDate <= DateTime.UtcNow.AddDays(7) &&
                                              t.Status != TaskStatus.Completed && 
                                              t.Status != TaskStatus.Cancelled),
                NotStartedTasks = tasks.Count(t => t.Status == TaskStatus.NotStarted),
                InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress),
                TasksByStatus = tasks.GroupBy(t => t.Status.ToString())
                                   .ToDictionary(g => g.Key, g => g.Count()),
                TasksByPriority = tasks.GroupBy(t => t.Priority.ToString())
                                    .ToDictionary(g => g.Key, g => g.Count()),
                TasksByType = tasks.GroupBy(t => t.TaskType)
                                 .ToDictionary(g => g.Key, g => g.Count()),
                TasksByAssignee = tasks.Where(t => t.AssignedToUserId.HasValue)
                                    .GroupBy(t => t.AssignedToUser?.FullName ?? "Unassigned")
                                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Calculate average completion time for completed tasks
            var completedTasks = tasks.Where(t => t.Status == TaskStatus.Completed).ToList();
            if (completedTasks.Any())
            {
                stats.AverageCompletionTimeHours = completedTasks
                    .Where(t => t.CompletedAt.HasValue)
                    .Average(t => (t.CompletedAt.Value - t.CreatedAt).TotalHours);

                // Calculate completion trend (last 30 days)
                var startDate = DateTime.UtcNow.AddDays(-30).Date;
                var endDate = DateTime.UtcNow.Date;
                
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var nextDay = date.AddDays(1);
                    var count = completedTasks.Count(t => t.CompletedAt >= date && t.CompletedAt < nextDay);
                    stats.CompletionTrend[date] = count;
                }
            }

            return stats;
        }

        public async Task<IEnumerable<TaskItem>> GetOverdueTasksAsync(Guid userId, int limit = 50)
        {
            var now = DateTime.UtcNow;
            
            // Get tasks assigned to the user that are overdue
            var tasks = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId &&
                           t.DueDate < now &&
                           t.Status != TaskStatus.Completed &&
                           t.Status != TaskStatus.Cancelled &&
                           !t.IsDeleted)
                .OrderBy(t => t.DueDate)
                .Take(limit)
                .ToListAsync();

            // Filter to only include tasks for contacts the user has access to
            var accessibleContactIds = await _contactAccessService.GetAccessibleContactIdsAsync(userId);
            return tasks.Where(t => accessibleContactIds.Contains(t.ContactId));
        }

        public async Task<IEnumerable<TaskItem>> GetUpcomingTasksAsync(Guid userId, DateTime? endDate = null, int limit = 50)
        {
            var now = DateTime.UtcNow;
            var targetEndDate = endDate ?? now.AddDays(7);
            
            // Get tasks assigned to the user that are due soon
            var tasks = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId &&
                           t.DueDate > now &&
                           t.DueDate <= targetEndDate &&
                           t.Status != TaskStatus.Completed &&
                           t.Status != TaskStatus.Cancelled &&
                           !t.IsDeleted)
                .OrderBy(t => t.DueDate)
                .Take(limit)
                .ToListAsync();

            // Filter to only include tasks for contacts the user has access to
            var accessibleContactIds = await _contactAccessService.GetAccessibleContactIdsAsync(userId);
            return tasks.Where(t => accessibleContactIds.Contains(t.ContactId));
        }

        // ===== Helper Methods =====

        private IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, string? sortBy, bool sortDescending)
        {
            if (string.IsNullOrEmpty(sortBy))
                return sortDescending 
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate);

            return (sortBy.ToLower(), sortDescending) switch
            {
                ("duedate", false) => query.OrderBy(t => t.DueDate),
                ("duedate", true) => query.OrderByDescending(t => t.DueDate),
                ("priority", false) => query.OrderBy(t => t.Priority),
                ("priority", true) => query.OrderByDescending(t => t.Priority),
                ("status", false) => query.OrderBy(t => t.Status),
                ("status", true) => query.OrderByDescending(t => t.Status),
                ("createdat", false) => query.OrderBy(t => t.CreatedAt),
                ("createdat", true) => query.OrderByDescending(t => t.CreatedAt),
                _ => sortDescending 
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate)
            };
        }
    }
}
