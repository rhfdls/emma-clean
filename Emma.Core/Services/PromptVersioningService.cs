using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq; // Add this namespace

namespace Emma.Core.Services;

/// <summary>
/// Provides comprehensive versioning, audit logging, and rollback capabilities for prompt configurations
/// Implements enterprise-grade configuration governance and operational safety
/// </summary>
public class PromptVersioningService : IDisposable
{
    private bool _disposed = false; // To detect redundant calls

    private readonly ILogger<PromptVersioningService> _logger;
    private readonly string _configurationPath;
    private readonly string _backupDirectory;
    private readonly string _auditLogPath;

    public PromptVersioningService(
        ILogger<PromptVersioningService> logger,
        string configurationPath,
        string? backupDirectory = null)
    {
        _logger = logger;
        _configurationPath = configurationPath;
        _backupDirectory = backupDirectory ?? Path.Combine(Path.GetDirectoryName(configurationPath)!, "Backups");
        _auditLogPath = Path.Combine(Path.GetDirectoryName(configurationPath)!, "prompt-audit.log");

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
            var config = JsonSerializer.Deserialize<PromptConfiguration>(configContent);
            
            if (config?.Metadata == null)
            {
                config = config ?? new PromptConfiguration();
                config.Metadata = new PromptMetadata();
            }

            // Create backup file
            var backupFileName = $"prompts-v{version}-{timestamp:yyyyMMdd-HHmmss}.json";
            var backupFilePath = Path.Combine(_backupDirectory, backupFileName);
            await File.WriteAllTextAsync(backupFilePath, configContent);

            // Calculate file hash for integrity
            var configHash = CalculateFileHash(configContent);
            var fileInfo = new FileInfo(_configurationPath);

            // Create version history entry
            var versionEntry = new PromptVersionHistoryEntry
            {
                Version = version,
                CreatedAt = timestamp,
                CreatedBy = createdBy,
                Description = description,
                BackupFilePath = backupFilePath,
                ConfigurationHash = configHash,
                FileSizeBytes = fileInfo.Length,
                Tags = tags?.ToList() ?? new List<string>(), // Correct misuse of ?? operator
                ChangeSummary = await CalculateChangeSummaryAsync(config)
            };

            // Add to version history
            if (config.Metadata.VersionHistory == null)
                config.Metadata.VersionHistory = new List<PromptVersionHistoryEntry>();
            
            config.Metadata.VersionHistory.Add(versionEntry);
            config.Metadata.Version = version;
            config.Metadata.LastModified = timestamp;
            config.Metadata.ModifiedBy = createdBy;

            // Save updated configuration
            var updatedContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_configurationPath, updatedContent);

            // Log the version creation
            await LogChangeAsync(new PromptChangeLogEntry
            {
                ChangeType = PromptChangeType.Create,
                ChangedBy = createdBy,
                Description = $"Created version {version}: {description}",
                Metadata = new Dictionary<string, object>
                {
                    ["version"] = version,
                    ["backupPath"] = backupFilePath,
                    ["tags"] = tags?.ToList() ?? new List<string>() // Correct misuse of ?? operator
                }
            });

            _logger.LogInformation("Created prompt configuration version {Version} by {User}: {Description}", 
                version, createdBy, description);

            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create prompt configuration version");
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
            var config = JsonSerializer.Deserialize<PromptConfiguration>(configContent);
            
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

            // Create a pre-rollback backup
            var currentBackupVersion = await CreateVersionAsync($"Pre-rollback backup before reverting to {version}", rolledBackBy);

            // Verify backup integrity
            var targetContent = await File.ReadAllTextAsync(targetVersion.BackupFilePath);
            var targetHash = CalculateFileHash(targetContent);
            
            if (targetHash != targetVersion.ConfigurationHash)
            {
                _logger.LogError("Backup file integrity check failed for version {Version}", version);
                return false;
            }

            // Perform rollback
            await File.WriteAllTextAsync(_configurationPath, targetContent);

            // Log the rollback
            await LogChangeAsync(new PromptChangeLogEntry
            {
                ChangeType = PromptChangeType.Rollback,
                ChangedBy = rolledBackBy,
                Description = $"Rolled back to version {version}: {reason}",
                Metadata = new Dictionary<string, object>
                {
                    ["targetVersion"] = version,
                    ["reason"] = reason,
                    ["preRollbackVersion"] = currentBackupVersion
                }
            });

