using System.Text.Json.Serialization;

namespace Emma.Core.Models;

/// <summary>
/// Root configuration for dynamic enum management
/// Supports multi-level overrides: Global → Industry → Agent-specific
/// </summary>
public class EnumConfiguration
{
    /// <summary>
    /// Global enum definitions available across all industries and agents
    /// </summary>
    [JsonPropertyName("globalEnums")]
    public Dictionary<string, EnumDefinition> GlobalEnums { get; set; } = new();

    /// <summary>
    /// Industry-specific enum overrides and additions
    /// </summary>
    [JsonPropertyName("industries")]
    public Dictionary<string, IndustryEnumOverrides> Industries { get; set; } = new();

    /// <summary>
    /// Agent-specific enum customizations
    /// </summary>
    [JsonPropertyName("agents")]
    public Dictionary<string, AgentEnumOverrides> Agents { get; set; } = new();

    /// <summary>
    /// Configuration metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public EnumConfigurationMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Definition of an enum type with its values and metadata
/// </summary>
public class EnumDefinition
{
    /// <summary>
    /// Enum type identifier (e.g., "ContactStatus", "PropertyType")
    /// </summary>
    [JsonPropertyName("enumType")]
    public string EnumType { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the enum's purpose
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping related enums
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Enum values with their properties
    /// </summary>
    [JsonPropertyName("values")]
    public Dictionary<string, EnumValue> Values { get; set; } = new();

    /// <summary>
    /// Default value key
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether this enum allows custom values
    /// </summary>
    [JsonPropertyName("allowCustomValues")]
    public bool AllowCustomValues { get; set; } = false;

    /// <summary>
    /// Validation rules for custom values
    /// </summary>
    [JsonPropertyName("customValueRules")]
    public CustomValueRules? CustomValueRules { get; set; }

    /// <summary>
    /// UI configuration
    /// </summary>
    [JsonPropertyName("uiConfig")]
    public EnumUIConfig? UIConfig { get; set; }
}

/// <summary>
/// Individual enum value with display properties and metadata
/// </summary>
public class EnumValue
{
    /// <summary>
    /// Unique key for the enum value
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; } = 0;

    /// <summary>
    /// Whether this value is active/enabled
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// CSS class or styling information
    /// </summary>
    [JsonPropertyName("cssClass")]
    public string? CssClass { get; set; }

    /// <summary>
    /// Icon identifier
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Color code for UI display
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Additional metadata for business logic
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Business rules or conditions for this value
    /// </summary>
    [JsonPropertyName("businessRules")]
    public List<BusinessRule>? BusinessRules { get; set; }
}

/// <summary>
/// Industry-specific enum overrides
/// </summary>
public class IndustryEnumOverrides
{
    /// <summary>
    /// Industry code (e.g., "RealEstate", "Insurance")
    /// </summary>
    [JsonPropertyName("industryCode")]
    public string IndustryCode { get; set; } = string.Empty;

    /// <summary>
    /// Industry display name
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Industry-specific enum definitions
    /// </summary>
    [JsonPropertyName("enums")]
    public Dictionary<string, EnumDefinition> Enums { get; set; } = new();

    /// <summary>
    /// Overrides for global enum values
    /// </summary>
    [JsonPropertyName("enumOverrides")]
    public Dictionary<string, EnumValueOverrides> EnumOverrides { get; set; } = new();
}

/// <summary>
/// Agent-specific enum customizations
/// </summary>
public class AgentEnumOverrides
{
    /// <summary>
    /// Agent type identifier
    /// </summary>
    [JsonPropertyName("agentType")]
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Agent-specific enum definitions
    /// </summary>
    [JsonPropertyName("enums")]
    public Dictionary<string, EnumDefinition> Enums { get; set; } = new();

    /// <summary>
    /// Overrides for existing enum values
    /// </summary>
    [JsonPropertyName("enumOverrides")]
    public Dictionary<string, EnumValueOverrides> EnumOverrides { get; set; } = new();
}

/// <summary>
/// Overrides for specific enum values
/// </summary>
public class EnumValueOverrides
{
    /// <summary>
    /// Values to add or override
    /// </summary>
    [JsonPropertyName("addOrUpdate")]
    public Dictionary<string, EnumValue>? AddOrUpdate { get; set; }

    /// <summary>
    /// Values to remove/disable
    /// </summary>
    [JsonPropertyName("remove")]
    public List<string>? Remove { get; set; }

    /// <summary>
    /// New default value
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Rules for custom enum values
/// </summary>
public class CustomValueRules
{
    /// <summary>
    /// Minimum length for custom values
    /// </summary>
    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    /// <summary>
    /// Maximum length for custom values
    /// </summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Regular expression pattern for validation
    /// </summary>
    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    /// <summary>
    /// Allowed characters or character sets
    /// </summary>
    [JsonPropertyName("allowedCharacters")]
    public string? AllowedCharacters { get; set; }

