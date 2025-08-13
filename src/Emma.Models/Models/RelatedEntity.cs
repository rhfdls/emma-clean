namespace Emma.Models.Models;

public class RelatedEntity
{
    public string Type { get; set; } = string.Empty; // deal|property|listing|task|other
    public Guid Id { get; set; }
    public string? ExternalId { get; set; } // Optional external system reference
    public string? Name { get; set; }
}

