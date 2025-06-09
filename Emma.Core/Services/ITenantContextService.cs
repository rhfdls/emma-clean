using Emma.Core.Models;
using Emma.Core.Industry;

namespace Emma.Core.Services;

/// <summary>
/// Provides tenant-aware context for multi-tenant scenarios
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
    /// Validate if a user has access to a specific tenant
    /// </summary>
    Task<bool> ValidateTenantAccessAsync(Guid tenantId, Guid userId);
}
