using Emma.Data.Models;

namespace Emma.Core.Services;

/// <summary>
/// Provides tenant-aware context for industry-specific AI operations
/// </summary>
public interface ITenantContextService
{
    /// <summary>
    /// Get the current tenant organization
    /// </summary>
    Task<Organization> GetCurrentTenantAsync();
    
    /// <summary>
    /// Get industry-specific AI configuration for the current tenant
    /// </summary>
    Task<IndustryProfile> GetIndustryProfileAsync();
    
    /// <summary>
    /// Get specialized agents available for the current tenant's industry
    /// </summary>
    Task<List<string>> GetAvailableAgentsAsync();
    
    /// <summary>
    /// Get industry-specific resource types for the current tenant
    /// </summary>
    Task<List<string>> GetIndustryResourceTypesAsync();
    
    /// <summary>
    /// Get tenant-specific system prompt for AI interactions
    /// </summary>
    Task<string> GetSystemPromptAsync();
    
    /// <summary>
    /// Check if a specific agent is available for the current tenant
    /// </summary>
    Task<bool> IsAgentAvailableAsync(string agentType);
}