    /// <summary>
    /// Whether custom values require approval
    /// </summary>
    [JsonPropertyName("requiresApproval")]
    public bool RequiresApproval { get; set; } = false;
}

/// <summary>
/// UI configuration for enum display
/// </summary>
public class EnumUIConfig
{
    /// <summary>
    /// UI control type (dropdown, radio, checkbox, etc.)
    /// </summary>
    [JsonPropertyName("controlType")]
    public string ControlType { get; set; } = "dropdown";

    /// <summary>
    /// Whether to show descriptions in UI
    /// </summary>
    [JsonPropertyName("showDescriptions")]
    public bool ShowDescriptions { get; set; } = false;

    /// <summary>
    /// Whether to show icons
    /// </summary>
    [JsonPropertyName("showIcons")]
    public bool ShowIcons { get; set; } = false;

    /// <summary>
    /// Whether to allow multiple selections
    /// </summary>
    [JsonPropertyName("allowMultiple")]
    public bool AllowMultiple { get; set; } = false;

    /// <summary>
    /// Whether to show search/filter
    /// </summary>
    [JsonPropertyName("searchable")]
    public bool Searchable { get; set; } = false;

    /// <summary>
    /// Custom CSS classes
    /// </summary>
    [JsonPropertyName("cssClasses")]
    public List<string>? CssClasses { get; set; }
}

/// <summary>
/// Business rule for enum values
/// </summary>
public class BusinessRule
{
    /// <summary>
    /// Rule identifier
    /// </summary>
    [JsonPropertyName("ruleId")]
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Rule description
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Condition for rule application
    /// </summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    /// <summary>
    /// Action to take when rule applies
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// Rule metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Metadata for enum definitions
/// </summary>
public class EnumMetadata
{
    /// <summary>
    /// Enum type
    /// </summary>
    public string EnumType { get; set; } = string.Empty;

    /// <summary>
    /// Total number of values
    /// </summary>
    public int ValueCount { get; set; }

    /// <summary>
    /// Number of active values
    /// </summary>
    public int ActiveValueCount { get; set; }

    /// <summary>
    /// Default value
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether custom values are allowed
    /// </summary>
    public bool AllowCustomValues { get; set; }

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Categories used by enum values
    /// </summary>
    public List<string> Categories { get; set; } = new();
}

/// <summary>
/// Configuration metadata with versioning and audit support
/// </summary>
public class EnumConfigurationMetadata
{
    /// <summary>
    /// Configuration version
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated by user/system
    /// </summary>
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Configuration description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Change log entries for audit trail
    /// </summary>
    [JsonPropertyName("changeLog")]
    public List<ChangeLogEntry> ChangeLog { get; set; } = new();

    /// <summary>
    /// Version history for rollback support
    /// </summary>
    [JsonPropertyName("versionHistory")]
    public List<VersionHistoryEntry> VersionHistory { get; set; } = new();

    /// <summary>
    /// Approval status for changes
    /// </summary>
    [JsonPropertyName("approvalStatus")]
    public EnumApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>
    /// Backup configuration for rollback
    /// </summary>
    [JsonPropertyName("backupPath")]
    public string? BackupPath { get; set; }
}

/// <summary>
/// Change log entry for audit trail
/// </summary>
public class ChangeLogEntry
{
    /// <summary>
    /// Unique identifier for the change
    /// </summary>
    [JsonPropertyName("changeId")]
    public string ChangeId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp of the change
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who made the change
    /// </summary>
    [JsonPropertyName("changedBy")]
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Type of change (Create, Update, Delete, Rollback)
    /// </summary>
    [JsonPropertyName("changeType")]
    public ChangeType ChangeType { get; set; }

    /// <summary>
    /// Enum type that was changed
    /// </summary>
    [JsonPropertyName("enumType")]
    public string? EnumType { get; set; }

    /// <summary>
    /// Specific enum value key that was changed
    /// </summary>
    [JsonPropertyName("enumValueKey")]
    public string? EnumValueKey { get; set; }

    /// <summary>
    /// Description of the change
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Previous value (for rollback)
    /// </summary>
    [JsonPropertyName("previousValue")]
    public object? PreviousValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    [JsonPropertyName("newValue")]
    public object? NewValue { get; set; }

    /// <summary>
    /// Industry context for the change
    /// </summary>
    [JsonPropertyName("industryCode")]
    public string? IndustryCode { get; set; }

    /// <summary>
    /// Agent context for the change
    /// </summary>
    [JsonPropertyName("agentType")]
    public string? AgentType { get; set; }

