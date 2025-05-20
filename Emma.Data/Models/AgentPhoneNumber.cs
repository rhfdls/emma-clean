namespace Emma.Data.Models;

public class AgentPhoneNumber
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; }
    public Guid AgentId { get; set; }
    public Agent Agent { get; set; }
}
