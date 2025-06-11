using Emma.Core.Industry;

namespace Emma.Core.Models;

/// <summary>
/// Represents tenant context information for multi-tenant scenarios with industry specialization
/// </summary>
public class TenantContext
{
    // Multi-tenancy infrastructure
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string DatabaseConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Industry specialization
    public string IndustryCode { get; set; } = string.Empty;
    public IIndustryProfile? IndustryProfile { get; set; }
    
    // Feature management
    public HashSet<string> EnabledFeatures { get; set; } = new();
    
    // Configuration settings
    public Dictionary<string, object> Settings { get; set; } = new();
    
    // Explainability and audit
    public Guid AuditId { get; set; }
    public string Reason { get; set; } = string.Empty;
    
    // Compatibility properties
    public string Id => TenantId.ToString();
}
