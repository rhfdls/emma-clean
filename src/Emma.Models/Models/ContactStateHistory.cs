using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Tracks the history of relationship state transitions for a contact.
/// </summary>
public class ContactStateHistory : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the contact this state history belongs to.
    /// </summary>
    [Required]
    public Guid ContactId { get; set; }

    /// <summary>
    /// Gets or sets the previous relationship state.
    /// </summary>
    [Required]
    public RelationshipState FromState { get; set; }

    /// <summary>
    /// Gets or sets the new relationship state after the transition.
    /// </summary>
    [Required]
    public RelationshipState ToState { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the state transition occurred.
    /// </summary>
    [Required]
    public DateTime TransitionDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the reason or notes for the state transition.
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who initiated the state transition.
    /// </summary>
    public Guid? ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets any additional metadata about the state transition.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the contact this state history belongs to.
    /// </summary>
    [ForeignKey(nameof(ContactId))]
    public virtual Contact? Contact { get; set; }

    /// <summary>
    /// Gets or sets the user who initiated the state transition.
    /// </summary>
    [ForeignKey(nameof(ChangedByUserId))]
    public virtual User? ChangedByUser { get; set; }
}
