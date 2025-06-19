using Emma.Core.Models;
using Emma.Core.Industry;

namespace Emma.Core.Interfaces;

/// <summary>
/// Provides dynamic prompt management for AI agents
/// Enables business-configurable prompts without code changes
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Get system prompt for a specific agent and industry
    /// </summary>
    Task<string> GetSystemPromptAsync(string agentType, IIndustryProfile industryProfile);
    
    /// <summary>
    /// Build a prompt from template with dynamic context
    /// </summary>
    Task<string> BuildPromptAsync(string templateName, Dictionary<string, object> context);
    
    /// <summary>
    /// Get all available prompt templates for an agent
    /// </summary>
    Task<Dictionary<string, string>> GetAgentTemplatesAsync(string agentType);
    
    /// <summary>
    /// Validate prompt template syntax and placeholders
    /// </summary>
    Task<PromptValidationResult> ValidatePromptAsync(string templateName, string promptContent);
    
    /// <summary>
    /// Reload prompts from external configuration (hot-reload capability)
    /// </summary>
    Task ReloadPromptsAsync();

    // ===== VERSIONING AND AUDIT CAPABILITIES =====
    
    /// <summary>
    /// Create a versioned backup of the current prompt configuration
    /// </summary>
    Task<string> CreateVersionAsync(string description, string createdBy, List<string>? tags = null);
    
    /// <summary>
    /// Rollback to a specific version of the prompt configuration
    /// </summary>
    Task<bool> RollbackToVersionAsync(string version, string rolledBackBy, string reason);
    
    /// <summary>
    /// Get version history of prompt configuration changes
    /// </summary>
    Task<IEnumerable<PromptVersionHistoryEntry>> GetVersionHistoryAsync();
    
    /// <summary>
    /// Compare two versions and return differences
    /// </summary>
    Task<PromptVersionComparisonResult> CompareVersionsAsync(string version1, string version2);
    
    /// <summary>
    /// Get filtered change log entries for audit trail
    /// </summary>
    Task<IEnumerable<PromptChangeLogEntry>> GetChangeLogAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? agentType = null,
        string? changedBy = null,
        PromptChangeType? changeType = null);
    
    /// <summary>
    /// Export prompt configuration to file
    /// </summary>
    Task<string> ExportConfigurationAsync(string? version = null);
    
    /// <summary>
    /// Import prompt configuration from file
    /// </summary>
    Task<bool> ImportConfigurationAsync(string importFilePath, string importedBy, PromptMergeStrategy mergeStrategy = PromptMergeStrategy.Replace);
    
    /// <summary>
    /// Get current configuration metadata
    /// </summary>
    Task<PromptMetadata?> GetConfigurationMetadataAsync();
}

/// <summary>
/// Result of prompt validation
/// </summary>
public class PromptValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> RequiredPlaceholders { get; set; } = new();
}
