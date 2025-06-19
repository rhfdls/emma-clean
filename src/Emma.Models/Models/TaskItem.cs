using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Emma.Models.Enums;

namespace Emma.Models.Models
{
    /// <summary>
    /// Represents a task or to-do item that can be assigned to users or related to other entities.
    /// </summary>
    public class TaskItem : BaseEntity
    {

        /// <summary>
        /// Gets or sets the title of the task.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the detailed description of the task.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the status of the task.
        /// </summary>
        public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.NotStarted;

        /// <summary>
        /// Gets or sets the priority of the task.
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        /// <summary>
        /// Gets or sets the date and time when the task is due.
        /// </summary>
        public DateTimeOffset? DueDate { get; set; }


        /// <summary>
        /// Gets or sets the date and time when the task was completed.
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created the task.
        /// </summary>
        public Guid? CreatedById { get; set; }

        /// <summary>
        /// Gets or sets the user who created the task.
        /// </summary>
        [ForeignKey(nameof(CreatedById))]
        public virtual User CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user the task is assigned to.
        /// </summary>
        public Guid? AssignedToId { get; set; }

        /// <summary>
        /// Gets or sets the user the task is assigned to.
        /// </summary>
        [ForeignKey(nameof(AssignedToId))]
        public virtual User AssignedTo { get; set; }

        /// <summary>
        /// Gets or sets the ID of the related contact, if any.
        /// </summary>
        public Guid? ContactId { get; set; }

        /// <summary>
        /// Gets or sets the related contact, if any.
        /// </summary>
        [ForeignKey(nameof(ContactId))]
        public virtual Contact Contact { get; set; }

        /// <summary>
        /// Gets or sets the ID of the related interaction, if any.
        /// </summary>
        public Guid? InteractionId { get; set; }

        /// <summary>
        /// Gets or sets the related interaction, if any.
        /// </summary>
        [ForeignKey(nameof(InteractionId))]
        public virtual Interaction Interaction { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the task was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the task was last updated.
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets any additional metadata for the task in JSON format.
        /// </summary>
        public string Metadata { get; set; }
    }
}
