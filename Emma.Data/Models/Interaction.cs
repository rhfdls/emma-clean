namespace Emma.Data.Models;

public class Interaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContactId { get; set; } // Link to Contact (legacy)
    public Guid ClientId { get => ContactId; set => ContactId = value; } // Alias for backward compatibility
    public Guid OrganizationId { get; set; }
    public string ClientFirstName { get; set; } = string.Empty;
    public string ClientLastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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


