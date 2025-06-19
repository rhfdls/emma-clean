namespace Emma.Models.Models;

public class RelatedEntity
{
    public string Type { get; set; } = string.Empty; // deal|property|listing|task|other
    public Guid Id { get; set; }
}
