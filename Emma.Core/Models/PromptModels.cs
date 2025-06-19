namespace Emma.Core.Models;

/// <summary>
/// Root configuration for all AI prompts
/// Loaded from external JSON/YAML file for business configurability
/// </summary>
public class PromptConfiguration
{
    /// <summary>
    /// System prompts by agent type
    /// </summary>
    public Dictionary<string, AgentPromptSet> Agents { get; set; } = new();
    
    /// <summary>
    /// Industry-specific prompt overrides
    /// </summary>
    public Dictionary<string, IndustryPromptOverrides> Industries { get; set; } = new();
    
    /// <summary>
    /// Global prompt templates used across agents
    /// </summary>
    public Dictionary<string, string> GlobalTemplates { get; set; } = new();
    
    /// <summary>
    /// Configuration metadata
    /// </summary>
    public PromptMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Complete prompt set for a specific agent
/// </summary>
public class AgentPromptSet
{
    /// <summary>
    /// Primary system prompt defining agent role and capabilities
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Context-building prompt templates
    /// </summary>
    public Dictionary<string, string> ContextTemplates { get; set; } = new();
    
    /// <summary>
    /// Action-specific prompt templates
    /// </summary>
    public Dictionary<string, string> ActionTemplates { get; set; } = new();
    
    /// <summary>
    /// Response formatting instructions
    /// </summary>
    public Dictionary<string, string> ResponseFormats { get; set; } = new();
    
    /// <summary>
    /// Agent-specific configuration
    /// </summary>
    public AgentPromptConfig Configuration { get; set; } = new();
}

/// <summary>
/// Industry-specific prompt customizations
/// </summary>
public class IndustryPromptOverrides
{
    /// <summary>
    /// Industry code (e.g., "RealEstate", "Mortgage")
    /// </summary>
    public string IndustryCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent prompt overrides for this industry
    /// </summary>
    public Dictionary<string, AgentPromptSet> AgentOverrides { get; set; } = new();
    
    /// <summary>
    /// Industry-specific terminology and context
    /// </summary>
    public Dictionary<string, string> Terminology { get; set; } = new();
    
    /// <summary>
    /// Industry-specific response formats
    /// </summary>
    public Dictionary<string, string> ResponseFormats { get; set; } = new();
}

/// <summary>
/// Agent-specific prompt configuration
/// </summary>
public class AgentPromptConfig
{
    /// <summary>
    /// Maximum tokens for this agent's responses
    /// </summary>
    public int MaxTokens { get; set; } = 1000;
    
    /// <summary>
    /// Temperature setting for creativity vs consistency
    /// </summary>
    public float Temperature { get; set; } = 0.7f;
    
    /// <summary>
    /// Required placeholders that must be provided
    /// </summary>
    public List<string> RequiredPlaceholders { get; set; } = new();
    
    /// <summary>
    /// Optional placeholders with default values
    /// </summary>
    public Dictionary<string, string> DefaultPlaceholders { get; set; } = new();
    
    /// <summary>
    /// Validation rules for this agent's prompts
    /// </summary>
    public List<string> ValidationRules { get; set; } = new();
}

/// <summary>
/// Metadata about the prompt configuration
/// </summary>
public class PromptMetadata
{
    /// <summary>
    /// Configuration version for tracking changes
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Author/editor information
    /// </summary>
    public string ModifiedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of changes in this version
    /// </summary>
    public string ChangeDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Environment this configuration is for
    /// </summary>
    public string Environment { get; set; } = "Development";
    
    /// <summary>
    /// Version history for tracking changes over time
    /// </summary>
    public List<PromptVersionHistoryEntry> VersionHistory { get; set; } = new();
    
    /// <summary>
    /// Change log entries for audit trail
    /// </summary>
    public List<PromptChangeLogEntry> ChangeLog { get; set; } = new();
}

/// <summary>
/// Version history entry for prompt configuration tracking
/// </summary>
public class PromptVersionHistoryEntry
{
    /// <summary>
    /// Unique version identifier
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Who created this version
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of changes in this version
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to the backup file for this version
    /// </summary>
    public string BackupFilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// SHA256 hash of the configuration for integrity verification
    /// </summary>
    public string ConfigurationHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Size of the configuration file in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
    