    /// <summary>
    /// IP address of the change source
    /// </summary>
    [JsonPropertyName("sourceIpAddress")]
    public string? SourceIpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata for the change
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Version history entry for rollback support
/// </summary>
public class VersionHistoryEntry
{
    /// <summary>
    /// Version identifier
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when version was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this version
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Description of changes in this version
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// File path to the backup of this version
    /// </summary>
    [JsonPropertyName("backupFilePath")]
    public string BackupFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the configuration for integrity checking
    /// </summary>
    [JsonPropertyName("configurationHash")]
    public string ConfigurationHash { get; set; } = string.Empty;

    /// <summary>
    /// Size of the configuration file in bytes
    /// </summary>
    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Whether this version is marked as stable
    /// </summary>
    [JsonPropertyName("isStable")]
    public bool IsStable { get; set; } = false;

    /// <summary>
    /// Tags for categorizing versions
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Change summary for this version
    /// </summary>
    [JsonPropertyName("changeSummary")]
    public VersionChangeSummary? ChangeSummary { get; set; }
}

/// <summary>
/// Summary of changes in a version
/// </summary>
public class VersionChangeSummary
{
    /// <summary>
    /// Number of enums added
    /// </summary>
    [JsonPropertyName("enumsAdded")]
    public int EnumsAdded { get; set; }

    /// <summary>
    /// Number of enums modified
    /// </summary>
    [JsonPropertyName("enumsModified")]
    public int EnumsModified { get; set; }

    /// <summary>
    /// Number of enums removed
    /// </summary>
    [JsonPropertyName("enumsRemoved")]
    public int EnumsRemoved { get; set; }

    /// <summary>
    /// Number of enum values added
    /// </summary>
    [JsonPropertyName("valuesAdded")]
    public int ValuesAdded { get; set; }

    /// <summary>
    /// Number of enum values modified
    /// </summary>
    [JsonPropertyName("valuesModified")]
    public int ValuesModified { get; set; }

    /// <summary>
    /// Number of enum values removed
    /// </summary>
    [JsonPropertyName("valuesRemoved")]
    public int ValuesRemoved { get; set; }

    /// <summary>
    /// List of affected enum types
    /// </summary>
    [JsonPropertyName("affectedEnumTypes")]
    public List<string> AffectedEnumTypes { get; set; } = new();
}

/// <summary>
/// Approval status for enum changes
/// </summary>
public class EnumApprovalStatus
{
    /// <summary>
    /// Current approval state
    /// </summary>
    [JsonPropertyName("status")]
    public ApprovalState Status { get; set; } = ApprovalState.Pending;

    /// <summary>
    /// User who submitted for approval
    /// </summary>
    [JsonPropertyName("submittedBy")]
    public string? SubmittedBy { get; set; }

