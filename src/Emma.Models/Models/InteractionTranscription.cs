using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Explicitly links an audio-based interaction to its transcription.
/// </summary>
public class InteractionTranscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InteractionId { get; set; }
    [ForeignKey(nameof(InteractionId))]
    public virtual Interaction Interaction { get; set; } = null!;
    public Guid TranscriptionId { get; set; }
    [ForeignKey(nameof(TranscriptionId))]
    public virtual Transcription Transcription { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
