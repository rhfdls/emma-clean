namespace Emma.Models.Models;

public class ConversationSummary
{
    // Removed ConversationId as part of refactor
    // public Guid ConversationId { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InteractionId { get; set; }
    public Interaction? Interaction { get; set; }
    public double? QualityScore { get; set; }
    public string? SummaryText { get; set; }
}
