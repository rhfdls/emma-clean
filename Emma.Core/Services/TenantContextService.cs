using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Industry;

namespace Emma.Core.Services
{
    /// <summary>
    /// Service for managing tenant context in multi-tenant scenarios
    /// Currently simplified for single-tenant demo
    /// </summary>
    public class TenantContextService : ITenantContextService
    {
        private readonly IIndustryProfileService _industryProfileService;

        public TenantContextService(IIndustryProfileService industryProfileService)
        {
            _industryProfileService = industryProfileService;
        }

        public async Task<TenantContext> GetCurrentTenantAsync()
        {
            // For demo purposes, return a default tenant
            var industryProfile = await GetIndustryProfileAsync();
            
            var defaultTenant = new TenantContext
            {
                TenantId = Guid.NewGuid(),
                TenantName = "Demo Tenant",
                DatabaseConnectionString = "Demo Connection",
                IsActive = true,
                IndustryCode = "RealEstate",
                IndustryProfile = industryProfile,
                Settings = new Dictionary<string, object>(),
                EnabledFeatures = new HashSet<string>(),
                AuditId = Guid.NewGuid(),
                Reason = "Default tenant context for demo"
            };

            return defaultTenant;
        }

        public async Task<IIndustryProfile> GetIndustryProfileAsync()
        {
            // Dynamically determine the industry based on the current tenant's configuration
            var tenantContext = await GetCurrentTenantAsync();
            var industryCode = tenantContext.IndustryCode; 
            var profile = await _industryProfileService.GetProfileAsync(industryCode);
            return profile ?? throw new InvalidOperationException($"{industryCode} profile not found");
        }

        public Task<bool> ValidateTenantAccessAsync(Guid tenantId)
        {
            // For demo purposes, always return true
            // In a real multi-tenant scenario, this would validate tenant access
            return Task.FromResult(true);
        }
    }
}
