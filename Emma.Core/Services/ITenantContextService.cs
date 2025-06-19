using Emma.Core.Models;
using Emma.Core.Industry;

namespace Emma.Core.Services;

/// <summary>
/// Service for managing tenant context in multi-tenant scenarios
/// </summary>
public interface ITenantContextService
{
    /// <summary>
    /// Get the current tenant context
    /// </summary>
    Task<TenantContext> GetCurrentTenantAsync();
    
    /// <summary>
    /// Get the industry profile for the current tenant
    /// </summary>
    Task<IIndustryProfile> GetIndustryProfileAsync();
    
    /// <summary>
    /// Validate tenant access for the given tenant ID
    /// </summary>
    Task<bool> ValidateTenantAccessAsync(Guid tenantId);
}
