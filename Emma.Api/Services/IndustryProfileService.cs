using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Industry;
using Emma.Core.Industry.Profiles;
using Emma.Core.Interfaces;
using Emma.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Services
{
    /// <summary>
    /// Service for managing industry profiles and configurations
    /// </summary>
    public class IndustryProfileService : IIndustryProfileService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<IndustryProfileService> _logger;
        private readonly Dictionary<string, IIndustryProfile> _profiles;

        public IndustryProfileService(AppDbContext dbContext, ILogger<IndustryProfileService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize available industry profiles
            _profiles = new Dictionary<string, IIndustryProfile>
            {
                { "RealEstate", new RealEstateProfile() },
                { "Mortgage", new MortgageProfile() },
                { "Financial", new FinancialAdvisorProfile() }
            };
        }

        /// <summary>
        /// Get industry profile by code
        /// </summary>
        public async Task<IIndustryProfile?> GetProfileAsync(string industryCode)
        {
            if (string.IsNullOrWhiteSpace(industryCode))
                return null;

            _profiles.TryGetValue(industryCode, out var profile);
            return await Task.FromResult(profile);
        }

        /// <summary>
        /// Get industry profile for an organization
        /// </summary>
        public async Task<IIndustryProfile> GetProfileForOrganizationAsync(Guid organizationId)
        {
            try
            {
                var organization = await _dbContext.Organizations
                    .FirstOrDefaultAsync(o => o.Id == organizationId);

                if (organization?.IndustryCode != null && _profiles.TryGetValue(organization.IndustryCode, out var profile))
                {
                    _logger.LogDebug("Using industry profile {IndustryCode} for organization {OrganizationId}", 
                        organization.IndustryCode, organizationId);
                    return profile;
                }

                // Default to Real Estate if no industry configured
                _logger.LogDebug("No industry configured for organization {OrganizationId}, defaulting to RealEstate", organizationId);
                return _profiles["RealEstate"];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting industry profile for organization {OrganizationId}, defaulting to RealEstate", organizationId);
                return _profiles["RealEstate"];
            }
        }

        /// <summary>
        /// Get all available industry profiles
        /// </summary>
        public async Task<List<IIndustryProfile>> GetAvailableProfilesAsync()
        {
            return await Task.FromResult(_profiles.Values.ToList());
        }

        /// <summary>
        /// Set industry profile for an organization
        /// </summary>
        public async Task SetOrganizationIndustryAsync(Guid organizationId, string industryCode)
        {
            if (!_profiles.ContainsKey(industryCode))
            {
                throw new ArgumentException($"Industry code '{industryCode}' is not supported", nameof(industryCode));
            }

            var organization = await _dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                throw new InvalidOperationException($"Organization with ID {organizationId} not found");
            }

            organization.IndustryCode = industryCode;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Set industry code {IndustryCode} for organization {OrganizationId}", 
                industryCode, organizationId);
        }

        /// <summary>
        /// Build AI prompt with industry-specific context
        /// </summary>
        public async Task<string> BuildPromptAsync(string industryCode, string promptType, Dictionary<string, object>? parameters = null)
        {
            var profile = await GetProfileAsync(industryCode);
            if (profile == null)
            {
                throw new ArgumentException($"Industry code '{industryCode}' not found", nameof(industryCode));
            }

            var template = promptType.ToLower() switch
            {
                "system" => profile.PromptTemplates.SystemPrompt,
                "context" => profile.PromptTemplates.ContextPrompt,
                "query" => profile.PromptTemplates.QueryTemplates.GetValueOrDefault("default", ""),
                "action" => profile.PromptTemplates.ActionPrompts.GetValueOrDefault("default", ""),
                _ => throw new ArgumentException($"Prompt type '{promptType}' not supported", nameof(promptType))
            };

            // Simple parameter substitution
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    template = template.Replace($"{{{param.Key}}}", param.Value?.ToString() ?? "");
                }
            }

            return template;
        }
    }
}
