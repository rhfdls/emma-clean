namespace Emma.Data.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ClientId { get; set; }
    public string ClientFirstName { get; set; } = string.Empty;
    public string ClientLastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid OrganizationId { get; init; }
    public Guid AgentId { get; init; }
    public Organization Organization { get; set; } = null!;
    public List<Message> Messages { get; set; } = new();
    public ConversationSummary? Summary { get; set; }
}
