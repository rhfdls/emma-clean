using Emma.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Emma.Core.Services
{
    /// <summary>
    /// Thread-safe implementation of IAgentRegistry following Microsoft DI best practices.
    /// Uses ConcurrentDictionary for thread safety without locking.
    /// Supports both first-class and factory-created agents.
    /// </summary>
    public class AgentRegistry : IAgentRegistry
    {
        private readonly ILogger<AgentRegistry> _logger;
        private readonly ConcurrentDictionary<string, RegisteredAgent> _agents;

        public AgentRegistry(ILogger<AgentRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agents = new ConcurrentDictionary<string, RegisteredAgent>();
        }

        /// <inheritdoc />
        public Task RegisterAgentAsync<T>(string agentType, T agent, AgentRegistrationMetadata? metadata = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));

            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            metadata ??= new AgentRegistrationMetadata
            {
                Name = agentType,
                Reason = $"Agent {agentType} registered programmatically"
            };

            var registeredAgent = new RegisteredAgent
            {
                AgentType = agentType,
                Instance = agent,
                Metadata = metadata,
                InterfaceType = typeof(T)
            };

            var wasAdded = _agents.TryAdd(agentType, registeredAgent);
            if (!wasAdded)
            {
                // Update existing registration
                _agents.TryUpdate(agentType, registeredAgent, _agents[agentType]);
                _logger.LogInformation("Updated agent registration: {AgentType} (AuditId: {AuditId})", 
                    agentType, metadata.AuditId);
            }
            else
            {
                _logger.LogInformation("Registered new agent: {AgentType} (AuditId: {AuditId})", 
                    agentType, metadata.AuditId);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<T?> GetAgentAsync<T>(string agentType) where T : class
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Task.FromResult<T?>(null);

            if (_agents.TryGetValue(agentType, out var registeredAgent))
            {
                if (registeredAgent.Instance is T typedAgent)
                {
                    _logger.LogDebug("Retrieved agent: {AgentType} as {InterfaceType}", 
                        agentType, typeof(T).Name);
                    return Task.FromResult<T?>(typedAgent);
                }
                else
                {
                    _logger.LogWarning("Agent {AgentType} found but cannot be cast to {RequestedType}. " +
                        "Registered as {ActualType}", 
                        agentType, typeof(T).Name, registeredAgent.InterfaceType.Name);
                }
            }

            _logger.LogDebug("Agent not found: {AgentType}", agentType);
            return Task.FromResult<T?>(null);
        }

        /// <inheritdoc />
        public Task<bool> IsAgentRegisteredAsync(string agentType)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Task.FromResult(false);

            var isRegistered = _agents.ContainsKey(agentType);
            return Task.FromResult(isRegistered);
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetRegisteredAgentTypesAsync()
        {
            var agentTypes = _agents.Keys.ToList().AsEnumerable();
            _logger.LogDebug("Retrieved {Count} registered agent types", agentTypes.Count());
            return Task.FromResult(agentTypes);
        }

        /// <inheritdoc />
        public Task<AgentRegistrationMetadata?> GetAgentMetadataAsync(string agentType)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Task.FromResult<AgentRegistrationMetadata?>(null);

            if (_agents.TryGetValue(agentType, out var registeredAgent))
            {
                return Task.FromResult<AgentRegistrationMetadata?>(registeredAgent.Metadata);
            }

            return Task.FromResult<AgentRegistrationMetadata?>(null);
        }

        /// <inheritdoc />
        public Task<bool> UnregisterAgentAsync(string agentType)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                return Task.FromResult(false);

            var wasRemoved = _agents.TryRemove(agentType, out var removedAgent);
            if (wasRemoved && removedAgent != null)
            {
                _logger.LogInformation("Unregistered agent: {AgentType} (AuditId: {AuditId})", 
                    agentType, removedAgent.Metadata.AuditId);

                // Dispose agent if it implements IDisposable
                if (removedAgent.Instance is IDisposable disposableAgent)
                {
                    try
                    {
                        disposableAgent.Dispose();
                        _logger.LogDebug("Disposed agent instance: {AgentType}", agentType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing agent instance: {AgentType}", agentType);
                    }
                }
            }

            return Task.FromResult(wasRemoved);
        }

        /// <inheritdoc />
        public Task<IDictionary<string, AgentHealthStatus>> GetAgentHealthStatusAsync()
        {
            var healthStatuses = new Dictionary<string, AgentHealthStatus>();

            foreach (var kvp in _agents)
            {
                var agentType = kvp.Key;
                var registeredAgent = kvp.Value;

                try
                {
                    // Check if agent implements IAgentLifecycle for health checks
                    if (registeredAgent.Instance is IAgentLifecycle lifecycleAgent)
                    {
                        // For now, assume healthy if lifecycle is implemented
                        // TODO: Implement actual health check when IAgentLifecycle is created
                        healthStatuses[agentType] = AgentHealthStatus.Healthy;
                    }
                    else
                    {
                        // Basic health check - ensure instance is not null
                        healthStatuses[agentType] = registeredAgent.Instance != null 
                            ? AgentHealthStatus.Healthy 
                            : AgentHealthStatus.Unhealthy;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking health for agent: {AgentType}", agentType);
                    healthStatuses[agentType] = AgentHealthStatus.Unhealthy;
                }
            }

            _logger.LogDebug("Health check completed for {Count} agents", healthStatuses.Count);
            return Task.FromResult<IDictionary<string, AgentHealthStatus>>(healthStatuses);
        }

        /// <summary>
        /// Internal representation of a registered agent.
        /// </summary>
        private class RegisteredAgent
        {
            public string AgentType { get; set; } = string.Empty;
            public object Instance { get; set; } = null!;
            public AgentRegistrationMetadata Metadata { get; set; } = null!;
            public Type InterfaceType { get; set; } = null!;
        }
    }
}
