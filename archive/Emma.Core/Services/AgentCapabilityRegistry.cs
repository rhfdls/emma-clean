using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emma.Core.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAgentCapabilityRegistry"/> that manages agent capabilities and permissions.
    /// </summary>
    public class AgentCapabilityRegistry : IAgentCapabilityRegistry
    {
        private readonly ILogger<AgentCapabilityRegistry> _logger;
        private readonly ConcurrentDictionary<string, AgentCapability> _capabilities;
        private readonly ConcurrentDictionary<string, AgentCapability> _defaultCapabilities;
        private readonly AgentCapabilityRegistryOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentCapabilityRegistry"/> class.
        /// </summary>
        public AgentCapabilityRegistry(
            ILogger<AgentCapabilityRegistry> logger,
            IOptions<AgentCapabilityRegistryOptions>? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AgentCapabilityRegistryOptions();
            _capabilities = new ConcurrentDictionary<string, AgentCapability>(StringComparer.OrdinalIgnoreCase);
            _defaultCapabilities = new ConcurrentDictionary<string, AgentCapability>(
                _options.DefaultCapabilities ?? new Dictionary<string, AgentCapability>(),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public Task RegisterCapabilityAsync(string agentType, AgentCapability capability)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));

            if (capability == null)
                throw new ArgumentNullException(nameof(capability));

            _capabilities[agentType] = capability;
            _logger.LogInformation("Registered capabilities for agent type: {AgentType}", agentType);
            
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<AgentCapability?> GetCapabilityAsync(string agentType)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));

            if (_capabilities.TryGetValue(agentType, out var capability))
                return Task.FromResult<AgentCapability?>(capability);

            return Task.FromResult<AgentCapability?>(null);
        }

        /// <inheritdoc />
        public async Task<bool> IsActionAllowedAsync(string agentType, string action)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action cannot be null or empty", nameof(action));

            var capability = await GetEffectiveCapabilityAsync(agentType);
            return capability.IsActionAllowed(action);
        }

        /// <inheritdoc />
        public async Task<bool> IsToolAllowedAsync(string agentType, string toolName, string action)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action cannot be null or empty", nameof(action));

            var capability = await GetEffectiveCapabilityAsync(agentType);
            return capability.IsToolAllowed(toolName, action);
        }

        /// <inheritdoc />
        public async Task<bool> CanDelegateToAsync(string sourceAgentType, string targetAgentType)
        {
            if (string.IsNullOrWhiteSpace(sourceAgentType))
                throw new ArgumentException("Source agent type cannot be null or empty", nameof(sourceAgentType));
            if (string.IsNullOrWhiteSpace(targetAgentType))
                throw new ArgumentException("Target agent type cannot be null or empty", nameof(targetAgentType));

            // An agent can always delegate to itself
            if (string.Equals(sourceAgentType, targetAgentType, StringComparison.OrdinalIgnoreCase))
                return true;

            var sourceCapability = await GetEffectiveCapabilityAsync(sourceAgentType);
            return sourceCapability.CanDelegateTo(targetAgentType);
        }

        /// <inheritdoc />
        public Task<IReadOnlyDictionary<string, AgentCapability>> GetAllCapabilitiesAsync()
        {
            // Create a read-only snapshot of the current capabilities
            var snapshot = _capabilities.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);
                
            return Task.FromResult<IReadOnlyDictionary<string, AgentCapability>>(snapshot);
        }

        /// <inheritdoc />
        public async Task ValidateActionAsync(string agentType, string action)
        {
            if (!await IsActionAllowedAsync(agentType, action))
            {
                _logger.LogWarning("Agent {AgentType} is not authorized to perform action: {Action}", agentType, action);
                throw new UnauthorizedAccessException($"Agent '{agentType}' is not authorized to perform action: {action}");
            }
        }

        /// <inheritdoc />
        public async Task ValidateToolUsageAsync(string agentType, string toolName, string action)
        {
            if (!await IsToolAllowedAsync(agentType, toolName, action))
            {
                _logger.LogWarning(
                    "Agent {AgentType} is not authorized to use tool {ToolName} with action {Action}", 
                    agentType, toolName, action);
                throw new UnauthorizedAccessException(
                    $"Agent '{agentType}' is not authorized to use tool '{toolName}' with action: {action}");
            }
        }

        /// <inheritdoc />
        public async Task ValidateDelegationAsync(string sourceAgentType, string targetAgentType)
        {
            if (!await CanDelegateToAsync(sourceAgentType, targetAgentType))
            {
                _logger.LogWarning(
                    "Agent {SourceAgentType} is not authorized to delegate to agent {TargetAgentType}", 
                    sourceAgentType, targetAgentType);
                throw new UnauthorizedAccessException(
                    $"Agent '{sourceAgentType}' is not authorized to delegate to agent '{targetAgentType}'");
            }
        }

        /// <inheritdoc />
        public Task<AgentCapability?> GetDefaultCapabilityAsync(string agentType)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));

            _defaultCapabilities.TryGetValue(agentType, out var capability);
            return Task.FromResult(capability);
        }

        /// <inheritdoc />
        public Task SetDefaultCapabilityAsync(string agentType, AgentCapability capability)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));

            _defaultCapabilities[agentType] = capability;
            _logger.LogInformation("Set default capability for agent type: {AgentType}", agentType);
            
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<AgentCapability> GetEffectiveCapabilityAsync(string agentType)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));

            // First, try to get explicit capability
            if (_capabilities.TryGetValue(agentType, out var capability))
                return capability;

            // Then try to get default capability
            if (_defaultCapabilities.TryGetValue(agentType, out capability))
                return capability;

            // If no capability is defined, use the global default if configured
            if (_options.DefaultCapabilityForUnknownAgents != null)
                return _options.DefaultCapabilityForUnknownAgents;

            // Otherwise, return a deny-all capability
            return new AgentCapability(agentType, Array.Empty<string>(), Array.Empty<AgentToolCapability>(), Array.Empty<string>());
        }
    }

    /// <summary>
    /// Options for configuring the <see cref="AgentCapabilityRegistry"/>.
    /// </summary>
    public class AgentCapabilityRegistryOptions
    {
        /// <summary>
        /// Gets or sets the default capabilities for specific agent types.
        /// These are used when no explicit capability is registered for an agent type.
        /// </summary>
        public Dictionary<string, AgentCapability>? DefaultCapabilities { get; set; }

        /// <summary>
        /// Gets or sets the default capability to use for unknown agent types.
        /// If not set, unknown agent types will have a deny-all capability.
        /// </summary>
        public AgentCapability? DefaultCapabilityForUnknownAgents { get; set; }
    }
}