            _logger.LogInformation("Successfully rolled back prompt configuration to version {Version} by {User}: {Reason}", 
                version, rolledBackBy, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback prompt configuration to version {Version}", version);
            return false;
        }
    }

    /// <summary>
    /// Get version history
    /// </summary>
    public async Task<IEnumerable<PromptVersionHistoryEntry>> GetVersionHistoryAsync()
    {
        try
        {
            if (!File.Exists(_configurationPath))
                return Enumerable.Empty<PromptVersionHistoryEntry>();

            var configContent = await File.ReadAllTextAsync(_configurationPath);
            var config = JsonSerializer.Deserialize<PromptConfiguration>(configContent);
            
            return (config?.Metadata?.VersionHistory?.OrderByDescending(v => v.CreatedAt).AsEnumerable() 
                   ?? Enumerable.Empty<PromptVersionHistoryEntry>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt version history");
            return Enumerable.Empty<PromptVersionHistoryEntry>();
        }
    }

    /// <summary>
    /// Compare two versions and return differences
    /// </summary>
    public async Task<PromptVersionComparisonResult> CompareVersionsAsync(string version1, string version2)
    {
        try
        {
            var config = await GetCurrentConfigurationAsync();
            var history = config?.Metadata?.VersionHistory ?? new List<PromptVersionHistoryEntry>();

            var v1Entry = history.FirstOrDefault(v => v.Version == version1);
            var v2Entry = history.FirstOrDefault(v => v.Version == version2);

            if (v1Entry == null || v2Entry == null)
            {
                throw new ArgumentException("One or both versions not found");
            }

            var v1Content = await File.ReadAllTextAsync(v1Entry.BackupFilePath);
            var v2Content = await File.ReadAllTextAsync(v2Entry.BackupFilePath);

            var v1Config = JsonSerializer.Deserialize<PromptConfiguration>(v1Content);
            var v2Config = JsonSerializer.Deserialize<PromptConfiguration>(v2Content);

            return new PromptVersionComparisonResult
            {
                Version1 = version1,
                Version2 = version2,
                Differences = CalculateConfigurationDifferences(v1Config!, v2Config!),
                Version1Metadata = v1Entry,
                Version2Metadata = v2Entry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare prompt versions {Version1} and {Version2}", version1, version2);
            throw;
        }
    }

    /// <summary>
    /// Get filtered change log entries
    /// </summary>
    public async Task<IEnumerable<PromptChangeLogEntry>> GetChangeLogAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? agentType = null,
        string? changedBy = null,
        PromptChangeType? changeType = null)
    {
        try
        {
            if (!File.Exists(_auditLogPath))
                return Enumerable.Empty<PromptChangeLogEntry>();

            var lines = await File.ReadAllLinesAsync(_auditLogPath);
            var entries = new List<PromptChangeLogEntry>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var entry = JsonSerializer.Deserialize<PromptChangeLogEntry>(line);
                    if (entry != null)
                        entries.Add(entry);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse audit log entry: {Line}", line);
                }
            }

            // Apply filters
            var filtered = entries.AsEnumerable();

            if (fromDate.HasValue)
                filtered = filtered.Where(e => e.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                filtered = filtered.Where(e => e.Timestamp <= toDate.Value);

            if (!string.IsNullOrEmpty(agentType))
                filtered = filtered.Where(e => e.AgentType == agentType);

            if (!string.IsNullOrEmpty(changedBy))
                filtered = filtered.Where(e => e.ChangedBy == changedBy);

            if (changeType.HasValue)
                filtered = filtered.Where(e => e.ChangeType == changeType.Value);

            return filtered.OrderByDescending(e => e.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt change log");
            return Enumerable.Empty<PromptChangeLogEntry>();
        }
    }

    /// <summary>
    /// Export configuration to file
    /// </summary>
    public async Task<string> ExportConfigurationAsync(string? version = null)
    {
        try
        {
            string content;
            string fileName;

            if (string.IsNullOrEmpty(version))
            {
                // Export current configuration
                content = await File.ReadAllTextAsync(_configurationPath);
                fileName = $"prompts-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            }
            else
            {
                // Export specific version
                var config = await GetCurrentConfigurationAsync();
                var versionEntry = config?.Metadata?.VersionHistory?
                    .FirstOrDefault(v => v.Version == version);

                if (versionEntry == null)
                    throw new ArgumentException($"Version {version} not found");

                content = await File.ReadAllTextAsync(versionEntry.BackupFilePath);
                fileName = $"prompts-export-v{version}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            }

            var exportPath = Path.Combine(_backupDirectory, fileName);
            await File.WriteAllTextAsync(exportPath, content);

            _logger.LogInformation("Exported prompt configuration to {ExportPath}", exportPath);
            return exportPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export prompt configuration");
            throw;
        }
    }

    /// <summary>
    /// Import configuration from file
    /// </summary>
    public async Task<bool> ImportConfigurationAsync(string importFilePath, string importedBy, PromptMergeStrategy mergeStrategy = PromptMergeStrategy.Replace)
    {
        try
        {
            if (!File.Exists(importFilePath))
                throw new FileNotFoundException($"Import file not found: {importFilePath}");

            var importContent = await File.ReadAllTextAsync(importFilePath);
            var importConfig = JsonSerializer.Deserialize<PromptConfiguration>(importContent);

            if (importConfig == null)
                throw new InvalidOperationException("Failed to deserialize import configuration");

            // Create backup before import
            await CreateVersionAsync($"Pre-import backup before importing from {Path.GetFileName(importFilePath)}", importedBy);

            PromptConfiguration finalConfig;

            switch (mergeStrategy)
            {
                case PromptMergeStrategy.Replace:
                    finalConfig = importConfig;
                    break;

                case PromptMergeStrategy.Merge:
                    var currentConfig = await GetCurrentConfigurationAsync();
                    finalConfig = MergeConfigurations(currentConfig!, importConfig);
                    break;

                case PromptMergeStrategy.KeepExisting:
                    var existingConfig = await GetCurrentConfigurationAsync();
                    finalConfig = MergeConfigurations(importConfig, existingConfig!);
                    break;

                default:
                    throw new ArgumentException($"Unsupported merge strategy: {mergeStrategy}");
            }

            // Update metadata
            finalConfig.Metadata = finalConfig.Metadata ?? new PromptMetadata();
            finalConfig.Metadata.LastModified = DateTime.UtcNow;
            finalConfig.Metadata.ModifiedBy = importedBy;

            // Save merged configuration
            var finalContent = JsonSerializer.Serialize(finalConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_configurationPath, finalContent);

            // Log the import
            await LogChangeAsync(new PromptChangeLogEntry
            {
                ChangeType = PromptChangeType.Import,
                ChangedBy = importedBy,
                Description = $"Imported configuration from {Path.GetFileName(importFilePath)} using {mergeStrategy} strategy",
                Metadata = new Dictionary<string, object>
                {
                    ["importFile"] = importFilePath,
                    ["mergeStrategy"] = mergeStrategy.ToString()
                }
            });

            _logger.LogInformation("Successfully imported prompt configuration from {ImportFile} using {MergeStrategy} strategy", 
                importFilePath, mergeStrategy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import prompt configuration from {ImportFile}", importFilePath);
            return false;
        }
    }

    /// <summary>
    /// Get current configuration metadata
    /// </summary>
    public async Task<PromptMetadata?> GetConfigurationMetadataAsync()
    {
        try
        {
            var config = await GetCurrentConfigurationAsync();
            return config?.Metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt configuration metadata");
            return null;
        }
    }

    #region Private Helper Methods

    private async Task<PromptConfiguration?> GetCurrentConfigurationAsync()
    {
        if (!File.Exists(_configurationPath))
            return null;

        var content = await File.ReadAllTextAsync(_configurationPath);
        return JsonSerializer.Deserialize<PromptConfiguration>(content);
    }

    private string GenerateVersionId()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd.HHmmss");
    }

    private string CalculateFileHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }

    private async Task<Dictionary<string, object>> CalculateChangeSummaryAsync(PromptConfiguration config)
    {
        return new Dictionary<string, object>
        {
            ["agentCount"] = config.Agents?.Count ?? 0,
            ["industryCount"] = config.Industries?.Count ?? 0,
            ["globalTemplateCount"] = config.GlobalTemplates?.Count ?? 0,
            ["totalTemplates"] = config.Agents?.Values.Sum(a => 
                (a.ContextTemplates?.Count ?? 0) + 
                (a.ActionTemplates?.Count ?? 0) + 
                (a.ResponseFormats?.Count ?? 0)) ?? 0
        };
    }

    private async Task LogChangeAsync(PromptChangeLogEntry entry)
    {
        try
        {
            entry.Timestamp = DateTime.UtcNow;
            entry.Id = Guid.NewGuid().ToString();

            var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.AppendAllTextAsync(_auditLogPath, json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to prompt audit log");
        }
    }

    private List<string> CalculateConfigurationDifferences(PromptConfiguration config1, PromptConfiguration config2)
    {
        var differences = new List<string>();

        // Compare agents
        var agents1 = config1.Agents?.Keys.ToList(); // Correct misuse of ?? operator
        var agents2 = config2.Agents?.Keys.ToList(); // Correct misuse of ?? operator

        var addedAgents = agents2.Except(agents1);
        var removedAgents = agents1.Except(agents2);
        var commonAgents = agents1.Intersect(agents2);

        differences.AddRange(addedAgents.Select(a => $"Added agent: {a}"));
        differences.AddRange(removedAgents.Select(a => $"Removed agent: {a}"));

        foreach (var agent in commonAgents)
        {
            var agent1 = config1.Agents![agent];
            var agent2 = config2.Agents![agent];

            if (agent1.SystemPrompt != agent2.SystemPrompt)
                differences.Add($"Modified system prompt for agent: {agent}");

            // Compare templates
            var templates1 = agent1.ContextTemplates?.Keys.ToList(); // Correct misuse of ?? operator
            var templates2 = agent2.ContextTemplates?.Keys.ToList(); // Correct misuse of ?? operator

            differences.AddRange(templates2.Except(templates1).Select(t => $"Added context template '{t}' to agent {agent}"));
            differences.AddRange(templates1.Except(templates2).Select(t => $"Removed context template '{t}' from agent {agent}"));
        }

        // Compare industries
        var industries1 = config1.Industries?.Keys.ToList(); // Correct misuse of ?? operator
        var industries2 = config2.Industries?.Keys.ToList(); // Correct misuse of ?? operator

        differences.AddRange(industries2.Except(industries1).Select(i => $"Added industry: {i}"));
        differences.AddRange(industries1.Except(industries2).Select(i => $"Removed industry: {i}"));

        return differences;
    }

    private PromptConfiguration MergeConfigurations(PromptConfiguration primary, PromptConfiguration secondary)
    {
        var merged = new PromptConfiguration
        {
            Agents = new Dictionary<string, AgentPromptSet>(primary.Agents ?? new()),
            Industries = new Dictionary<string, IndustryPromptOverrides>(primary.Industries ?? new()),
            GlobalTemplates = new Dictionary<string, string>(primary.GlobalTemplates ?? new()),
            Metadata = primary.Metadata ?? new PromptMetadata()
        };

        // Merge agents from secondary
        foreach (var kvp in secondary.Agents ?? new Dictionary<string, AgentPromptSet>())
        {
            if (!merged.Agents.ContainsKey(kvp.Key))
            {
                merged.Agents[kvp.Key] = kvp.Value;
            }
        }

        // Merge industries from secondary
        foreach (var kvp in secondary.Industries ?? new Dictionary<string, IndustryPromptOverrides>())
        {
            if (!merged.Industries.ContainsKey(kvp.Key))
            {
                merged.Industries[kvp.Key] = kvp.Value;
            }
        }

        // Merge global templates from secondary
        foreach (var kvp in secondary.GlobalTemplates ?? new Dictionary<string, string>())
        {
            if (!merged.GlobalTemplates.ContainsKey(kvp.Key))
            {
                merged.GlobalTemplates[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Free any other managed objects here.
            // If there are any file streams or other IDisposable resources, dispose them here.
        }

        // Free any unmanaged resources here.

        _disposed = true;
    }

    ~PromptVersioningService()
    {
        Dispose(false);
    }
}
