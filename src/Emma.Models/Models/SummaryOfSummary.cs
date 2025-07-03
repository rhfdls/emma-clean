using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// AI-generated summary-of-summaries, linked to an interaction or contact, managed as a standalone entity.
/// </summary>
public class SummaryOfSummary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? InteractionId { get; set; }
    [ForeignKey(nameof(InteractionId))]
    public virtual Interaction? Interaction { get; set; }
    public Guid? ContactId { get; set; }
    [ForeignKey(nameof(ContactId))]
    public virtual Contact? Contact { get; set; }
    public string SummaryText { get; set; } = string.Empty;
    public string? SourceAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdated { get; set; }
}
