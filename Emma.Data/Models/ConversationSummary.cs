namespace Emma.Data.Models;

public class ConversationSummary
{
    public Guid ConversationId { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InteractionId { get; set; }
    public Interaction? Interaction { get; set; }
    public double? QualityScore { get; set; }
    public string? SummaryText { get; set; }
}
