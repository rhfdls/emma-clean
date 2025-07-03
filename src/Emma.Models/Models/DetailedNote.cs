using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// AI-generated detailed note linked to an interaction, but managed as a standalone entity.
/// </summary>
public class DetailedNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InteractionId { get; set; }
    [ForeignKey(nameof(InteractionId))]
    public virtual Interaction Interaction { get; set; } = null!;
    public string NoteText { get; set; } = string.Empty;
    public string? SourceAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
}