    /// <summary>
    /// Tags for categorizing versions
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Summary of changes made in this version
    /// </summary>
    public Dictionary<string, object> ChangeSummary { get; set; } = new();
}

/// <summary>
/// Change log entry for prompt configuration audit trail
/// </summary>
public class PromptChangeLogEntry
{
    /// <summary>
    /// Unique identifier for this change
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// When the change occurred
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Type of change made
    /// </summary>
    public PromptChangeType ChangeType { get; set; }
    
    /// <summary>
    /// Agent type affected by the change (if applicable)
    /// </summary>
    public string? AgentType { get; set; }
    
    /// <summary>
    /// Industry affected by the change (if applicable)
    /// </summary>
    public string? IndustryCode { get; set; }
    
    /// <summary>
    /// Template name affected by the change (if applicable)
    /// </summary>
    public string? TemplateName { get; set; }
    
    /// <summary>
    /// Who made the change
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the change
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional metadata about the change
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of changes that can be made to prompt configuration
/// </summary>
public enum PromptChangeType
{
    Create,
    Update,
    Delete,
    Rollback,
    Import,
    Export,
    Approve,
    Reject
}

/// <summary>
/// Result of comparing two prompt configuration versions
/// </summary>
public class PromptVersionComparisonResult
{
    /// <summary>
    /// First version being compared
    /// </summary>
    public string Version1 { get; set; } = string.Empty;
    
    /// <summary>
    /// Second version being compared
    /// </summary>
    public string Version2 { get; set; } = string.Empty;
    
    /// <summary>
    /// List of differences between the versions
    /// </summary>
    public List<string> Differences { get; set; } = new();
    
    /// <summary>
    /// Metadata for version 1
    /// </summary>
    public PromptVersionHistoryEntry? Version1Metadata { get; set; }
    
    /// <summary>
    /// Metadata for version 2
    /// </summary>
    public PromptVersionHistoryEntry? Version2Metadata { get; set; }
}

/// <summary>
/// Strategies for merging prompt configurations during import
/// </summary>
public enum PromptMergeStrategy
{
    /// <summary>
    /// Replace entire configuration with imported one
    /// </summary>
    Replace,
    
    /// <summary>
    /// Merge imported configuration with existing, imported takes precedence
    /// </summary>
    Merge,
    
    /// <summary>
    /// Merge imported configuration with existing, existing takes precedence
    /// </summary>
    KeepExisting
}

/// <summary>
/// Request model for creating a new prompt configuration version
/// </summary>
public class CreatePromptVersionRequest
{
    /// <summary>
    /// Description of changes in this version
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Who is creating this version
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional tags for categorizing this version
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request model for rolling back to a specific version
/// </summary>
public class PromptRollbackRequest
{
    /// <summary>
    /// Version to rollback to
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Who is performing the rollback
    /// </summary>
    public string RolledBackBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for the rollback
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request model for importing prompt configuration
/// </summary>
public class PromptImportRequest
{
    /// <summary>
    /// Who is performing the import
    /// </summary>
    public string ImportedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Strategy for merging the imported configuration
    /// </summary>
    public PromptMergeStrategy MergeStrategy { get; set; } = PromptMergeStrategy.Replace;
}

/// <summary>
/// Response model for prompt configuration operations
/// </summary>
public class PromptConfigurationResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Result data (version ID, export path, etc.)
    /// </summary>
    public object? Data { get; set; }
    
    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Additional metadata about the operation
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Context for building dynamic prompts
/// </summary>
public class PromptContext
{
    /// <summary>
    /// Dynamic values to substitute in prompt templates
    /// </summary>
    public Dictionary<string, object> Values { get; set; } = new();
    
    /// <summary>
    /// Industry profile for industry-specific customization
    /// </summary>
    public string IndustryCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent type requesting the prompt
    /// </summary>
    public string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Request context for additional customization
    /// </summary>
    public Dictionary<string, object> RequestContext { get; set; } = new();
}
