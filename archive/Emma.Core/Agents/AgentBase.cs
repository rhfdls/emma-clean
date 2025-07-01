using System.Collections.Concurrent;
using Emma.Models.Enums;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emma.Core.Agents;

/// <summary>
/// Base class for all agents in the EMMA platform.
/// Provides common functionality and default implementations.
/// </summary>
public abstract class AgentBase : IAgent, IDisposable
{
    private readonly ILogger _logger;
    private bool _isInitialized;
    private bool _isDisposed;
    private readonly ConcurrentDictionary<string, object> _configuration = new(StringComparer.OrdinalIgnoreCase);

    protected AgentBase(
        string agentType,
        string displayName,
        string description,
        string version,
        ILogger logger,
        IOptions<AgentOptions>? options = null)
    {
        if (string.IsNullOrWhiteSpace(agentType))
            throw new ArgumentException("Agent type cannot be null or whitespace.", nameof(agentType));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be null or whitespace.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or whitespace.", nameof(version));

        AgentId = $"{agentType}-{Guid.NewGuid():N}";
        AgentType = agentType;
        DisplayName = displayName;
        Description = description;
        Version = version;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Capability = new AgentCapability
        {
            AgentId = AgentId,
            AgentName = displayName,
            Description = description,
            Version = version,
            SupportedTasks = new List<string>(),
            RequiredPermissions = new List<string>(),
            Configuration = new Dictionary<string, object>()
        };
    }

    public string AgentId { get; }
    public string AgentType { get; }
    public string DisplayName { get; protected set; }
    public string Description { get; protected set; }
    public string Version { get; protected set; }
    public AgentCapability Capability { get; protected set; }

    protected ILogger Logger => _logger;
    protected bool IsInitialized => _isInitialized;
    protected bool IsDisposed => _isDisposed;

    public virtual async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, AgentContext context)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (!_isInitialized)
            throw new InvalidOperationException($"Agent {GetType().Name} has not been initialized. Call InitializeAsync() first.");

        _logger.LogInformation("[{TraceId}] Processing request for agent {AgentId} ({AgentType})", context.TraceId, AgentId, GetType().Name);

        try
        {
            await ValidateRequestAsync(request, context);

            return new AgentResponse
            {
                Success = false,
                Message = $"Agent {GetType().Name} does not implement ProcessRequestAsync.",
                StatusCode = 501,
                AgentType = AgentType,
                TraceId = context.TraceId,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "[{TraceId}] Unauthorized access in agent {AgentId}: {Message}", context.TraceId, AgentId, ex.Message);

            return new AgentResponse
            {
                Success = false,
                Message = "Access denied: " + ex.Message,
                StatusCode = 403,
                AgentType = AgentType,
                TraceId = context.TraceId,
                Timestamp = DateTime.UtcNow,
                ErrorDetails = ex.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] Error processing request for agent {AgentId}: {Message}", context.TraceId, AgentId, ex.Message);

            return new AgentResponse
            {
                Success = false,
                Message = $"Error processing request: {ex.Message}",
                StatusCode = 500,
                AgentType = AgentType,
                TraceId = context.TraceId,
                Timestamp = DateTime.UtcNow,
                ErrorDetails = ex.ToString()
            };
        }
    }

    public virtual async Task ValidateRequestAsync(AgentRequest request, AgentContext context)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (!_isInitialized)
            throw new InvalidOperationException($"Agent {GetType().Name} has not been initialized.");

        await Task.CompletedTask;
    }

    public virtual async Task<AgentHealthStatus> GetHealthStatusAsync()
    {
        var status = new AgentHealthStatus
        {
            AgentId = AgentId,
            AgentType = AgentType,
            Status = _isInitialized ? "Healthy" : "Not Initialized",
            Timestamp = DateTime.UtcNow,
            Metrics = new Dictionary<string, object>
            {
                { "IsInitialized", _isInitialized },
                { "IsDisposed", _isDisposed },
                { "ConfigurationCount", _configuration.Count }
            },
            Details = _isInitialized
                ? "Agent is running and healthy"
                : "Agent has not been initialized"
        };

        if (Capability != null)
        {
            status.Metrics["CapabilityVersion"] = Capability.Version;
            status.Metrics["SupportedTasks"] = Capability.SupportedTasks?.Count ?? 0;
            status.Metrics["RequiredPermissions"] = Capability.RequiredPermissions?.Count ?? 0;
        }

        return status;
    }

    public virtual async Task InitializeAsync()
    {
        if (_isInitialized)
            return;
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name, "Cannot initialize a disposed agent");

        _logger.LogInformation("Initializing agent {AgentId} ({AgentType})", AgentId, GetType().Name);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (_configuration.Count == 0 && this is IConfigurableAgent configurableAgent)
            {
                var config = await configurableAgent.LoadConfigurationAsync();
                if (config != null)
                {
                    foreach (var kvp in config)
                    {
                        _configuration[kvp.Key] = kvp.Value;
                    }
                }
            }

            await OnInitializeAsync();
            _isInitialized = true;
            stopwatch.Stop();

            _logger.LogInformation("Agent {AgentId} initialized successfully in {ElapsedMs}ms", AgentId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to initialize agent {AgentId} after {ElapsedMs}ms: {ErrorMessage}", AgentId, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    public virtual async Task ShutdownAsync()
    {
        if (!_isInitialized || _isDisposed)
            return;

        _logger.LogInformation("Shutting down agent {AgentId} ({AgentType})", AgentId, GetType().Name);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (this is IConfigurableAgent configurableAgent && _configuration.Count > 0)
            {
                await configurableAgent.SaveConfigurationAsync(_configuration);
            }

            await OnShutdownAsync();
            _isInitialized = false;
            stopwatch.Stop();

            _logger.LogInformation("Agent {AgentId} shut down successfully in {ElapsedMs}ms", AgentId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error shutting down agent {AgentId} after {ElapsedMs}ms: {ErrorMessage}", AgentId, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
        finally
        {
            _isDisposed = true;
        }
    }

    protected virtual Task OnInitializeAsync() => Task.CompletedTask;
    protected virtual Task OnShutdownAsync() => Task.CompletedTask;
    protected virtual Task OnDisposeAsync() => Task.CompletedTask;

    protected T GetConfigValue<T>(string key, T defaultValue = default)
    {
        if (_configuration.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    protected void SetConfigValue<T>(string key, T value)
    {
        _configuration[key] = value!;
    }

    public IReadOnlyDictionary<string, object> GetConfiguration()
    {
        // Return a read-only copy of the configuration dictionary
        return new ReadOnlyDictionary<string, object>(_configuration);
    }

    public async Task UpdateConfigurationAsync(IReadOnlyDictionary<string, object> configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        foreach (var kvp in configuration)
        {
            _configuration[kvp.Key] = kvp.Value;
        }
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_isDisposed)
        {
            try
            {
                if (_isInitialized)
                {
                    await ShutdownAsync().ConfigureAwait(false);
                }

                _configuration.Clear();
                await OnDisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing agent {AgentId}: {ErrorMessage}", AgentId, ex.Message);
                throw;
            }
            finally
            {
                _isDisposed = true;
            }
        }
    }
}