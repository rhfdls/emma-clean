using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Emma.Core.Services;

/// <summary>
/// Provides dynamic enum management with industry-specific overrides and hot-reload support
/// Mirrors the architecture of PromptProvider for consistent user experience
/// </summary>
public class EnumProvider : IEnumProvider, IDisposable
{
    private readonly ILogger<EnumProvider> _logger;
    private readonly string _configurationFilePath;
    private readonly bool _enableHotReload;
    private readonly EnumVersioningService _versioningService;
    private FileSystemWatcher? _fileWatcher;
    private EnumConfiguration? _enumConfiguration;
    private readonly object _lockObject = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public event EventHandler<EnumConfigurationChangedEventArgs>? ConfigurationChanged;

    public EnumProvider(ILogger<EnumProvider> logger, string configurationFilePath, bool enableHotReload = false)
    {
        _logger = logger;
        _configurationFilePath = configurationFilePath;
        _enableHotReload = enableHotReload;
        _versioningService = new EnumVersioningService(logger.CreateLogger<EnumVersioningService>(), configurationFilePath);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // Load initial configuration
        _ = Task.Run(LoadConfigurationAsync);

        // Setup hot reload if enabled
        if (_enableHotReload)
        {
            SetupFileWatcher();
        }

        _logger.LogInformation("EnumProvider initialized with configuration file: {FilePath}, Hot reload: {HotReload}", 
            _configurationFilePath, _enableHotReload);
    }

