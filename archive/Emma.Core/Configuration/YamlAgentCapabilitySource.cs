using System;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Event arguments for the CapabilitiesReloaded event.
    /// </summary>
    public class CapabilitiesReloadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the path to the YAML file that was reloaded.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the timestamp when the capabilities were reloaded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the reload was successful.
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Gets or sets the error that occurred during reload, if any.
        /// </summary>
        public Exception? Error { get; set; }
    }

    /// <summary>
    /// Loads and parses YAML configuration files containing agent capabilities
    /// </summary>
    public interface IYamlAgentCapabilitySource
    {
        /// <summary>
        /// Event that is raised when agent capabilities are reloaded from the YAML file.
        /// </summary>
        event Func<object, CapabilitiesReloadedEventArgs, Task>? CapabilitiesReloaded;
        
        /// <summary>
        /// Loads and parses agent capabilities from YAML content
        /// </summary>
        Task<AgentCapabilityYaml> LoadCapabilitiesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the last time the capabilities were loaded
        /// </summary>
        DateTimeOffset? LastModified { get; }
        
        /// <summary>
        /// Gets whether the source has been loaded at least once
        /// </summary>
        bool IsLoaded { get; }
    }

    /// <summary>
    /// Options for configuring the YAML capability source
    /// </summary>
    public class YamlAgentCapabilitySourceOptions
    {
        /// <summary>
        /// Path to the YAML file containing agent capabilities
        /// </summary>
        public string FilePath { get; set; } = "agent_capabilities.yaml";
        
        /// <summary>
        /// Whether to validate the YAML schema on load
        /// </summary>
        public bool ValidateSchema { get; set; } = true;
        
        /// <summary>
        /// Whether to throw an exception if the YAML file is not found
        /// </summary>
        public bool ThrowOnFileNotFound { get; set; } = false;
    }

    /// <summary>
    /// Default implementation of IYamlAgentCapabilitySource that loads from the file system
    /// </summary>
    /// <summary>
    /// Implementation of IYamlAgentCapabilitySource that supports hot reloading of YAML configuration files.
    /// </summary>
    public class YamlAgentCapabilitySource : IYamlAgentCapabilitySource, IDisposable
    {
        private readonly IFileProvider _fileProvider;
        private readonly IOptionsMonitor<YamlAgentCapabilitySourceOptions> _optionsMonitor;
        private readonly ILogger<YamlAgentCapabilitySource> _logger;
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;
        private readonly object _syncLock = new();
        private readonly ConcurrentDictionary<string, IChangeToken> _changeTokens = new();
        private readonly CancellationTokenSource _reloadToken = new();
        
        private AgentCapabilityYaml _cachedCapabilities = new();
        private DateTimeOffset? _lastModified;
        private bool _disposed;
        
        /// <inheritdoc />
        public event Func<object, CapabilitiesReloadedEventArgs, Task>? CapabilitiesReloaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlAgentCapabilitySource"/> class.
        /// </summary>
        public YamlAgentCapabilitySource(
            IFileProvider fileProvider,
            IOptionsMonitor<YamlAgentCapabilitySourceOptions> optionsMonitor,
            ILogger<YamlAgentCapabilitySource> logger)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Configure YAML serialization
            var namingConvention = new CamelCaseNamingConvention();
            _serializer = new SerializerBuilder()
                .WithNamingConvention(namingConvention)
                .Build();
                
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(namingConvention)
                .Build();
                
            // Register for configuration changes
            _optionsMonitor.OnChange(OptionsChanged);
            
            // Initial load of capabilities
            _ = LoadCapabilitiesAsync();
        }

        /// <inheritdoc />
        public bool IsLoaded { get; private set; }
        
        /// <inheritdoc />
        public DateTimeOffset? LastModified => _lastModified;

        /// <inheritdoc />
        public async Task<AgentCapabilityYaml> LoadCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
                
            // If we have cached capabilities and they're still valid, return them
            if (_cachedCapabilities != null && !ShouldReloadCapabilities())
            {
                return _cachedCapabilities;
            }
            
            // Use a lock to prevent multiple concurrent reloads
            using (await AsyncLock.LockAsync(_syncLock, cancellationToken))
            {
                // Double-check if another thread already updated the cache while we were waiting
                if (_cachedCapabilities != null && !ShouldReloadCapabilities())
                {
                    return _cachedCapabilities;
                }
                
                var options = _optionsMonitor.CurrentValue;
                var fileInfo = _fileProvider.GetFileInfo(options.FilePath);
                
                // Check if file exists
                if (!fileInfo.Exists || fileInfo.IsDirectory)
                {
                    var message = $"Agent capabilities file not found: {options.FilePath}";
                    if (options.ThrowOnFileNotFound)
                    {
                        _logger.LogError(message);
                        throw new FileNotFoundException(message, options.FilePath);
                    }
                    
                    _logger.LogWarning(message);
                    return new AgentCapabilityYaml();
                }
                
                try
                {
                    // Read and parse the YAML file
                    using var stream = fileInfo.CreateReadStream();
                    using var reader = new StreamReader(stream);
                    var yamlContent = await reader.ReadToEndAsync(cancellationToken);
                    
                    // Deserialize the YAML content
                    var capabilities = _deserializer.Deserialize<AgentCapabilityYaml>(yamlContent);
                    
                    // Validate the schema if enabled
                    if (options.ValidateSchema)
                    {
                        ValidateCapabilities(capabilities);
                    }
                    
                    // Update state
                    _cachedCapabilities = capabilities;
                    _lastModified = fileInfo.LastModified;
                    IsLoaded = true;
                    
                    // Set up file change token for hot reloading
                    SetupFileChangeWatcher(fileInfo, options);
                    
                    _logger.LogInformation("Successfully loaded agent capabilities from {FilePath}", options.FilePath);
                    return _cachedCapabilities;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading agent capabilities from {FilePath}", options.FilePath);
                    throw new InvalidOperationException($"Failed to load agent capabilities from {options.FilePath}", ex);
                }
            }
        }
        
        private void ValidateCapabilities(AgentCapabilityYaml capabilities)
        {
            if (capabilities == null)
                throw new ArgumentNullException(nameof(capabilities));
                
            // Validate version
            if (string.IsNullOrEmpty(capabilities.Version))
            {
                throw new InvalidOperationException("Agent capabilities must specify a version");
            }
            
            // Validate each agent's capabilities
            foreach (var (agentType, agentConfig) in capabilities.Agents)
            {
                if (agentConfig.Capabilities == null)
                {
                    _logger.LogWarning("Agent {AgentType} has null capabilities collection", agentType);
                    continue;
                }
                
                // Check for duplicate capability names
                var duplicateCapabilities = agentConfig.Capabilities
                    .GroupBy(c => c.Name)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                    
                if (duplicateCapabilities.Any())
                {
                    throw new InvalidOperationException(
                        $"Agent '{agentType}' has duplicate capability definitions: {string.Join(", ", duplicateCapabilities)}");
                }
            }
        }
        
        /// <summary>
        /// Sets up a file change watcher for the specified file.
        /// </summary>
        private void SetupFileChangeWatcher(IFileInfo fileInfo, YamlAgentCapabilitySourceOptions options)
        {
            // Remove any existing change token for this path
            if (_changeTokens.TryRemove(options.FilePath, out var existingToken))
            {
                existingToken.RegisterChangeCallback(OnConfigurationChanged, state: null);
            }
            
            // Create a new change token for the file
            var changeToken = _fileProvider.Watch(options.FilePath);
            if (changeToken.ActiveChangeCallbacks)
            {
                _changeTokens[options.FilePath] = changeToken;
                changeToken.RegisterChangeCallback(OnConfigurationChanged, options.FilePath);
            }
            else
            {
                _logger.LogWarning("Change tokens for {FilePath} do not support active callbacks. Hot reloading will not work for this file.", options.FilePath);
            }
        }
        
        /// <summary>
        /// Called when the configuration file changes.
        /// </summary>
        private void OnConfigurationChanged(object state)
        {
            var filePath = state as string;
            if (string.IsNullOrEmpty(filePath))
                return;
                
            try
            {
                _logger.LogInformation("Detected changes in {FilePath}, reloading capabilities...", filePath);
                
                // Trigger a reload of capabilities
                _ = Task.Run(async () =>
                {
                    var eventArgs = new CapabilitiesReloadedEventArgs
                    {
                        FilePath = filePath,
                        Timestamp = DateTimeOffset.UtcNow,
                        Success = false
                    };
                    
                    try
                    {
                        await LoadCapabilitiesAsync(_reloadToken.Token);
                        
                        // Update event args for success
                        eventArgs.Success = true;
                        
                        // Notify listeners that capabilities have been reloaded
                        var handlers = CapabilitiesReloaded;
                        if (handlers != null)
                        {
                            await handlers.Invoke(this, eventArgs);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reloading capabilities from {FilePath}", filePath);
                        eventArgs.Error = ex;
                        
                        // Notify listeners of the error
                        var errorHandlers = CapabilitiesReloaded;
                        if (errorHandlers != null)
                        {
                            await errorHandlers.Invoke(this, eventArgs);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling configuration change for {FilePath}", filePath);
            }
        }
        
        /// <summary>
        /// Called when the options configuration changes.
        /// </summary>
        private void OptionsChanged(YamlAgentCapabilitySourceOptions options, string? name)
        {
            _logger.LogInformation("Configuration options changed, reloading capabilities...");
            _ = LoadCapabilitiesAsync(_reloadToken.Token);
        }
        
        /// <summary>
        /// Determines if the capabilities should be reloaded.
        /// </summary>
        private bool ShouldReloadCapabilities()
        {
            // Always reload if we don't have cached capabilities
            if (_cachedCapabilities == null)
                return true;
                
            // Check if the file has been modified since we last loaded it
            var options = _optionsMonitor.CurrentValue;
            var fileInfo = _fileProvider.GetFileInfo(options.FilePath);
            
            return fileInfo.Exists && 
                  !fileInfo.IsDirectory && 
                  fileInfo.LastModified > (_lastModified ?? DateTimeOffset.MinValue);
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _reloadToken.Cancel();
                _reloadToken.Dispose();
                _changeTokens.Clear();
            }
        }
        
        /// <summary>
        /// Creates a YAML string representation of the capabilities
        /// </summary>
        public string SerializeCapabilities(AgentCapabilityYaml capabilities)
        {
            if (capabilities == null)
                throw new ArgumentNullException(nameof(capabilities));
                
            return _serializer.Serialize(capabilities);
        }
    }
}
