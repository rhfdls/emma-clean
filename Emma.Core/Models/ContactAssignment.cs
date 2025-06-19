using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Core.Models
{
    /// <summary>
    /// Tracks the assignment of contacts to users for workflow management
    /// </summary>
    public class ContactAssignment
    {
        /// <summary>
        /// Unique identifier for the assignment
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// The contact being assigned
        /// </summary>
        [Required]
        public Guid ContactId { get; set; }
        
        /// <summary>
        /// Navigation property to the assigned contact
        /// </summary>
        [ForeignKey(nameof(ContactId))]
        public virtual Contact Contact { get; set; }
        
        /// <summary>
        /// The user who is being assigned the contact
        /// </summary>
        [Required]
        public Guid AssignedToUserId { get; set; }
        
        /// <summary>
        /// Navigation property to the assigned user
        /// </summary>
        [ForeignKey(nameof(AssignedToUserId))]
        public virtual User AssignedToUser { get; set; }
        
        /// <summary>
        /// The user who made the assignment
        /// </summary>
        [Required]
        public Guid AssignedByUserId { get; set; }
        
        /// <summary>
        /// Navigation property to the user who made the assignment
        /// </summary>
        [ForeignKey(nameof(AssignedByUserId))]
        public virtual User AssignedByUser { get; set; }
        
        /// <summary>
        /// The date and time when the assignment was made
        /// </summary>
        [Required]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Optional end date for temporary assignments
        /// </summary>
        public DateTime? AssignmentEndDate { get; set; }
        
        /// <summary>
        /// The previous user the contact was assigned to (if any)
        /// </summary>
        public Guid? PreviousOwnerId { get; set; }
        
        /// <summary>
        /// Navigation property to the previous owner (if any)
        /// </summary>
        [ForeignKey(nameof(PreviousOwnerId))]
        public virtual User? PreviousOwner { get; set; }
        
        /// <summary>
        /// Optional notes about the assignment
        /// </summary>
        [MaxLength(2000)]
        public string? Notes { get; set; }
        
        /// <summary>
        /// Whether this is the current active assignment
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// The date and time when the assignment was ended (if applicable)
        /// </summary>
        public DateTime? EndedAt { get; set; }
        
        /// <summary>
        /// The reason the assignment was ended (if applicable)
        /// </summary>
        [MaxLength(500)]
        public string? EndReason { get; set; }
        
        /// <summary>
        /// Any additional metadata about the assignment
        /// </summary>
        [Column(TypeName = "jsonb")]
        public Dictionary<string, object>? Metadata { get; set; }
        
        /// <summary>
        /// Ends this assignment with the specified reason
        /// </summary>
        public void EndAssignment(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required to end an assignment", nameof(reason));
                
            IsActive = false;
            EndedAt = DateTime.UtcNow;
            EndReason = reason;
        }
        
        /// <summary>
        /// Extends the assignment end date, or makes it permanent if null is provided
        /// </summary>
        public void ExtendAssignment(DateTime? newEndDate = null)
        {
            AssignmentEndDate = newEndDate;
        }
    }
}
