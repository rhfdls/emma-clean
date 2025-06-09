using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Industry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Emma.Core.Services;

/// <summary>
/// Provides dynamic prompt management for AI agents
/// Loads prompts from external configuration files for business configurability
/// </summary>
public class PromptProvider : IPromptProvider, IDisposable
{
    private readonly ILogger<PromptProvider> _logger;
    private readonly string _configurationPath;
    private readonly bool _enableHotReload;
    private readonly PromptVersioningService _versioningService;
    private PromptConfiguration? _promptConfiguration;
    private readonly object _lockObject = new();
    private FileSystemWatcher? _fileWatcher;
    private DateTime _lastReloadTime = DateTime.MinValue;

    public PromptProvider(ILogger<PromptProvider> logger, string configurationPath, bool enableHotReload = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationPath = configurationPath ?? throw new ArgumentNullException(nameof(configurationPath));
        _enableHotReload = enableHotReload;
        
        // Initialize versioning service
        var versioningLogger = _logger as ILogger<PromptVersioningService> ?? 
            new LoggerFactory().CreateLogger<PromptVersioningService>();
        _versioningService = new PromptVersioningService(versioningLogger, _configurationPath);
        
        // Load initial configuration
        _ = Task.Run(LoadConfigurationAsync);
        
        // Set up file watcher for hot reload if enabled
        if (_enableHotReload)
        {
            SetupFileWatcher();
        }
    }

    public PromptProvider(IOptions<PromptProviderConfig> config, ILogger<PromptProvider> logger)
        : this(logger, config.Value?.ConfigurationFilePath ?? "Configuration/prompts.json", config.Value?.EnableHotReload ?? true)
    {
    }

