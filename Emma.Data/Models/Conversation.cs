namespace Emma.Data.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContactId { get; set; } // Link to Contact
    public Dictionary<string, string>? ExternalIds { get; set; } // e.g., FUB, HubSpot, Salesforce
    public string Type { get; set; } = string.Empty; // call|email|sms|meeting|note|task|other
    public string Direction { get; set; } = string.Empty; // inbound|outbound|system
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid AgentId { get; set; }
    public string? Content { get; set; } // Message body, note, etc.
    public string Channel { get; set; } = string.Empty; // twilio|email|gog|crm|other
    public string Status { get; set; } = string.Empty; // completed|pending|failed|scheduled
    public List<RelatedEntity>? RelatedEntities { get; set; }
    public List<string> Tags { get; set; } = new(); // Privacy/business logic tags (CRM, PERSONAL, PRIVATE, etc.)
    public Dictionary<string, string>? CustomFields { get; set; }
    public List<Message> Messages { get; set; } = new();
}