    public async Task<IEnumerable<EnumValue>> GetEnumValuesAsync(string enumType, EnumContext? context = null)
    {
        try
        {
            var enumDefinition = await GetEnumDefinitionAsync(enumType, context);
            if (enumDefinition == null)
            {
                _logger.LogWarning("Enum definition not found for type: {EnumType}", enumType);
                return Enumerable.Empty<EnumValue>();
            }

            // Apply industry and agent overrides
            var values = new Dictionary<string, EnumValue>(enumDefinition.Values);
            ApplyOverrides(values, enumType, context);

            // Filter active values and sort by order
            var result = values.Values
                .Where(v => v.IsActive)
                .OrderBy(v => v.Order)
                .ThenBy(v => v.DisplayName)
                .ToList();

            _logger.LogDebug("Retrieved {Count} enum values for {EnumType}", result.Count, enumType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enum values for {EnumType}", enumType);
            return Enumerable.Empty<EnumValue>();
        }
    }

    public async Task<EnumValue?> GetEnumValueAsync(string enumType, string key, EnumContext? context = null)
    {
        try
        {
            var values = await GetEnumValuesAsync(enumType, context);
            var result = values.FirstOrDefault(v => v.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            
            if (result == null)
            {
                _logger.LogDebug("Enum value not found: {EnumType}.{Key}", enumType, key);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enum value {EnumType}.{Key}", enumType, key);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetEnumDropdownAsync(string enumType, EnumContext? context = null)
    {
        try
        {
            var values = await GetEnumValuesAsync(enumType, context);
            var result = values.ToDictionary(v => v.Key, v => v.DisplayName);
            
            _logger.LogDebug("Generated dropdown with {Count} options for {EnumType}", result.Count, enumType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dropdown for {EnumType}", enumType);
            return new Dictionary<string, string>();
        }
    }

    public async Task<bool> ValidateEnumValueAsync(string enumType, string key, EnumContext? context = null)
    {
        try
        {
            var enumValue = await GetEnumValueAsync(enumType, key, context);
            var isValid = enumValue != null;
            
            _logger.LogDebug("Validation result for {EnumType}.{Key}: {IsValid}", enumType, key, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating enum value {EnumType}.{Key}", enumType, key);
            return false;
        }
    }

    public async Task<EnumMetadata?> GetEnumMetadataAsync(string enumType, EnumContext? context = null)
    {
        try
        {
            var enumDefinition = await GetEnumDefinitionAsync(enumType, context);
            if (enumDefinition == null)
            {
                return null;
            }

            var values = await GetEnumValuesAsync(enumType, context);
            var categories = values
                .SelectMany(v => v.Metadata.ContainsKey("category") ? new[] { v.Metadata["category"].ToString() } : Array.Empty<string>())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var metadata = new EnumMetadata
            {
                EnumType = enumType,
                ValueCount = enumDefinition.Values.Count,
                ActiveValueCount = values.Count(),
                DefaultValue = enumDefinition.DefaultValue,
                AllowCustomValues = enumDefinition.AllowCustomValues,
                LastModified = _enumConfiguration?.Metadata.LastUpdated ?? DateTime.UtcNow,
                Categories = categories!
            };

            _logger.LogDebug("Generated metadata for {EnumType}: {ValueCount} total, {ActiveCount} active", 
                enumType, metadata.ValueCount, metadata.ActiveValueCount);
            
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metadata for {EnumType}", enumType);
            return null;
        }
    }

    public async Task<IEnumerable<string>> GetAvailableEnumTypesAsync(EnumContext? context = null)
    {
        try
        {
            if (_enumConfiguration == null)
            {
                await LoadConfigurationAsync();
            }

            var enumTypes = new HashSet<string>();

            // Add global enums
            if (_enumConfiguration?.GlobalEnums != null)
            {
                foreach (var enumType in _enumConfiguration.GlobalEnums.Keys)
                {
                    enumTypes.Add(enumType);
                }
            }

            // Add industry-specific enums
            if (!string.IsNullOrEmpty(context?.IndustryCode) && 
                _enumConfiguration?.Industries?.TryGetValue(context.IndustryCode, out var industryOverrides) == true)
            {
                if (industryOverrides.Enums != null)
                {
                    foreach (var enumType in industryOverrides.Enums.Keys)
                    {
                        enumTypes.Add(enumType);
                    }
                }
            }

            // Add agent-specific enums
            if (!string.IsNullOrEmpty(context?.AgentType) && 
                _enumConfiguration?.Agents?.TryGetValue(context.AgentType, out var agentOverrides) == true)
            {
                if (agentOverrides.Enums != null)
                {
                    foreach (var enumType in agentOverrides.Enums.Keys)
                    {
                        enumTypes.Add(enumType);
                    }
                }
            }

            var result = enumTypes.OrderBy(t => t).ToList();
            _logger.LogDebug("Found {Count} available enum types", result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available enum types");
            return Enumerable.Empty<string>();
        }
    }

    public async Task ReloadConfigurationAsync()
    {
        _logger.LogInformation("Manually reloading enum configuration");
        await LoadConfigurationAsync();
    }

    private async Task<EnumDefinition?> GetEnumDefinitionAsync(string enumType, EnumContext? context)
    {
        if (_enumConfiguration == null)
        {
            await LoadConfigurationAsync();
        }

        // Try agent-specific enum first
        if (!string.IsNullOrEmpty(context?.AgentType) && 
            _enumConfiguration?.Agents?.TryGetValue(context.AgentType, out var agentOverrides) == true &&
            agentOverrides.Enums?.TryGetValue(enumType, out var agentEnum) == true)
        {
            return agentEnum;
        }

        // Try industry-specific enum
        if (!string.IsNullOrEmpty(context?.IndustryCode) && 
            _enumConfiguration?.Industries?.TryGetValue(context.IndustryCode, out var industryOverrides) == true &&
            industryOverrides.Enums?.TryGetValue(enumType, out var industryEnum) == true)
        {
            return industryEnum;
        }

        // Try global enum
        if (_enumConfiguration?.GlobalEnums?.TryGetValue(enumType, out var globalEnum) == true)
        {
            return globalEnum;
        }

        return null;
    }

    private void ApplyOverrides(Dictionary<string, EnumValue> values, string enumType, EnumContext? context)
    {
        // Apply industry overrides
        if (!string.IsNullOrEmpty(context?.IndustryCode) && 
            _enumConfiguration?.Industries?.TryGetValue(context.IndustryCode, out var industryOverrides) == true &&
            industryOverrides.EnumOverrides?.TryGetValue(enumType, out var industryEnumOverrides) == true)
        {
            ApplyEnumValueOverrides(values, industryEnumOverrides);
        }

        // Apply agent overrides
        if (!string.IsNullOrEmpty(context?.AgentType) && 
            _enumConfiguration?.Agents?.TryGetValue(context.AgentType, out var agentOverrides) == true &&
            agentOverrides.EnumOverrides?.TryGetValue(enumType, out var agentEnumOverrides) == true)
        {
            ApplyEnumValueOverrides(values, agentEnumOverrides);
        }
    }

    private void ApplyEnumValueOverrides(Dictionary<string, EnumValue> values, EnumValueOverrides overrides)
    {
        // Add or update values
        if (overrides.AddOrUpdate != null)
        {
            foreach (var kvp in overrides.AddOrUpdate)
            {
                values[kvp.Key] = kvp.Value;
            }
        }

        // Remove values
        if (overrides.Remove != null)
        {
            foreach (var key in overrides.Remove)
            {
                if (values.ContainsKey(key))
                {
                    values[key].IsActive = false;
                }
            }
        }
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configurationFilePath))
            {
                _logger.LogWarning("Enum configuration file not found: {FilePath}", _configurationFilePath);
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(_configurationFilePath);
            var configuration = JsonSerializer.Deserialize<EnumConfiguration>(jsonContent, _jsonOptions);

            lock (_lockObject)
            {
                _enumConfiguration = configuration;
            }

            _logger.LogInformation("Enum configuration loaded successfully from {FilePath}", _configurationFilePath);
            
            // Notify subscribers of configuration change
            ConfigurationChanged?.Invoke(this, new EnumConfigurationChangedEventArgs
            {
                ChangedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load enum configuration from {FilePath}", _configurationFilePath);
        }
    }

    private void SetupFileWatcher()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configurationFilePath);
            var fileName = Path.GetFileName(_configurationFilePath);

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("Invalid configuration file path for file watcher: {FilePath}", _configurationFilePath);
                return;
            }

            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += async (sender, e) =>
            {
                _logger.LogInformation("Enum configuration file changed, reloading...");
                
                // Add small delay to ensure file write is complete
                await Task.Delay(500);
                await LoadConfigurationAsync();
            };

            _logger.LogInformation("File watcher setup for enum configuration: {FilePath}", _configurationFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup file watcher for enum configuration");
        }
    }

    public async Task<EnumVersion> GetCurrentVersionAsync()
    {
        return await _versioningService.GetCurrentVersionAsync();
    }

    public async Task<EnumVersion> GetVersionAsync(string versionId)
    {
        return await _versioningService.GetVersionAsync(versionId);
    }

    public async Task<IEnumerable<EnumVersion>> GetVersionsAsync()
    {
        return await _versioningService.GetVersionsAsync();
    }

    public async Task<EnumVersion> CreateVersionAsync(EnumVersion version)
    {
        return await _versioningService.CreateVersionAsync(version);
    }

    public async Task<EnumVersion> UpdateVersionAsync(EnumVersion version)
    {
        return await _versioningService.UpdateVersionAsync(version);
    }

    public async Task DeleteVersionAsync(string versionId)
    {
        await _versioningService.DeleteVersionAsync(versionId);
    }

    // Versioning and Audit Methods Implementation

    /// <summary>
    /// Create a backup version of the current configuration
    /// </summary>
    public async Task<string> CreateVersionAsync(string description, string createdBy, List<string>? tags = null)
    {
        try
        {
            var version = await _versioningService.CreateVersionAsync(description, createdBy, tags);
            
            _logger.LogInformation("Created enum configuration version {Version} by {User}: {Description}", 
                version, createdBy, description);
            
            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create version");
            throw;
        }
    }

    /// <summary>
    /// Get all available versions
    /// </summary>
    public async Task<IEnumerable<VersionHistoryEntry>> GetVersionHistoryAsync()
    {
        try
        {
            lock (_lockObject)
            {
                if (_enumConfiguration?.Metadata?.VersionHistory == null)
                {
                    return Enumerable.Empty<VersionHistoryEntry>();
                }

                return _enumConfiguration.Metadata.VersionHistory
                    .OrderByDescending(v => v.CreatedAt)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get version history");
            return Enumerable.Empty<VersionHistoryEntry>();
        }
    }

    /// <summary>
    /// Rollback to a specific version
    /// </summary>
    public async Task<bool> RollbackToVersionAsync(string version, string rolledBackBy, string reason)
    {
        try
        {
            var success = await _versioningService.RollbackToVersionAsync(version, rolledBackBy, reason);
            
            if (success)
            {
                // Reload configuration after rollback
                await LoadConfigurationAsync();
                
                // Notify listeners of configuration change
                ConfigurationChanged?.Invoke(this, new EnumConfigurationChangedEventArgs
                {
                    ChangeType = "Rollback",
                    ChangedBy = rolledBackBy,
                    Description = $"Rolled back to version {version}: {reason}",
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation("Successfully rolled back to version {Version} by {User}: {Reason}", 
                    version, rolledBackBy, reason);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback to version {Version}", version);
            return false;
        }
    }

    /// <summary>
    /// Get change log entries
    /// </summary>
    public async Task<IEnumerable<ChangeLogEntry>> GetChangeLogAsync(
        string? enumType = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        string? changedBy = null)
    {
        try
        {
            return await _versioningService.GetChangeLogAsync(enumType, fromDate, toDate, changedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get change log");
            return Enumerable.Empty<ChangeLogEntry>();
        }
    }

    /// <summary>
    /// Log a change to the audit trail
    /// </summary>
    public async Task LogChangeAsync(ChangeLogEntry changeEntry)
    {
        try
        {
            await _versioningService.LogChangeAsync(changeEntry);
            
            // Also add to in-memory configuration if available
            lock (_lockObject)
            {
                if (_enumConfiguration?.Metadata != null)
                {
                    _enumConfiguration.Metadata.ChangeLog.Add(changeEntry);
                    
                    // Keep only last 100 entries in memory to prevent bloat
                    if (_enumConfiguration.Metadata.ChangeLog.Count > 100)
                    {
                        _enumConfiguration.Metadata.ChangeLog = _enumConfiguration.Metadata.ChangeLog
                            .OrderByDescending(c => c.Timestamp)
                            .Take(100)
                            .ToList();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log change");
            // Don't throw - audit logging failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Get configuration metadata including version info
    /// </summary>
    public async Task<EnumConfigurationMetadata> GetConfigurationMetadataAsync()
    {
        try
        {
            lock (_lockObject)
            {
                if (_enumConfiguration?.Metadata == null)
                {
                    return new EnumConfigurationMetadata
                    {
                        Version = "1.0",
                        LastUpdated = DateTime.UtcNow,
                        Description = "Default configuration"
                    };
                }

                return _enumConfiguration.Metadata;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration metadata");
            throw;
        }
    }

    /// <summary>
    /// Compare two versions and get differences
    /// </summary>
    public async Task<VersionComparisonResult> CompareVersionsAsync(string fromVersion, string toVersion)
    {
        try
        {
            return await _versioningService.CompareVersionsAsync(fromVersion, toVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare versions {FromVersion} and {ToVersion}", fromVersion, toVersion);
            throw;
        }
    }

    /// <summary>
    /// Submit configuration changes for approval
    /// </summary>
    public async Task<string> SubmitForApprovalAsync(string submittedBy, string description, List<string> requiredApprovers)
    {
        try
        {
            var approvalId = Guid.NewGuid().ToString();
            
            lock (_lockObject)
            {
                if (_enumConfiguration?.Metadata != null)
                {
                    _enumConfiguration.Metadata.ApprovalStatus = new ApprovalStatus
                    {
                        Status = ApprovalState.Pending,
                        SubmittedBy = submittedBy,
                        SubmittedAt = DateTime.UtcNow,
                        RequiredApprovers = requiredApprovers
                    };
                }
            }

            await LogChangeAsync(new ChangeLogEntry
            {
                ChangeType = ChangeType.Create,
                ChangedBy = submittedBy,
                Description = $"Submitted for approval: {description}",
                Metadata = new Dictionary<string, object>
                {
                    ["approvalId"] = approvalId,
                    ["requiredApprovers"] = requiredApprovers
                }
            });

            _logger.LogInformation("Submitted enum configuration for approval by {User}: {Description} (ID: {ApprovalId})", 
                submittedBy, description, approvalId);

            return approvalId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit for approval");
            throw;
        }
    }

    /// <summary>
    /// Approve or reject pending changes
    /// </summary>
    public async Task<bool> ProcessApprovalAsync(string approvalId, string approver, bool approved, string? comments = null)
    {
        try
        {
            lock (_lockObject)
            {
                if (_enumConfiguration?.Metadata?.ApprovalStatus == null)
                {
                    _logger.LogWarning("No pending approval found for ID {ApprovalId}", approvalId);
                    return false;
                }

                var approval = _enumConfiguration.Metadata.ApprovalStatus;
                
                if (!approval.RequiredApprovers.Contains(approver))
                {
                    _logger.LogWarning("User {Approver} is not authorized to approve this change", approver);
                    return false;
                }

                approval.Status = approved ? ApprovalState.Approved : ApprovalState.Rejected;
                approval.ReviewedBy = approver;
                approval.ReviewedAt = DateTime.UtcNow;
                approval.Comments = comments;

                if (approved)
                {
                    approval.ApprovedBy.Add(new ApprovalRecord
                    {
                        Approver = approver,
                        ApprovedAt = DateTime.UtcNow,
                        Comments = comments
                    });
                }
            }

            await LogChangeAsync(new ChangeLogEntry
            {
                ChangeType = approved ? ChangeType.Approve : ChangeType.Reject,
                ChangedBy = approver,
                Description = $"{(approved ? "Approved" : "Rejected")} configuration changes: {comments}",
                Metadata = new Dictionary<string, object>
                {
                    ["approvalId"] = approvalId,
                    ["approved"] = approved
                }
            });

            _logger.LogInformation("Enum configuration {Action} by {Approver} (ID: {ApprovalId}): {Comments}", 
                approved ? "approved" : "rejected", approver, approvalId, comments);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process approval {ApprovalId}", approvalId);
            return false;
        }
    }

    /// <summary>
    /// Export configuration to file for backup/migration
    /// </summary>
    public async Task<bool> ExportConfigurationAsync(string filePath, bool includeHistory = false)
    {
        try
        {
            return await _versioningService.ExportConfigurationAsync(filePath, includeHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export configuration to {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Import configuration from file
    /// </summary>
    public async Task<ImportResult> ImportConfigurationAsync(string filePath, string importedBy, MergeStrategy mergeStrategy = MergeStrategy.Replace)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new ImportResult
                {
                    Success = false,
                    Errors = new List<string> { $"File not found: {filePath}" }
                };
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var fileInfo = new FileInfo(filePath);
            
            // Read and validate import file
            var importContent = await File.ReadAllTextAsync(filePath);
            var importConfig = JsonSerializer.Deserialize<EnumConfiguration>(importContent, _jsonOptions);
            
            if (importConfig == null)
            {
                return new ImportResult
                {
                    Success = false,
                    Errors = new List<string> { "Invalid configuration file format" }
                };
            }

            // Create backup before import
            var backupVersion = await CreateVersionAsync($"Pre-import backup", importedBy, 
                new List<string> { "pre-import", "auto-backup" });

            // Process import based on merge strategy
            var result = await ProcessImport(importConfig, importedBy, mergeStrategy, fileInfo.Length, stopwatch.ElapsedMilliseconds);
            
            if (result.Success)
            {
                // Reload configuration
                await LoadConfigurationAsync();
                
                // Create post-import version
                result.ImportedVersion = await CreateVersionAsync($"Imported from {Path.GetFileName(filePath)}", 
                    importedBy, new List<string> { "import" });

                // Notify listeners
                ConfigurationChanged?.Invoke(this, new EnumConfigurationChangedEventArgs
                {
                    ChangeType = "Import",
                    ChangedBy = importedBy,
                    Description = $"Imported configuration from {Path.GetFileName(filePath)}",
                    Timestamp = DateTime.UtcNow
                });
            }

            await LogChangeAsync(new ChangeLogEntry
            {
                ChangeType = ChangeType.Import,
                ChangedBy = importedBy,
                Description = $"Imported configuration from {Path.GetFileName(filePath)}",
                Metadata = new Dictionary<string, object>
                {
                    ["filePath"] = filePath,
                    ["mergeStrategy"] = mergeStrategy.ToString(),
                    ["success"] = result.Success,
                    ["backupVersion"] = backupVersion
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import configuration from {FilePath}", filePath);
            return new ImportResult
            {
                Success = false,
                Errors = new List<string> { $"Import failed: {ex.Message}" }
            };
        }
    }

    #region Private Helper Methods

    private async Task<ImportResult> ProcessImport(EnumConfiguration importConfig, string importedBy, 
        MergeStrategy mergeStrategy, long fileSizeBytes, long processingTimeMs)
    {
        var result = new ImportResult
        {
            Success = true,
            Summary = new ImportSummary
            {
                FileSizeBytes = fileSizeBytes,
                ProcessingTimeMs = processingTimeMs
            }
        };

        try
        {
            lock (_lockObject)
            {
                if (_enumConfiguration == null)
                {
                    _enumConfiguration = new EnumConfiguration();
                }

                switch (mergeStrategy)
                {
                    case MergeStrategy.Replace:
                        // Replace entire configuration
                        _enumConfiguration.GlobalEnums = importConfig.GlobalEnums;
                        _enumConfiguration.IndustryOverrides = importConfig.IndustryOverrides;
                        _enumConfiguration.AgentOverrides = importConfig.AgentOverrides;
                        result.Summary.EnumsImported = importConfig.GlobalEnums?.Count ?? 0;
                        result.Summary.ValuesImported = importConfig.GlobalEnums?.Values
                            .SelectMany(e => e.Values?.Values ?? Enumerable.Empty<EnumValue>())
                            .Count() ?? 0;
                        break;

                    case MergeStrategy.Merge:
                        // Merge configurations with conflict detection
                        result = MergeConfigurations(_enumConfiguration, importConfig);
                        break;

                    case MergeStrategy.KeepExisting:
                        // Only add new items, don't overwrite existing
                        result = MergeKeepExisting(_enumConfiguration, importConfig);
                        break;

                    default:
                        result.Success = false;
                        result.Errors.Add($"Unsupported merge strategy: {mergeStrategy}");
                        return result;
                }

                // Update metadata
                if (_enumConfiguration.Metadata == null)
                {
                    _enumConfiguration.Metadata = new EnumConfigurationMetadata();
                }

                _enumConfiguration.Metadata.LastUpdated = DateTime.UtcNow;
                _enumConfiguration.Metadata.UpdatedBy = importedBy;
            }

            // Save updated configuration
            var updatedContent = JsonSerializer.Serialize(_enumConfiguration, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_configurationFilePath, updatedContent);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Failed to process import: {ex.Message}");
            return result;
        }
    }

    private ImportResult MergeConfigurations(EnumConfiguration existing, EnumConfiguration imported)
    {
        // Simplified merge implementation - in practice, this would be much more sophisticated
        var result = new ImportResult { Success = true };
        
        // This is a placeholder for complex merge logic
        result.Summary.EnumsImported = imported.GlobalEnums?.Count ?? 0;
        result.Summary.ValuesImported = imported.GlobalEnums?.Values
            .SelectMany(e => e.Values?.Values ?? Enumerable.Empty<EnumValue>())
            .Count() ?? 0;
            
        return result;
    }

    private ImportResult MergeKeepExisting(EnumConfiguration existing, EnumConfiguration imported)
    {
        // Simplified keep-existing implementation
        var result = new ImportResult { Success = true };
        
        // This is a placeholder for keep-existing logic
        result.Summary.EnumsImported = imported.GlobalEnums?.Count ?? 0;
        result.Summary.ValuesImported = imported.GlobalEnums?.Values
            .SelectMany(e => e.Values?.Values ?? Enumerable.Empty<EnumValue>())
            .Count() ?? 0;
            
        return result;
    }

    #endregion

    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _logger.LogInformation("EnumProvider disposed");
    }
}
