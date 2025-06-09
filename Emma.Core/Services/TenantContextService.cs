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

        public Task<TenantContext> GetCurrentTenantAsync()
        {
            // For demo purposes, return a default tenant
            var defaultTenant = new TenantContext
            {
                TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                TenantName = "Demo Organization",
                DatabaseConnectionString = "DefaultConnection",
                IsActive = true
            };

            return Task.FromResult(defaultTenant);
        }

        public async Task<IIndustryProfile> GetIndustryProfileAsync()
        {
            // For demo purposes, get the real estate profile
            // In a real multi-tenant scenario, this would be based on the current tenant's industry
            var profile = await _industryProfileService.GetProfileAsync("RealEstate");
            return profile ?? throw new InvalidOperationException("RealEstate profile not found");
        }

        public Task<bool> ValidateTenantAccessAsync(Guid tenantId, Guid userId)
        {
            // For demo purposes, always allow access
            return Task.FromResult(true);
        }
    }
}
