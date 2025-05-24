namespace Emma.Data.Models;

public class ConversationSummary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public double? QualityScore { get; set; }
    public string? SummaryText { get; set; }
}
