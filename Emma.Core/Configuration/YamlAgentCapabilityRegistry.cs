using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Exceptions;
using Emma.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Options for configuring the agent capability registry
    /// </summary>
    public class AgentCapabilityRegistryOptions
    {
        /// <summary>
        /// Whether to enable YAML-based capabilities
        /// </summary>
        public bool EnableYamlCapabilities { get; set; } = true;
        
        /// <summary>
        /// Whether to throw an exception when a capability check fails
        /// </summary>
        public bool ThrowOnValidationFailure { get; set; } = true;
        
        /// <summary>
        /// Programmatically registered capabilities (as an alternative to YAML)
        /// </summary>
        public Dictionary<string, List<AgentCapabilityYamlConfig>>? ProgrammaticCapabilities { get; set; }
    }

    /// <summary>
    /// Enhanced agent capability registry that supports YAML configuration
    /// </summary>
    public class YamlAgentCapabilityRegistry : IAgentCapabilityRegistry, IDisposable
    {
        private readonly IYamlAgentCapabilitySource _yamlSource;
        private readonly ILogger<YamlAgentCapabilityRegistry> _logger;
        private readonly AgentCapabilityRegistryOptions _options;
        private readonly object _syncLock = new();
        private bool _disposed;
        private AgentCapabilityYaml? _cachedCapabilities;
        
        public YamlAgentCapabilityRegistry(
            IYamlAgentCapabilitySource yamlSource,
            ILogger<YamlAgentCapabilityRegistry> logger,
            IOptions<AgentCapabilityRegistryOptions>? options = null)
        {
            _yamlSource = yamlSource ?? throw new ArgumentNullException(nameof(yamlSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new AgentCapabilityRegistryOptions();
        }

        /// <inheritdoc />
        public async Task RegisterCapabilityAsync(string agentType, AgentCapability capability)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type cannot be null or empty", nameof(agentType));

            if (capability == null)
                throw new ArgumentNullException(nameof(capability));

            // Ensure we have the latest capabilities loaded
            var capabilities = await GetCapabilitiesAsync();
            
            lock (_syncLock)
            {
                // Add or update the capability
                if (!capabilities.Agents.TryGetValue(agentType, out var agentConfig))
                {
                    agentConfig = new AgentYamlConfig();
                    capabilities.Agents[agentType] = agentConfig;
                }
                
                // Convert AgentCapability to YAML config
                var yamlCapability = new AgentCapabilityYamlConfig
                {
                    Name = capability.Name,
                    Description = capability.Description,
                    Enabled = capability.IsEnabled
                    // Additional mapping can be added here
                };
                
                // Add or update the capability
                var existingIndex = agentConfig.Capabilities.FindIndex(c => c.Name == capability.Name);
                if (existingIndex >= 0)
                {
                    agentConfig.Capabilities[existingIndex] = yamlCapability;
                    _logger.LogInformation("Updated capability '{CapabilityName}' for agent '{AgentType}'", 
                        capability.Name, agentType);
                }
                else
                {
                    agentConfig.Capabilities.Add(yamlCapability);
                    _logger.LogInformation("Registered new capability '{CapabilityName}' for agent '{AgentType}'", 
                        capability.Name, agentType);
                }
            }
        }

        /// <inheritdoc />
        public async Task<bool> HasCapabilityAsync(string agentType, string capabilityName, IDictionary<string, object>? context = null)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type is required", nameof(agentType));
                
            if (string.IsNullOrWhiteSpace(capabilityName))
                throw new ArgumentException("Capability name is required", nameof(capabilityName));
                
            var capabilities = await GetCapabilitiesAsync();
            
            // Check programmatic capabilities first
            if (_options.ProgrammaticCapabilities != null &&
                _options.ProgrammaticCapabilities.TryGetValue(agentType, out var programmaticCapabilities))
            {
                var programmaticCapability = programmaticCapabilities.FirstOrDefault(c => 
                    string.Equals(c.Name, capabilityName, StringComparison.OrdinalIgnoreCase));
                    
                if (programmaticCapability != null)
                {
                    return programmaticCapability.Enabled && 
                           ValidateCapability(programmaticCapability, agentType, capabilityName, context);
                }
            }
            
            // Check YAML capabilities if enabled
            if (_options.EnableYamlCapabilities && 
                capabilities.Agents.TryGetValue(agentType, out var agentConfig))
            {
                var capability = agentConfig.Capabilities.FirstOrDefault(c => 
                    string.Equals(c.Name, capabilityName, StringComparison.OrdinalIgnoreCase));
                    
                if (capability != null)
                {
                    return capability.Enabled && 
                           ValidateCapability(capability, agentType, capabilityName, context);
                }
            }
            
            _logger.LogDebug("Capability '{CapabilityName}' not found for agent '{AgentType}'", 
                capabilityName, agentType);
                
            return false;
        }

        /// <inheritdoc />
        public async Task ValidateActionAsync(string agentType, string action, IDictionary<string, object>? context = null)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type is required", nameof(agentType));
                
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action is required", nameof(action));
                
            if (!await HasCapabilityAsync(agentType, action, context))
            {
                var message = $"Agent '{agentType}' is not authorized to perform action '{action}'";
                _logger.LogWarning(message);
                
                if (_options.ThrowOnValidationFailure)
                {
                    throw new UnauthorizedAccessException(message);
                }
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAgentCapabilities(string agentType)
        {
            if (string.IsNullOrWhiteSpace(agentType))
                throw new ArgumentException("Agent type is required", nameof(agentType));
                
            var result = new List<string>();
            var capabilities = await GetCapabilitiesAsync();
            
            // Add programmatic capabilities
            if (_options.ProgrammaticCapabilities != null &&
                _options.ProgrammaticCapabilities.TryGetValue(agentType, out var programmaticCapabilities))
            {
                result.AddRange(programmaticCapabilities
                    .Where(c => c.Enabled)
                    .Select(c => c.Name));
            }
            
            // Add YAML capabilities if enabled
            if (_options.EnableYamlCapabilities && 
                capabilities.Agents.TryGetValue(agentType, out var agentConfig))
            {
                result.AddRange(agentConfig.Capabilities
                    .Where(c => c.Enabled)
                    .Select(c => c.Name));
            }
            
            return result.Distinct(StringComparer.OrdinalIgnoreCase);
        }
        
        private async Task<AgentCapabilityYaml> GetCapabilitiesAsync()
        {
            if (_cachedCapabilities != null)
            {
                return _cachedCapabilities;
            }
            
            try
            {
                _cachedCapabilities = await _yamlSource.LoadCapabilitiesAsync();
                return _cachedCapabilities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading agent capabilities");
                
                // Return empty capabilities if YAML loading fails
                return new AgentCapabilityYaml();
            }
        }
        
        private bool ValidateCapability(
            AgentCapabilityYamlConfig capability,
            string agentType,
            string capabilityName,
            IDictionary<string, object>? context)
        {
            if (!capability.Enabled)
            {
                _logger.LogDebug("Capability '{CapabilityName}' is disabled for agent '{AgentType}'", 
                    capabilityName, agentType);
                return false;
            }
            
            // Apply validation rules if any
            if (capability.ValidationRules != null && capability.ValidationRules.Count > 0)
            {
                // TODO: Implement validation rules based on context
                // This could include checking user roles, resource access, etc.
                _logger.LogDebug("Validation rules not yet implemented for capability '{CapabilityName}'", 
                    capabilityName);
            }
            
            return true;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                if (_yamlSource is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