    public async Task<string> GetSystemPromptAsync(string agentType, IIndustryProfile industryProfile)
    {
        await EnsureConfigurationLoadedAsync();
        
        lock (_lockObject)
        {
            try
            {
                // Try industry-specific override first
                if (_promptConfiguration?.Industries?.TryGetValue(industryProfile.IndustryCode, out var industryOverrides) == true &&
                    industryOverrides.AgentOverrides?.TryGetValue(agentType, out var industryAgentPrompt) == true &&
                    !string.IsNullOrEmpty(industryAgentPrompt.SystemPrompt))
                {
                    _logger.LogDebug("Using industry-specific system prompt for {AgentType} in {Industry}", 
                        agentType, industryProfile.IndustryCode);
                    return industryAgentPrompt.SystemPrompt;
                }

                // Fall back to default agent prompt
                if (_promptConfiguration?.Agents?.TryGetValue(agentType, out var agentPrompt) == true &&
                    !string.IsNullOrEmpty(agentPrompt.SystemPrompt))
                {
                    _logger.LogDebug("Using default system prompt for {AgentType}", agentType);
                    return agentPrompt.SystemPrompt;
                }

                // Ultimate fallback
                _logger.LogWarning("No system prompt found for {AgentType}, using fallback", agentType);
                return $"You are an AI assistant specialized in {agentType} tasks. Provide helpful, accurate, and professional assistance.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system prompt for {AgentType}", agentType);
                throw new InvalidOperationException($"Failed to retrieve system prompt for {agentType}", ex);
            }
        }
    }

    public async Task<string> BuildPromptAsync(string templateName, Dictionary<string, object> context)
    {
        await EnsureConfigurationLoadedAsync();

        try
        {
            var template = await GetTemplateAsync(templateName, context);
            
            if (string.IsNullOrEmpty(template))
            {
                throw new ArgumentException($"Template '{templateName}' not found");
            }

            // Validate required placeholders
            var validationResult = await ValidatePromptAsync(templateName, template);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Template validation failed for {TemplateName}: {Errors}", 
                    templateName, string.Join(", ", validationResult.Errors));
            }

            // Substitute placeholders
            var result = SubstitutePlaceholders(template, context);
            
            _logger.LogDebug("Built prompt from template {TemplateName}, length: {Length} chars", 
                templateName, result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building prompt from template {TemplateName}", templateName);
            throw new InvalidOperationException($"Failed to build prompt from template '{templateName}'", ex);
        }
    }

    public async Task<Dictionary<string, string>> GetAgentTemplatesAsync(string agentType)
    {
        await EnsureConfigurationLoadedAsync();
        
        lock (_lockObject)
        {
            var templates = new Dictionary<string, string>();

            if (_promptConfiguration?.Agents?.TryGetValue(agentType, out var agentPrompt) == true)
            {
                // Add context templates
                foreach (var kvp in agentPrompt.ContextTemplates ?? new Dictionary<string, string>())
                {
                    templates[$"context.{kvp.Key}"] = kvp.Value;
                }

                // Add action templates
                foreach (var kvp in agentPrompt.ActionTemplates ?? new Dictionary<string, string>())
                {
                    templates[$"action.{kvp.Key}"] = kvp.Value;
                }

                // Add response format templates
                foreach (var kvp in agentPrompt.ResponseFormats ?? new Dictionary<string, string>())
                {
                    templates[$"response.{kvp.Key}"] = kvp.Value;
                }
            }

            // Add global templates
            foreach (var kvp in _promptConfiguration?.GlobalTemplates ?? new Dictionary<string, string>())
            {
                templates[$"global.{kvp.Key}"] = kvp.Value;
            }

            _logger.LogDebug("Retrieved {Count} templates for agent {AgentType}", templates.Count, agentType);
            return templates;
        }
    }

    public async Task<PromptValidationResult> ValidatePromptAsync(string templateName, string promptContent)
    {
        var result = new PromptValidationResult { IsValid = true };

        try
        {
            // Extract placeholders
            var placeholderPattern = @"\{\{([^}]+)\}\}";
            var matches = Regex.Matches(promptContent, placeholderPattern);
            
            foreach (Match match in matches)
            {
                var placeholder = match.Groups[1].Value.Trim();
                result.RequiredPlaceholders.Add(placeholder);
            }

            // Check for template syntax issues
            if (promptContent.Contains("{{") && !promptContent.Contains("}}"))
            {
                result.IsValid = false;
                result.Errors.Add("Unclosed placeholder found");
            }

            if (promptContent.Contains("}}") && !promptContent.Contains("{{"))
            {
                result.IsValid = false;
                result.Errors.Add("Unopened placeholder found");
            }

            // Check for conditional block syntax
            var conditionalPattern = @"\{\%\s*if\s+([^%]+)\s*\%\}";
            var conditionalMatches = Regex.Matches(promptContent, conditionalPattern);
            var endifPattern = @"\{\%\s*endif\s*\%\}";
            var endifMatches = Regex.Matches(promptContent, endifPattern);

            if (conditionalMatches.Count != endifMatches.Count)
            {
                result.IsValid = false;
                result.Errors.Add("Mismatched conditional blocks (if/endif)");
            }

            _logger.LogDebug("Validated template {TemplateName}: {IsValid}, {PlaceholderCount} placeholders", 
                templateName, result.IsValid, result.RequiredPlaceholders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating template {TemplateName}", templateName);
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    public async Task ReloadPromptsAsync()
    {
        _logger.LogInformation("Manually reloading prompt configuration");
        await LoadConfigurationAsync();
    }

    // ===== VERSIONING AND AUDIT CAPABILITIES =====

    public async Task<string> CreateVersionAsync(string description, string createdBy, List<string>? tags = null)
    {
        try
        {
            var version = await _versioningService.CreateVersionAsync(description, createdBy, tags);
            _logger.LogInformation("Created prompt configuration version {Version} by {User}", version, createdBy);
            
            // Reload configuration to pick up version metadata changes
            await LoadConfigurationAsync();
            
            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create prompt configuration version");
            throw;
        }
    }

    public async Task<bool> RollbackToVersionAsync(string version, string rolledBackBy, string reason)
    {
        try
        {
            var success = await _versioningService.RollbackToVersionAsync(version, rolledBackBy, reason);
            
            if (success)
            {
                _logger.LogInformation("Successfully rolled back prompt configuration to version {Version} by {User}", 
                    version, rolledBackBy);
                
                // Reload configuration after rollback
                await LoadConfigurationAsync();
            }
            else
            {
                _logger.LogWarning("Failed to rollback prompt configuration to version {Version}", version);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during prompt configuration rollback to version {Version}", version);
            return false;
        }
    }

    public async Task<IEnumerable<PromptVersionHistoryEntry>> GetVersionHistoryAsync()
    {
        try
        {
            return await _versioningService.GetVersionHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt version history");
            return Enumerable.Empty<PromptVersionHistoryEntry>();
        }
    }

    public async Task<PromptVersionComparisonResult> CompareVersionsAsync(string version1, string version2)
    {
        try
        {
            return await _versioningService.CompareVersionsAsync(version1, version2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare prompt versions {Version1} and {Version2}", version1, version2);
            throw;
        }
    }

    public async Task<IEnumerable<PromptChangeLogEntry>> GetChangeLogAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? agentType = null,
        string? changedBy = null,
        PromptChangeType? changeType = null)
    {
        try
        {
            return await _versioningService.GetChangeLogAsync(fromDate, toDate, agentType, changedBy, changeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt change log");
            return Enumerable.Empty<PromptChangeLogEntry>();
        }
    }

    public async Task<string> ExportConfigurationAsync(string? version = null)
    {
        try
        {
            return await _versioningService.ExportConfigurationAsync(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export prompt configuration");
            throw;
        }
    }

    public async Task<bool> ImportConfigurationAsync(string importFilePath, string importedBy, PromptMergeStrategy mergeStrategy = PromptMergeStrategy.Replace)
    {
        try
        {
            var success = await _versioningService.ImportConfigurationAsync(importFilePath, importedBy, mergeStrategy);
            
            if (success)
            {
                _logger.LogInformation("Successfully imported prompt configuration from {ImportFile} by {User}", 
                    importFilePath, importedBy);
                
                // Reload configuration after import
                await LoadConfigurationAsync();
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import prompt configuration from {ImportFile}", importFilePath);
            return false;
        }
    }

    public async Task<PromptMetadata?> GetConfigurationMetadataAsync()
    {
        try
        {
            return await _versioningService.GetConfigurationMetadataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve prompt configuration metadata");
            return null;
        }
    }

    #region Private Helper Methods

    private async Task<string> GetTemplateAsync(string templateName, Dictionary<string, object> context)
    {
        await EnsureConfigurationLoadedAsync();
        
        lock (_lockObject)
        {
            // Parse template name (format: agent.type.name or global.name)
            var parts = templateName.Split('.');
            
            if (parts.Length >= 3 && parts[0] != "global")
            {
                // Agent-specific template: agent.context.templateName
                var agentType = parts[0];
                var templateType = parts[1]; // context, action, response
                var templateKey = string.Join(".", parts.Skip(2));

                if (_promptConfiguration?.Agents?.TryGetValue(agentType, out var agentPrompt) == true)
                {
                    var templates = templateType switch
                    {
                        "context" => agentPrompt.ContextTemplates,
                        "action" => agentPrompt.ActionTemplates,
                        "response" => agentPrompt.ResponseFormats,
                        _ => null
                    };

                    if (templates?.TryGetValue(templateKey, out var template) == true)
                    {
                        return ProcessTemplate(template, context);
                    }
                }
            }
            else if (parts.Length >= 2 && parts[0] == "global")
            {
                // Global template: global.templateName
                var templateKey = string.Join(".", parts.Skip(1));
                
                if (_promptConfiguration?.GlobalTemplates?.TryGetValue(templateKey, out var template) == true)
                {
                    return ProcessTemplate(template, context);
                }
            }

            return string.Empty;
        }
    }

    private string ProcessTemplate(string template, Dictionary<string, object> context)
    {
        // Process template inheritance
        template = ProcessTemplateInheritance(template, context);
        
        // Process conditional blocks
        template = ProcessConditionalBlocks(template, context);
        
        // Substitute placeholders
        template = SubstitutePlaceholders(template, context);
        
        return template;
    }

    /// <summary>
    /// Process template inheritance with {% extends %} and {% block %} syntax
    /// </summary>
    private string ProcessTemplateInheritance(string template, Dictionary<string, object> context)
    {
        // Check for extends directive
        var extendsPattern = @"\{\%\s*extends\s+['""]([^'""]+)['""]\s*\%\}";
        var extendsMatch = Regex.Match(template, extendsPattern);
        
        if (extendsMatch.Success)
        {
            var baseTemplateName = extendsMatch.Groups[1].Value;
            var baseTemplate = GetBaseTemplateAsync(baseTemplateName).Result;
            
            if (!string.IsNullOrEmpty(baseTemplate))
            {
                // Extract blocks from child template
                var childBlocks = ExtractTemplateBlocks(template);
                
                // Replace blocks in base template
                var result = ProcessTemplateBlocks(baseTemplate, childBlocks);
                
                // Remove the extends directive from the result
                result = Regex.Replace(result, extendsPattern, string.Empty);
                
                return result;
            }
        }
        
        return template;
    }

    /// <summary>
    /// Get base template for inheritance
    /// </summary>
    private async Task<string> GetBaseTemplateAsync(string templateName)
    {
        return await GetTemplateAsync($"global.{templateName}", new Dictionary<string, object>());
    }

    /// <summary>
    /// Extract {% block name %}...{% endblock %} sections from template
    /// </summary>
    private Dictionary<string, string> ExtractTemplateBlocks(string template)
    {
        var blocks = new Dictionary<string, string>();
        var blockPattern = @"\{\%\s*block\s+([^%]+)\s*\%\}(.*?)\{\%\s*endblock\s*\%\}";
        var matches = Regex.Matches(template, blockPattern, RegexOptions.Singleline);
        
        foreach (Match match in matches)
        {
            var blockName = match.Groups[1].Value.Trim();
            var blockContent = match.Groups[2].Value;
            blocks[blockName] = blockContent;
        }
        
        return blocks;
    }

    /// <summary>
    /// Process template blocks, replacing {% block name %}...{% endblock %} with provided content
    /// </summary>
    private string ProcessTemplateBlocks(string template, Dictionary<string, string> blocks)
    {
        var blockPattern = @"\{\%\s*block\s+([^%]+)\s*\%\}(.*?)\{\%\s*endblock\s*\%\}";
        
        return Regex.Replace(template, blockPattern, match =>
        {
            var blockName = match.Groups[1].Value.Trim();
            var defaultContent = match.Groups[2].Value;
            
            // Use provided block content if available, otherwise use default
            return blocks.TryGetValue(blockName, out var blockContent) ? blockContent : defaultContent;
        }, RegexOptions.Singleline);
    }

    private string SubstitutePlaceholders(string template, Dictionary<string, object> context)
    {
        var placeholderPattern = @"\{\{([^}]+)\}\}";
        
        return Regex.Replace(template, placeholderPattern, match =>
        {
            var placeholder = match.Groups[1].Value.Trim();
            
            if (context.TryGetValue(placeholder, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }
            
            _logger.LogWarning("Placeholder '{Placeholder}' not found in context", placeholder);
            return match.Value; // Return original placeholder if not found
        });
    }

    /// <summary>
    /// Process conditional blocks in templates
    /// Supports: {% if VariableName %}content{% endif %}
    /// </summary>
    private string ProcessConditionalBlocks(string template, Dictionary<string, object> context)
    {
        var conditionalPattern = @"\{\%\s*if\s+([^%]+)\s*\%\}(.*?)\{\%\s*endif\s*\%\}";
        
        return Regex.Replace(template, conditionalPattern, match =>
        {
            var condition = match.Groups[1].Value.Trim();
            var content = match.Groups[2].Value;
            
            // Check if condition is truthy
            if (context.TryGetValue(condition, out var value) && IsValueTruthy(value))
            {
                return content;
            }
            
            return string.Empty; // Remove block if condition is false
        }, RegexOptions.Singleline);
    }

    /// <summary>
    /// Determine if a value should be considered "truthy" for conditional logic
    /// </summary>
    private bool IsValueTruthy(object? value)
    {
        if (value == null) return false;
        
        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            int i => i != 0,
            double d => d != 0.0,
            float f => f != 0.0f,
            decimal dec => dec != 0m,
            System.Collections.ICollection collection => collection.Count > 0,
            System.Collections.IEnumerable enumerable => enumerable.Cast<object>().Any(),
            _ => true // Non-null objects are truthy
        };
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configurationPath))
            {
                _logger.LogWarning("Prompt configuration file not found: {FilePath}", _configurationPath);
                return;
            }

            var json = await File.ReadAllTextAsync(_configurationPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var newConfiguration = JsonSerializer.Deserialize<PromptConfiguration>(json, options);
            
            if (newConfiguration == null)
            {
                _logger.LogError("Failed to deserialize prompt configuration from {FilePath}", _configurationPath);
                return;
            }

            lock (_lockObject)
            {
                _promptConfiguration = newConfiguration;
                _lastReloadTime = DateTime.UtcNow;
            }

            _logger.LogInformation("Loaded prompt configuration from {FilePath}, version: {Version}, agents: {AgentCount}",
                _configurationPath, 
                newConfiguration.Metadata?.Version ?? "Unknown",
                newConfiguration.Agents?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prompt configuration from {FilePath}", _configurationPath);
        }
    }

    private async Task EnsureConfigurationLoadedAsync()
    {
        if (_promptConfiguration == null)
        {
            await LoadConfigurationAsync();
        }
    }

    private void SetupFileWatcher()
    {
        try
        {
            if (!File.Exists(_configurationPath))
                return;

            var directory = Path.GetDirectoryName(_configurationPath);
            var fileName = Path.GetFileName(_configurationPath);

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                return;

            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += async (sender, e) =>
            {
                // Debounce rapid file changes
                if (DateTime.UtcNow - _lastReloadTime < TimeSpan.FromSeconds(2))
                    return;

                _logger.LogInformation("Prompt configuration file changed, reloading...");
                await LoadConfigurationAsync();
            };

            _logger.LogDebug("Set up file watcher for prompt configuration: {FilePath}", _configurationPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set up file watcher for prompt configuration");
        }
    }

    #endregion

    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _versioningService?.Dispose();
    }
}

/// <summary>
/// Configuration for PromptProvider
/// </summary>
public class PromptProviderConfig
{
    /// <summary>
    /// Path to the prompt configuration JSON file
    /// </summary>
    public string ConfigurationFilePath { get; set; } = "Configuration/prompts.json";
    
    /// <summary>
    /// Enable hot reload of prompt configuration
    /// </summary>
    public bool EnableHotReload { get; set; } = true;
}
