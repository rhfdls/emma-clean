using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Emma.Models.Enums;

namespace Emma.Core.Models
{
    /// <summary>
    /// Tracks the history of relationship state changes for a contact
    /// </summary>
    public class ContactStateHistory
    {
        /// <summary>
        /// Unique identifier for the state history record
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// The contact this state change applies to
        /// </summary>
        [Required]
        public Guid ContactId { get; set; }
        
        /// <summary>
        /// Navigation property to the contact
        /// </summary>
        [ForeignKey(nameof(ContactId))]
        public virtual Contact Contact { get; set; }
        
        /// <summary>
        /// The previous relationship state
        /// </summary>
        public RelationshipState? PreviousState { get; set; }
        
        /// <summary>
        /// The new relationship state
        /// </summary>
        [Required]
        public RelationshipState NewState { get; set; }
        
        /// <summary>
        /// The user who initiated the state change
        /// </summary>
        [Required]
        public Guid ChangedByUserId { get; set; }
        
        /// <summary>
        /// Navigation property to the user who made the change
        /// </summary>
        [ForeignKey(nameof(ChangedByUserId))]
        public virtual User ChangedByUser { get; set; }
        
        /// <summary>
        /// The date and time when the state was changed
        /// </summary>
        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Optional reason or notes about the state change
        /// </summary>
        [MaxLength(1000)]
        public string? Reason { get; set; }
        
        /// <summary>
        /// Optional reference to the interaction that triggered this state change
        /// </summary>
        public Guid? TriggeringInteractionId { get; set; }
        
        /// <summary>
        /// Navigation property to the triggering interaction (if any)
        /// </summary>
        [ForeignKey(nameof(TriggeringInteractionId))]
        public virtual Interaction? TriggeringInteraction { get; set; }
        
        /// <summary>
        /// Any additional metadata about the state change
        /// </summary>
        [Column(TypeName = "jsonb")]
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
