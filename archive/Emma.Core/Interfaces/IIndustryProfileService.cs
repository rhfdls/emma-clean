using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emma.Core.Industry;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Service for managing industry profiles and configurations
    /// </summary>
    public interface IIndustryProfileService
    {
        /// <summary>
        /// Get industry profile by code
        /// </summary>
        /// <param name="industryCode">Industry identifier</param>
        /// <returns>Industry profile or null if not found</returns>
        Task<IIndustryProfile?> GetProfileAsync(string industryCode);

        /// <summary>
        /// Get industry profile for an organization
        /// </summary>
        /// <param name="organizationId">Organization identifier</param>
        /// <returns>Industry profile or default if not configured</returns>
        Task<IIndustryProfile> GetProfileForOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get all available industry profiles
        /// </summary>
        /// <returns>List of available industry profiles</returns>
        Task<List<IIndustryProfile>> GetAvailableProfilesAsync();

        /// <summary>
        /// Set industry profile for an organization
        /// </summary>
        /// <param name="organizationId">Organization identifier</param>
        /// <param name="industryCode">Industry code to assign</param>
        Task SetOrganizationIndustryAsync(Guid organizationId, string industryCode);

        /// <summary>
        /// Build AI prompt with industry-specific context
        /// </summary>
        /// <param name="industryCode">Industry code</param>
        /// <param name="promptType">Type of prompt (system, context, query, action)</param>
        /// <param name="parameters">Parameters to substitute in template</param>
        /// <returns>Formatted prompt string</returns>
        Task<string> BuildPromptAsync(string industryCode, string promptType, Dictionary<string, object>? parameters = null);
    }
}
