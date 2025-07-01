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
    

    /// <summary>
    /// Implementation of IYamlAgentCapabilitySource with hot reloading support.
    /// </summary>
    public class YamlHotReloadCapabilitySource : IYamlAgentCapabilitySource, IDisposable
    {
        private readonly IFileProvider _fileProvider;
        private readonly IOptionsMonitor<YamlAgentCapabilitySourceOptions> _optionsMonitor;
        private readonly ILogger<YamlHotReloadCapabilitySource> _logger;
        private readonly YamlCapabilityValidator _validator;
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;
        private readonly YamlFileCache _fileCache;
        private readonly object _syncLock = new();
        private readonly ConcurrentDictionary<string, IChangeToken> _changeTokens = new();
        private readonly CancellationTokenSource _reloadToken = new();
        private readonly IYamlCapabilityMetrics _metrics;
        
        private AgentCapabilityYaml _cachedCapabilities = new();
        private DateTimeOffset? _lastModified;
        private bool _disposed;
        
        /// <inheritdoc />
        public event Func<object, CapabilitiesReloadedEventArgs, Task>? CapabilitiesReloaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlHotReloadCapabilitySource"/> class.
        /// </summary>
        public YamlHotReloadCapabilitySource(
            IFileProvider fileProvider,
            IOptionsMonitor<YamlAgentCapabilitySourceOptions> optionsMonitor,
            ILogger<YamlHotReloadCapabilitySource> logger,
            YamlCapabilityValidator? validator = null,
            TimeSpan? maxCacheDuration = null,
            IYamlCapabilityMetrics? metrics = null)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator ?? new YamlCapabilityValidator(
                LoggerFactory.Create(builder => builder.AddConsole())
                    .CreateLogger<YamlCapabilityValidator>());
            
            // Configure YAML serialization
            var namingConvention = new CamelCaseNamingConvention();
            _serializer = new SerializerBuilder()
                .WithNamingConvention(namingConvention)
                .Build();
                
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(namingConvention)
                .Build();

            // Initialize file cache
            var options = _optionsMonitor.CurrentValue;
            _fileCache = new YamlFileCache(
                _fileProvider,
                options.FilePath,
                logger,
                maxCacheDuration);
                
            // Initialize metrics with null object pattern if not provided
            _metrics = metrics ?? new NullYamlCapabilityMetrics();
                
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
                    // Read and parse the YAML file using cache
                    var (yaml, wasCached) = await _fileCache.GetContentAsync(cancellationToken);
                    
                    if (yaml == null)
                    {
                        _logger.LogWarning("Configuration file not found or empty: {FilePath}", options.FilePath);
                        return null;
                    }
                    
                    if (wasCached && _cachedCapabilities != null)
                    {
                        _logger.LogDebug("Using cached capabilities from {FilePath}", options.FilePath);
                        return _cachedCapabilities;
                    }
                    
                    _logger.LogInformation("Loading capabilities from {FilePath}", options.FilePath);
                    
                    // Start timing the load operation
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    
                    // Deserialize the YAML content
                    var capabilities = _deserializer.Deserialize<AgentCapabilityYaml>(yaml);
                    
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
                    
                    // Record metrics for successful load
                    var agentCount = _cachedCapabilities?.Agents?.Count ?? 0;
                    var capabilityCount = _cachedCapabilities?.Agents?.Sum(a => a.Value?.Capabilities?.Count ?? 0) ?? 0;
                    _metrics.RecordHotReload(
                        options.FilePath, 
                        stopwatch.Elapsed, 
                        true, 
                        agentCount, 
                        capabilityCount);
                    
                    _logger.LogInformation("Successfully loaded {AgentCount} agents with {CapabilityCount} capabilities from {FilePath}", 
                        agentCount, capabilityCount, options.FilePath);
                    return _cachedCapabilities;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading agent capabilities from {FilePath}", options.FilePath);
                    throw new InvalidOperationException($"Failed to load agent capabilities from {options.FilePath}", ex);
                }
            }
        }
        
        /// <summary>
        /// Validates the deserialized capabilities against the schema.
        /// </summary>
        private void ValidateCapabilities(AgentCapabilityYaml capabilities)
        {
            if (capabilities == null)
                throw new InvalidDataException("Capabilities cannot be null");
                
            // Add any additional validation logic here
            if (capabilities.Agents == null)
            {
                capabilities.Agents = new Dictionary<string, AgentCapability>();
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
                existingToken.RegisterChangeCallback(_ => { }, null);
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