    /// <summary>
    /// Timestamp when submitted for approval
    /// </summary>
    [JsonPropertyName("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// User who approved/rejected
    /// </summary>
    [JsonPropertyName("reviewedBy")]
    public string? ReviewedBy { get; set; }

    /// <summary>
    /// Timestamp when reviewed
    /// </summary>
    [JsonPropertyName("reviewedAt")]
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Approval/rejection comments
    /// </summary>
    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    /// <summary>
    /// Required approvers for this change
    /// </summary>
    [JsonPropertyName("requiredApprovers")]
    public List<string> RequiredApprovers { get; set; } = new();

    /// <summary>
    /// Users who have approved
    /// </summary>
    [JsonPropertyName("approvedBy")]
    public List<ApprovalRecord> ApprovedBy { get; set; } = new();
}

/// <summary>
/// Individual approval record
/// </summary>
public class ApprovalRecord
{
    /// <summary>
    /// User who approved
    /// </summary>
    [JsonPropertyName("approver")]
    public string Approver { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of approval
    /// </summary>
    [JsonPropertyName("approvedAt")]
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Approval comments
    /// </summary>
    [JsonPropertyName("comments")]
    public string? Comments { get; set; }
}

/// <summary>
/// Type of change for audit logging
/// </summary>
public enum ChangeType
{
    Create,
    Update,
    Delete,
    Rollback,
    Approve,
    Reject,
    Import,
    Export
}

/// <summary>
/// Approval state for changes
/// </summary>
public enum ApprovalState
{
    Pending,
    Approved,
    Rejected,
    AutoApproved,
    RequiresReview
}

/// <summary>
/// Version comparison result
/// </summary>
public class VersionComparisonResult
{
    /// <summary>
    /// Source version identifier
    /// </summary>
    [JsonPropertyName("fromVersion")]
    public string FromVersion { get; set; } = string.Empty;

    /// <summary>
    /// Target version identifier
    /// </summary>
    [JsonPropertyName("toVersion")]
    public string ToVersion { get; set; } = string.Empty;

    /// <summary>
    /// List of differences between versions
    /// </summary>
    [JsonPropertyName("differences")]
    public List<VersionDifference> Differences { get; set; } = new();

    /// <summary>
    /// Summary of changes
    /// </summary>
    [JsonPropertyName("changeSummary")]
    public VersionChangeSummary ChangeSummary { get; set; } = new();

    /// <summary>
    /// Whether the versions are identical
    /// </summary>
    [JsonPropertyName("areIdentical")]
    public bool AreIdentical => !Differences.Any();
}

/// <summary>
/// Individual difference between versions
/// </summary>
public class VersionDifference
{
    /// <summary>
    /// Type of difference
    /// </summary>
    [JsonPropertyName("differenceType")]
    public DifferenceType DifferenceType { get; set; }

    /// <summary>
    /// Path to the changed item (e.g., "GlobalEnums.ContactStatus.Qualified")
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Description of the change
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Old value (if applicable)
    /// </summary>
    [JsonPropertyName("oldValue")]
    public object? OldValue { get; set; }

    /// <summary>
    /// New value (if applicable)
    /// </summary>
    [JsonPropertyName("newValue")]
    public object? NewValue { get; set; }

    /// <summary>
    /// Severity of the change
    /// </summary>
    [JsonPropertyName("severity")]
    public ChangeSeverity Severity { get; set; } = ChangeSeverity.Minor;
}

/// <summary>
/// Type of difference in version comparison
/// </summary>
public enum DifferenceType
{
    Added,
    Removed,
    Modified,
    Moved,
    Renamed
}

/// <summary>
/// Severity of a change
/// </summary>
public enum ChangeSeverity
{
    Minor,
    Major,
    Breaking,
    Critical
}

/// <summary>
/// Import result with details
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Import summary
    /// </summary>
    [JsonPropertyName("summary")]
    public ImportSummary Summary { get; set; } = new();

    /// <summary>
    /// List of errors encountered during import
    /// </summary>
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings generated during import
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// List of conflicts that were resolved
    /// </summary>
    [JsonPropertyName("conflicts")]
    public List<ImportConflict> Conflicts { get; set; } = new();

    /// <summary>
    /// Version created from the import
    /// </summary>
    [JsonPropertyName("importedVersion")]
    public string? ImportedVersion { get; set; }

    /// <summary>
    /// Timestamp of the import
    /// </summary>
    [JsonPropertyName("importedAt")]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Summary of import operation
/// </summary>
public class ImportSummary
{
    /// <summary>
    /// Number of enums imported
    /// </summary>
    [JsonPropertyName("enumsImported")]
    public int EnumsImported { get; set; }

    /// <summary>
    /// Number of enum values imported
    /// </summary>
    [JsonPropertyName("valuesImported")]
    public int ValuesImported { get; set; }

    /// <summary>
    /// Number of conflicts encountered
    /// </summary>
    [JsonPropertyName("conflictsEncountered")]
    public int ConflictsEncountered { get; set; }

    /// <summary>
    /// Number of items skipped
    /// </summary>
    [JsonPropertyName("itemsSkipped")]
    public int ItemsSkipped { get; set; }

    /// <summary>
    /// Size of imported file in bytes
    /// </summary>
    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Time taken for import operation
    /// </summary>
    [JsonPropertyName("processingTimeMs")]
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Import conflict details
/// </summary>
public class ImportConflict
{
    /// <summary>
    /// Path where conflict occurred
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Description of the conflict
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Existing value
    /// </summary>
    [JsonPropertyName("existingValue")]
    public object? ExistingValue { get; set; }

    /// <summary>
    /// Imported value
    /// </summary>
    [JsonPropertyName("importedValue")]
    public object? ImportedValue { get; set; }

    /// <summary>
    /// How the conflict was resolved
    /// </summary>
    [JsonPropertyName("resolution")]
    public ConflictResolution Resolution { get; set; }

    /// <summary>
    /// Final value after resolution
    /// </summary>
    [JsonPropertyName("resolvedValue")]
    public object? ResolvedValue { get; set; }
}

/// <summary>
/// How import conflicts are resolved
/// </summary>
public enum ConflictResolution
{
    KeepExisting,
    UseImported,
    Merged,
    Skipped,
    ManualReview
}

/// <summary>
/// Strategy for merging imported configuration
/// </summary>
public enum MergeStrategy
{
    Replace,
    Merge,
    KeepExisting,
    Interactive
}

/// <summary>
/// Version information for enum configuration
/// </summary>
public class EnumVersion
{
    /// <summary>
    /// Version identifier
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this version
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Version description or change summary
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// SHA256 hash for integrity verification
    /// </summary>
    [JsonPropertyName("hash")]
    public string? Hash { get; set; }

    /// <summary>
    /// File path to the version backup
    /// </summary>
    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }

    /// <summary>
    /// Whether this is the current active version
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Tags associated with this version
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}
