using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Emma.Models.Enums;

namespace Emma.Models.Models
{
    /// <summary>
    /// Represents a task item in the system, which can be assigned to users
    /// and associated with contacts and organizations.
    /// </summary>
    [Table("TaskItems")]
    public class TaskItem
    {
        // ===== CORE PROPERTIES =====
        
        /// <summary>
        /// Unique identifier for the task.
        /// </summary>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// For backward compatibility with legacy systems.
        /// </summary>
        [Obsolete("Use Id property instead. This will be removed in a future version.")]
        public string TaskId { get; set; } = string.Empty;
        
        /// <summary>
        /// The contact associated with this task.
        /// </summary>
        [Required(ErrorMessage = "ContactId is required")]
        public string ContactId { get; set; } = string.Empty;
        
        /// <summary>
        /// Navigation property for the associated contact.
        /// </summary>
        [ForeignKey(nameof(ContactId))]
        public virtual Contact? Contact { get; set; }
        
        /// <summary>
        /// The organization that owns this task.
        /// </summary>
        [Required(ErrorMessage = "OrganizationId is required")]
        public string OrganizationId { get; set; } = string.Empty;
        
        /// <summary>
        /// Navigation property for the organization.
        /// </summary>
        [ForeignKey(nameof(OrganizationId))]
        public virtual Organization? Organization { get; set; }
        
        /// <summary>
        /// Cached contact name for display purposes.
        /// </summary>
        [MaxLength(200)]
        public string ContactName { get; set; } = string.Empty;
        
        /// <summary>
        /// The title of the task.
        /// </summary>
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Detailed description of the task.
        /// </summary>
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }
        
        /// <summary>
        /// The due date and time for the task.
        /// </summary>
        [Required(ErrorMessage = "DueDate is required")]
        public DateTime DueDate { get; set; }
        
