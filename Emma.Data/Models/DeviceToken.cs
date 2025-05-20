namespace Emma.Data.Models;

public class DeviceToken
{
    public Guid AgentId { get; set; }
    public Guid DeviceId { get; set; }
    public Agent Agent { get; set; }
    public string Token { get; set; } = string.Empty;
}
