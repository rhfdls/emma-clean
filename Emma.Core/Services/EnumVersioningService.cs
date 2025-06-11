using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Emma.Core.Services;

/// <summary>
/// Service for managing enum configuration versioning, audit logging, and rollback
/// Provides enterprise-grade change tracking and recovery capabilities
/// </summary>
public class EnumVersioningService
{
    private readonly ILogger<EnumVersioningService> _logger;
    private readonly string _configurationPath;
    private readonly string _backupDirectory;
    private readonly string _auditLogPath;

    public EnumVersioningService(
        ILogger<EnumVersioningService> logger,
        string configurationPath,
        string? backupDirectory = null)
    {
        _logger = logger;
        _configurationPath = configurationPath;
        _backupDirectory = backupDirectory ?? Path.Combine(Path.GetDirectoryName(configurationPath)!, "Backups");
        _auditLogPath = Path.Combine(Path.GetDirectoryName(configurationPath)!, "enum-audit.log");

        // Ensure backup directory exists
        Directory.CreateDirectory(_backupDirectory);
    }

    /// <summary>
    /// Create a versioned backup of the current configuration
    /// </summary>
    public async Task<string> CreateVersionAsync(string description, string createdBy, List<string>? tags = null)
    {
        try
        {
            var version = GenerateVersionId();
            var timestamp = DateTime.UtcNow;
            
            // Read current configuration
            var configContent = await File.ReadAllTextAsync(_configurationPath);
            var config = JsonSerializer.Deserialize<EnumConfiguration>(configContent);
            
            if (config?.Metadata == null)
            {
                config = config ?? new EnumConfiguration();
                config.Metadata = new EnumConfigurationMetadata();
            }

            // Create backup file
            var backupFileName = $"enums-v{version}-{timestamp:yyyyMMdd-HHmmss}.json";
            var backupFilePath = Path.Combine(_backupDirectory, backupFileName);
            await File.WriteAllTextAsync(backupFilePath, configContent);

            // Calculate file hash for integrity
            var configHash = CalculateFileHash(configContent);
            var fileInfo = new FileInfo(_configurationPath);

            // Create version history entry
            var versionEntry = new VersionHistoryEntry
            {
                Version = version,
                CreatedAt = timestamp,
                CreatedBy = createdBy,
                Description = description,
                BackupFilePath = backupFilePath,
                ConfigurationHash = configHash,
                FileSizeBytes = fileInfo.Length,
                Tags = tags ?? new List<string>(),
                ChangeSummary = await CalculateChangeSummaryAsync(config)
            };

            // Add to version history
            config.Metadata.VersionHistory.Add(versionEntry);
            config.Metadata.Version = version;
            config.Metadata.LastUpdated = timestamp;
            config.Metadata.UpdatedBy = createdBy;

            // Save updated configuration
            var updatedContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_configurationPath, updatedContent);

            // Log the version creation
            await LogChangeAsync(new ChangeLogEntry
            {
                ChangeType = ChangeType.Create,
                ChangedBy = createdBy,
                Description = $"Created version {version}: {description}",
                Metadata = new Dictionary<string, object>
                {
                    ["version"] = version,
                    ["backupPath"] = backupFilePath,
                    ["tags"] = tags ?? new List<string>()
                }
            });

            _logger.LogInformation("Created enum configuration version {Version} by {User}: {Description}", 
                version, createdBy, description);

            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create enum configuration version");
            throw;
        }
    }

    /// <summary>
    /// Rollback to a specific version
    /// </summary>
    public async Task<bool> RollbackToVersionAsync(string version, string rolledBackBy, string reason)
    {
        try
        {
            // Read current configuration to get version history
            var configContent = await File.ReadAllTextAsync(_configurationPath);
            var config = JsonSerializer.Deserialize<EnumConfiguration>(configContent);
            
            if (config?.Metadata?.VersionHistory == null)
            {
                _logger.LogWarning("No version history found for rollback");
                return false;
            }

            // Find the target version
            var targetVersion = config.Metadata.VersionHistory
                .FirstOrDefault(v => v.Version == version);

            if (targetVersion == null)
            {
                _logger.LogWarning("Version {Version} not found in history", version);
                return false;
            }

            if (!File.Exists(targetVersion.BackupFilePath))
            {
                _logger.LogError("Backup file not found: {BackupPath}", targetVersion.BackupFilePath);
                return false;
            }

            // Create a backup of current state before rollback
            var currentBackupVersion = await CreateVersionAsync(
                $"Pre-rollback backup before reverting to {version}", 
                rolledBackBy, 
                new List<string> { "pre-rollback", "auto-backup" });

            // Read the target version configuration
            var targetContent = await File.ReadAllTextAsync(targetVersion.BackupFilePath);
            
            // Verify integrity
            var targetHash = CalculateFileHash(targetContent);
            if (targetHash != targetVersion.ConfigurationHash)
            {
                _logger.LogError("Backup file integrity check failed for version {Version}", version);
                return false;
            }

            // Parse target configuration and update metadata
            var targetConfig = JsonSerializer.Deserialize<EnumConfiguration>(targetContent);
            if (targetConfig?.Metadata != null)
            {
                // Preserve version history and add rollback entry
                targetConfig.Metadata.VersionHistory = config.Metadata.VersionHistory;
                targetConfig.Metadata.Version = GenerateVersionId();
                targetConfig.Metadata.LastUpdated = DateTime.UtcNow;
                targetConfig.Metadata.UpdatedBy = rolledBackBy;

                // Add rollback entry to change log
                targetConfig.Metadata.ChangeLog.Add(new ChangeLogEntry
                {
                    ChangeType = ChangeType.Rollback,
                    ChangedBy = rolledBackBy,
                    Description = $"Rolled back to version {version}: {reason}",
                    PreviousValue = config.Metadata.Version,
                    NewValue = version,
                    Metadata = new Dictionary<string, object>
                    {
                        ["targetVersion"] = version,
                        ["reason"] = reason,
                        ["preRollbackBackup"] = currentBackupVersion
                    }
                });
            }

            // Write the rolled back configuration
            var rolledBackContent = JsonSerializer.Serialize(targetConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_configurationPath, rolledBackContent);

            // Log the rollback
            await LogChangeAsync(new ChangeLogEntry
            {
                ChangeType = ChangeType.Rollback,
                ChangedBy = rolledBackBy,
                Description = $"Rolled back to version {version}: {reason}",
                PreviousValue = config.Metadata.Version,
                NewValue = version,
                Metadata = new Dictionary<string, object>
                {
                    ["targetVersion"] = version,
                    ["reason"] = reason,
                    ["preRollbackBackup"] = currentBackupVersion
                }
            });

            _logger.LogInformation("Successfully rolled back enum configuration to version {Version} by {User}: {Reason}", 
                version, rolledBackBy, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback to version {Version}", version);
            return false;
        }
    }

    /// <summary>
    /// Log a change to the audit trail
    /// </summary>
    public async Task LogChangeAsync(ChangeLogEntry changeEntry)
    {
        try
        {
            var logEntry = new
            {
                Timestamp = changeEntry.Timestamp,
                ChangeId = changeEntry.ChangeId,
                ChangeType = changeEntry.ChangeType.ToString(),
                ChangedBy = changeEntry.ChangedBy,
                EnumType = changeEntry.EnumType,
                EnumValueKey = changeEntry.EnumValueKey,
                Description = changeEntry.Description,
                IndustryCode = changeEntry.IndustryCode,
                AgentType = changeEntry.AgentType,
                SourceIpAddress = changeEntry.SourceIpAddress,
                UserAgent = changeEntry.UserAgent,
                Metadata = changeEntry.Metadata
            };

            var logLine = JsonSerializer.Serialize(logEntry) + Environment.NewLine;
            await File.AppendAllTextAsync(_auditLogPath, logLine);

            _logger.LogDebug("Logged change {ChangeId} of type {ChangeType} by {User}", 
                changeEntry.ChangeId, changeEntry.ChangeType, changeEntry.ChangedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry");
            // Don't throw - audit logging failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Get change log entries with filtering
    /// </summary>
    public async Task<IEnumerable<ChangeLogEntry>> GetChangeLogAsync(
        string? enumType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? changedBy = null)
    {
        try
        {
            if (!File.Exists(_auditLogPath))
            {
                return Enumerable.Empty<ChangeLogEntry>();
            }

            var lines = await File.ReadAllLinesAsync(_auditLogPath);
            var entries = new List<ChangeLogEntry>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var logEntry = JsonSerializer.Deserialize<ChangeLogEntry>(line);
                    if (logEntry != null)
                    {
                        // Apply filters
                        if (enumType != null && logEntry.EnumType != enumType) continue;
                        if (fromDate.HasValue && logEntry.Timestamp < fromDate.Value) continue;
                        if (toDate.HasValue && logEntry.Timestamp > toDate.Value) continue;
                        if (changedBy != null && logEntry.ChangedBy != changedBy) continue;

                        entries.Add(logEntry);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse audit log line: {Line}", line);
                }
            }

            return entries.OrderByDescending(e => e.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read change log");
            return Enumerable.Empty<ChangeLogEntry>();
        }
    }

    /// <summary>
    /// Compare two versions and get differences
    /// </summary>
    public async Task<VersionComparisonResult> CompareVersionsAsync(string fromVersion, string toVersion)
    {
        try
        {
            // Read current configuration to get version history
            var configContent = await File.ReadAllTextAsync(_configurationPath);
            var config = JsonSerializer.Deserialize<EnumConfiguration>(configContent);
            
            if (config?.Metadata?.VersionHistory == null)
            {
                throw new InvalidOperationException("No version history available");
            }

            // Find version entries
            var fromVersionEntry = config.Metadata.VersionHistory
                .FirstOrDefault(v => v.Version == fromVersion);
            var toVersionEntry = config.Metadata.VersionHistory
                .FirstOrDefault(v => v.Version == toVersion);

            if (fromVersionEntry == null)
                throw new ArgumentException($"Version {fromVersion} not found");
            if (toVersionEntry == null)
                throw new ArgumentException($"Version {toVersion} not found");

            // Read both configurations
            var fromConfig = JsonSerializer.Deserialize<EnumConfiguration>(
                await File.ReadAllTextAsync(fromVersionEntry.BackupFilePath));
            var toConfig = JsonSerializer.Deserialize<EnumConfiguration>(
                await File.ReadAllTextAsync(toVersionEntry.BackupFilePath));

            // Compare configurations
            var differences = CompareConfigurations(fromConfig!, toConfig!);
            var changeSummary = CalculateChangeSummary(differences);

            return new VersionComparisonResult
            {
                FromVersion = fromVersion,
                ToVersion = toVersion,
                Differences = differences,
                ChangeSummary = changeSummary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare versions {FromVersion} and {ToVersion}", fromVersion, toVersion);
            throw;
        }
    }

    /// <summary>
    /// Export configuration with optional version history
    /// </summary>
    public async Task<bool> ExportConfigurationAsync(string filePath, bool includeHistory = false)
    {
        try
        {
            var configContent = await File.ReadAllTextAsync(_configurationPath);
            
            if (!includeHistory)
            {
                // Export just the current configuration without history
                var config = JsonSerializer.Deserialize<EnumConfiguration>(configContent);
                if (config?.Metadata != null)
                {
                    config.Metadata.VersionHistory.Clear();
                    config.Metadata.ChangeLog.Clear();
                }
                
                configContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            await File.WriteAllTextAsync(filePath, configContent);
            
            _logger.LogInformation("Exported enum configuration to {FilePath} (includeHistory: {IncludeHistory})", 
                filePath, includeHistory);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export configuration to {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Get the current version information
    /// </summary>
    public async Task<EnumVersion> GetCurrentVersionAsync()
    {
        try
        {
            var configContent = await File.ReadAllTextAsync(_configurationPath);
            var config = JsonSerializer.Deserialize<EnumConfiguration>(configContent);
            
            if (config?.Metadata == null)
            {
                return new EnumVersion
                {
                    Version = "1.0.0",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    Description = "Initial version"
                };
            }

            if (config.Metadata.VersionHistory?.Any() == true)
            {
                return new EnumVersion
                {
                    Version = config.Metadata.Version ?? "1.0.0",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    Description = "Current version"
                };
            }

            var latestVersion = config.Metadata.VersionHistory.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
            return new EnumVersion
            {
                Version = latestVersion.Version,
                CreatedAt = latestVersion.CreatedAt,
                CreatedBy = latestVersion.CreatedBy,
                Description = latestVersion.Description
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current version");
            throw;
        }
    }

    /// <summary>
    /// Get a specific version by ID
    /// </summary>
    public async Task<EnumVersion> GetVersionAsync(string versionId)
    {
        try
        {
            var configContent = await File.ReadAllTextAsync(_configurationPath);
            var config = JsonSerializer.Deserialize<EnumConfiguration>(configContent);
            
            var versionEntry = config?.Metadata?.VersionHistory
                .FirstOrDefault(v => v.Version == versionId);

            if (versionEntry == null)
            {
                throw new ArgumentException($"Version {versionId} not found");
            }

            return new EnumVersion
            {
                Version = versionEntry.Version,
                CreatedAt = versionEntry.CreatedAt,
                CreatedBy = versionEntry.CreatedBy,
                Description = versionEntry.Description
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get version {VersionId}", versionId);
            throw;
        }
    }

    /// <summary>
    /// Get all available versions
    /// </summary>
    public async Task<IEnumerable<EnumVersion>> GetVersionsAsync()
    {
        try
        {
            var configContent = await File.ReadAllTextAsync(_configurationPath);
            var config = JsonSerializer.Deserialize<EnumConfiguration>(configContent);
            
            if (config?.Metadata?.VersionHistory == null)
            {
                return new List<EnumVersion>();
            }

            return config.Metadata.VersionHistory.Select(v => new EnumVersion
            {
                Version = v.Version,
                CreatedAt = v.CreatedAt,
                CreatedBy = v.CreatedBy,
                Description = v.Description
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get versions");
            throw;
        }
    }

    /// <summary>
    /// Create a new version (overload for EnumVersion parameter)
    /// </summary>
    public async Task<EnumVersion> CreateVersionAsync(EnumVersion version)
    {
        var versionId = await CreateVersionAsync(version.Description, version.CreatedBy);
        return new EnumVersion
        {
            Version = versionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = version.CreatedBy,
            Description = version.Description
        };
    }

    /// <summary>
    /// Update an existing version (placeholder implementation)
    /// </summary>
    public async Task<EnumVersion> UpdateVersionAsync(EnumVersion version)
    {
        // For now, just return the version as-is since version history is typically immutable
        // In a real implementation, you might update metadata or description
        _logger.LogWarning("UpdateVersionAsync called - version history is typically immutable");
        return version;
    }

    /// <summary>
    /// Delete a version (placeholder implementation)
    /// </summary>
    public async Task DeleteVersionAsync(string versionId)
    {
        try
        {
            var configContent = await File.ReadAllTextAsync(_configurationPath);
            var config = JsonSerializer.Deserialize<EnumConfiguration>(configContent);
            
            if (config?.Metadata?.VersionHistory != null)
            {
                var versionToRemove = config.Metadata.VersionHistory
                    .FirstOrDefault(v => v.Version == versionId);
                
                if (versionToRemove != null)
                {
                    config.Metadata.VersionHistory.Remove(versionToRemove);
                    
                    var updatedContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    await File.WriteAllTextAsync(_configurationPath, updatedContent);
                    
                    _logger.LogInformation("Deleted version {VersionId}", versionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete version {VersionId}", versionId);
            throw;
        }
    }

    #region Private Helper Methods

    private string GenerateVersionId()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd.HHmmss") + "." + 
               Random.Shared.Next(1000, 9999).ToString();
    }

    private string CalculateFileHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes);
    }

    private async Task<VersionChangeSummary> CalculateChangeSummaryAsync(EnumConfiguration config)
    {
        // This is a simplified implementation - in practice, you'd compare with the previous version
        var summary = new VersionChangeSummary();
        
        if (config.GlobalEnums != null)
        {
            summary.EnumsAdded = config.GlobalEnums.Count;
            summary.ValuesAdded = config.GlobalEnums.Values
                .SelectMany(e => e.Values?.Values ?? Enumerable.Empty<EnumValue>())
                .Count();
            summary.AffectedEnumTypes = config.GlobalEnums.Keys.ToList();
        }

        return summary;
    }

    private List<VersionDifference> CompareConfigurations(EnumConfiguration from, EnumConfiguration to)
    {
        var differences = new List<VersionDifference>();
        
        // This is a simplified comparison - a full implementation would do deep comparison
        // of all enum definitions, values, overrides, etc.
        
        // Compare global enums
        var fromEnums = from.GlobalEnums?.Keys ?? Enumerable.Empty<string>();
        var toEnums = to.GlobalEnums?.Keys ?? Enumerable.Empty<string>();
        
        foreach (var enumType in toEnums.Except(fromEnums))
        {
            differences.Add(new VersionDifference
            {
                DifferenceType = DifferenceType.Added,
                Path = $"GlobalEnums.{enumType}",
                Description = $"Added enum type '{enumType}'",
                NewValue = enumType,
                Severity = ChangeSeverity.Minor
            });
        }
        
        foreach (var enumType in fromEnums.Except(toEnums))
        {
            differences.Add(new VersionDifference
            {
                DifferenceType = DifferenceType.Removed,
                Path = $"GlobalEnums.{enumType}",
                Description = $"Removed enum type '{enumType}'",
                OldValue = enumType,
                Severity = ChangeSeverity.Major
            });
        }

        return differences;
    }

    private VersionChangeSummary CalculateChangeSummary(List<VersionDifference> differences)
    {
        var summary = new VersionChangeSummary();
        
        foreach (var diff in differences)
        {
            switch (diff.DifferenceType)
            {
                case DifferenceType.Added:
                    if (diff.Path.Contains("GlobalEnums.") && !diff.Path.Contains(".Values."))
                        summary.EnumsAdded++;
                    else
                        summary.ValuesAdded++;
                    break;
                case DifferenceType.Removed:
                    if (diff.Path.Contains("GlobalEnums.") && !diff.Path.Contains(".Values."))
                        summary.EnumsRemoved++;
                    else
                        summary.ValuesRemoved++;
                    break;
                case DifferenceType.Modified:
                    if (diff.Path.Contains("GlobalEnums.") && !diff.Path.Contains(".Values."))
                        summary.EnumsModified++;
                    else
                        summary.ValuesModified++;
                    break;
            }
        }

        summary.AffectedEnumTypes = differences
            .Select(d => d.Path.Split('.')[1])
            .Distinct()
            .ToList();

        return summary;
    }

    #endregion
}
