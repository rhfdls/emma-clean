namespace Emma.Core.Models;

/// <summary>
/// Represents tenant context information for multi-tenant scenarios
/// </summary>
public class TenantContext
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string DatabaseConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Settings { get; set; } = new();
}
