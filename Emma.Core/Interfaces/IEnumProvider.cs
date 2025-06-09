using Emma.Core.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Provides dynamic enum management with industry-specific overrides and hot-reload support
/// Enables business users to configure dropdown values, categories, and classifications without code changes
/// </summary>
public interface IEnumProvider
{
    /// <summary>
    /// Get enum values for a specific enum type with industry and context overrides
    /// </summary>
    /// <param name="enumType">Type of enum (e.g., "ContactStatus", "PropertyType", "LeadSource")</param>
    /// <param name="context">Context for industry-specific overrides</param>
    /// <returns>List of enum values with display names and metadata</returns>
    Task<IEnumerable<EnumValue>> GetEnumValuesAsync(string enumType, EnumContext? context = null);

    /// <summary>
    /// Get a specific enum value by its key
    /// </summary>
    /// <param name="enumType">Type of enum</param>
    /// <param name="key">Enum value key</param>
    /// <param name="context">Context for industry-specific overrides</param>
    /// <returns>Enum value or null if not found</returns>
    Task<EnumValue?> GetEnumValueAsync(string enumType, string key, EnumContext? context = null);

    /// <summary>
    /// Get enum values formatted for UI dropdowns
    /// </summary>
    /// <param name="enumType">Type of enum</param>
    /// <param name="context">Context for industry-specific overrides</param>
    /// <returns>Dictionary of key-value pairs for UI binding</returns>
    Task<Dictionary<string, string>> GetEnumDropdownAsync(string enumType, EnumContext? context = null);

    /// <summary>
    /// Validate that an enum value exists and is valid for the given context
    /// </summary>
    /// <param name="enumType">Type of enum</param>
    /// <param name="key">Enum value key to validate</param>
    /// <param name="context">Context for validation</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateEnumValueAsync(string enumType, string key, EnumContext? context = null);

    /// <summary>
    /// Get enum metadata including descriptions, categories, and business rules
    /// </summary>
    /// <param name="enumType">Type of enum</param>
    /// <param name="context">Context for industry-specific metadata</param>
    /// <returns>Enum metadata</returns>
    Task<EnumMetadata?> GetEnumMetadataAsync(string enumType, EnumContext? context = null);

    /// <summary>
    /// Get all available enum types for the current context
    /// </summary>
    /// <param name="context">Context for filtering enum types</param>
    /// <returns>List of available enum types</returns>
    Task<IEnumerable<string>> GetAvailableEnumTypesAsync(EnumContext? context = null);

    /// <summary>
    /// Reload enum configuration from source (for hot-reload support)
    /// </summary>
    Task ReloadConfigurationAsync();

    /// <summary>
    /// Event fired when enum configuration changes (for hot-reload notifications)
    /// </summary>
    event EventHandler<EnumConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Event fired when enum configuration is reloaded
    /// </summary>
    event EventHandler<EnumConfigurationChangedEventArgs>? ConfigurationReloaded;

    // Versioning and Audit Methods

    /// <summary>
    /// Create a backup version of the current configuration
    /// </summary>
    /// <param name="description">Description of the version</param>
    /// <param name="createdBy">User creating the version</param>
    /// <param name="tags">Optional tags for the version</param>
    /// <returns>Version identifier</returns>
    Task<string> CreateVersionAsync(string description, string createdBy, List<string>? tags = null);

    /// <summary>
    /// Get all available versions
    /// </summary>
    /// <returns>List of version history entries</returns>
    Task<IEnumerable<VersionHistoryEntry>> GetVersionHistoryAsync();

    /// <summary>
    /// Rollback to a specific version
    /// </summary>
    /// <param name="version">Version to rollback to</param>
    /// <param name="rolledBackBy">User performing the rollback</param>
    /// <param name="reason">Reason for rollback</param>
    /// <returns>True if rollback successful</returns>
    Task<bool> RollbackToVersionAsync(string version, string rolledBackBy, string reason);

    /// <summary>
    /// Get change log entries
    /// </summary>
    /// <param name="enumType">Optional filter by enum type</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="changedBy">Optional filter by user</param>
    /// <returns>List of change log entries</returns>
    Task<IEnumerable<ChangeLogEntry>> GetChangeLogAsync(
        string? enumType = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        string? changedBy = null);

    /// <summary>
    /// Log a change to the audit trail
    /// </summary>
    /// <param name="changeEntry">Change log entry to record</param>
    Task LogChangeAsync(ChangeLogEntry changeEntry);

    /// <summary>
    /// Get configuration metadata including version info
    /// </summary>
    /// <returns>Configuration metadata</returns>
    Task<EnumConfigurationMetadata> GetConfigurationMetadataAsync();

    /// <summary>
    /// Compare two versions and get differences
    /// </summary>
    /// <param name="fromVersion">Source version</param>
    /// <param name="toVersion">Target version</param>
    /// <returns>Version comparison result</returns>
    Task<VersionComparisonResult> CompareVersionsAsync(string fromVersion, string toVersion);

    /// <summary>
    /// Submit configuration changes for approval
    /// </summary>
    /// <param name="submittedBy">User submitting for approval</param>
    /// <param name="description">Description of changes</param>
    /// <param name="requiredApprovers">List of required approvers</param>
    /// <returns>Approval request ID</returns>
    Task<string> SubmitForApprovalAsync(string submittedBy, string description, List<string> requiredApprovers);

    /// <summary>
    /// Approve or reject pending changes
    /// </summary>
    /// <param name="approvalId">Approval request ID</param>
    /// <param name="approver">User approving/rejecting</param>
    /// <param name="approved">True to approve, false to reject</param>
    /// <param name="comments">Approval/rejection comments</param>
    /// <returns>True if action successful</returns>
    Task<bool> ProcessApprovalAsync(string approvalId, string approver, bool approved, string? comments = null);

    /// <summary>
    /// Export configuration to file for backup/migration
    /// </summary>
    /// <param name="filePath">Target file path</param>
    /// <param name="includeHistory">Whether to include version history</param>
    /// <returns>True if export successful</returns>
    Task<bool> ExportConfigurationAsync(string filePath, bool includeHistory = false);

    /// <summary>
    /// Import configuration from file
    /// </summary>
    /// <param name="filePath">Source file path</param>
    /// <param name="importedBy">User performing the import</param>
    /// <param name="mergeStrategy">How to handle conflicts</param>
    /// <returns>Import result with details</returns>
    Task<ImportResult> ImportConfigurationAsync(string filePath, string importedBy, MergeStrategy mergeStrategy = MergeStrategy.Replace);
}

/// <summary>
/// Context for enum resolution with industry and agent-specific overrides
/// </summary>
public class EnumContext
{
    public string? IndustryCode { get; set; }
    public string? AgentType { get; set; }
    public string? TenantId { get; set; }
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
}

/// <summary>
/// Event arguments for enum configuration changes
/// </summary>
public class EnumConfigurationChangedEventArgs : EventArgs
{
    public string? EnumType { get; set; }
    public string? IndustryCode { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