        /// <summary>
        /// The priority level of the task.
        /// </summary>
        [Required(ErrorMessage = "Priority is required")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        
        /// <summary>
        /// The current status of the task.
        /// </summary>
        [Required(ErrorMessage = "Status is required")]
        public Emma.Models.Enums.TaskStatus Status { get; set; } = Emma.Models.Enums.TaskStatus.Pending;
        
        /// <summary>
        /// The type of task (e.g., FollowUp, Showing, Paperwork).
        /// </summary>
        [Required(ErrorMessage = "TaskType is required")]
        [MaxLength(100, ErrorMessage = "TaskType cannot exceed 100 characters")]
        public string TaskType { get; set; } = string.Empty;
        
        // ===== USER ASSIGNMENT =====
        
        /// <summary>
        /// The ID of the user this task is assigned to.
        /// </summary>
        public Guid? AssignedToUserId { get; set; }
        
        /// <summary>
        /// Navigation property for the assigned user.
        /// </summary>
        [ForeignKey(nameof(AssignedToUserId))]
        public virtual User? AssignedToUser { get; set; }
        
        // ===== AUDIT FIELDS =====
        
        /// <summary>
        /// The date and time when the task was created.
        /// </summary>
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// The date and time when the task was last updated.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// The ID of the user who created the task.
        /// </summary>
        [Required(ErrorMessage = "CreatedByUserId is required")]
        public Guid CreatedByUserId { get; set; }
        
        /// <summary>
        /// Navigation property for the user who created the task.
        /// </summary>
        [ForeignKey(nameof(CreatedByUserId))]
        public virtual User CreatedByUser { get; set; } = null!;
        
        /// <summary>
        /// The ID of the user who last updated the task.
        /// </summary>
        public Guid? UpdatedByUserId { get; set; }
        
        /// <summary>
        /// Navigation property for the user who last updated the task.
        /// </summary>
        [ForeignKey(nameof(UpdatedByUserId))]
        public virtual User? UpdatedByUser { get; set; }
        
        // ===== NBA INTEGRATION =====
        
        /// <summary>
        /// The ID of the NBA recommendation that created this task, if applicable.
        /// </summary>
        [MaxLength(100, ErrorMessage = "NBA recommendation ID cannot exceed 100 characters")]
        public string? NbaRecommendationId { get; set; }
        /// <summary>
        /// The confidence score of the NBA recommendation (0-1).
        /// </summary>
        [Range(0, 1, ErrorMessage = "Confidence score must be between 0 and 1")]
        public double? ConfidenceScore { get; set; }
        
        /// <summary>
        /// The reason for validation or rejection of the NBA recommendation.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Validation reason cannot exceed 500 characters")]
        public string? ValidationReason { get; set; }
        
        /// <summary>
        /// Indicates whether this task requires approval before being acted upon.
        /// </summary>
        public bool RequiresApproval { get; set; }
        
        /// <summary>
        /// The ID of the approval request, if this task requires approval.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Approval request ID cannot exceed 100 characters")]
        public string? ApprovalRequestId { get; set; }
        
        // ===== METADATA =====
        
        /// <summary>
        /// Custom metadata for extensibility.
        /// </summary>
        [Column(TypeName = "jsonb")]
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        // ===== HELPER METHODS =====
        
        /// <summary>
        /// Ensures backward compatibility with older versions of the model.
        /// </summary>
        [Obsolete("This method is maintained for backward compatibility and will be removed in a future version.")]
        public void EnsureCompatibility()
        {
            if (string.IsNullOrEmpty(TaskId) && !string.IsNullOrEmpty(Id))
            {
                TaskId = Id;
            }
            else if (string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(TaskId))
            {
                Id = TaskId;
            }
            
            // Ensure required fields have defaults
            if (CreatedAt == default)
            {
                CreatedAt = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Updates the task status and records the user who made the change.
        /// </summary>
        /// <param name="newStatus">The new status to set</param>
        /// <param name="updatedBy">The user making the change</param>
        /// <param name="notes">Optional notes about the status change</param>
        public void UpdateStatus(Emma.Models.Enums.TaskStatus newStatus, User updatedBy, string? notes = null)
        {
            if (updatedBy == null)
                throw new ArgumentNullException(nameof(updatedBy));
                
            Status = newStatus;
            UpdatedByUserId = updatedBy.Id;
            UpdatedAt = DateTime.UtcNow;
            
            // Add status change to metadata history
            var statusHistory = Metadata.TryGetValue("StatusHistory", out var history) && history is List<object> historyList
                ? historyList
                : new List<object>();
                
            statusHistory.Add(new 
            { 
                Status = newStatus.ToString(),
                ChangedAt = DateTime.UtcNow,
                ChangedBy = updatedBy.Id,
                Notes = notes
            });
            
            Metadata["StatusHistory"] = statusHistory;
        }
        
        /// <summary>
        /// Assigns the task to a user.
        /// </summary>
        /// <param name="user">The user to assign the task to</param>
        /// <param name="assignedBy">The user making the assignment</param>
        public void AssignTo(User user, User assignedBy)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
                
            if (assignedBy == null)
                throw new ArgumentNullException(nameof(assignedBy));
                
            AssignedToUserId = user.Id;
            AssignedToUser = user;
            
            // Update audit fields
            UpdatedByUserId = assignedBy.Id;
            UpdatedAt = DateTime.UtcNow;
            
            // Add to assignment history
            var assignmentHistory = Metadata.TryGetValue("AssignmentHistory", out var history) && history is List<object> historyList
                ? historyList
                : new List<object>();
                
            assignmentHistory.Add(new 
            { 
                AssignedToUserId = user.Id,
                AssignedByUserId = assignedBy.Id,
                AssignedAt = DateTime.UtcNow
            });
            
            Metadata["AssignmentHistory"] = assignmentHistory;
        }
        
        /// <summary>
        /// Validates the task data.
        /// </summary>
        /// <returns>A tuple indicating whether the task is valid and any error message.</returns>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(Title))
                return (false, "Title is required");
                
            if (DueDate < DateTime.UtcNow.AddYears(-1))
                return (false, "Due date cannot be more than 1 year in the past");
                
            if (DueDate > DateTime.UtcNow.AddYears(5))
                return (false, "Due date cannot be more than 5 years in the future");
                
            if (ConfidenceScore.HasValue && (ConfidenceScore < 0 || ConfidenceScore > 1))
                return (false, "Confidence score must be between 0 and 1");
                
            if (string.IsNullOrWhiteSpace(ContactId))
                return (false, "Contact ID is required");
                
            if (string.IsNullOrWhiteSpace(OrganizationId))
                return (false, "Organization ID is required");
                
            if (CreatedByUserId == Guid.Empty)
                return (false, "Created by user ID is required");
                
            return (true, null);
        }
    }
}
