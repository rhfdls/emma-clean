using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization.NamingConventions;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Validates YAML capability configuration files against the expected schema.
    /// </summary>
    public class YamlCapabilityValidator
    {
        private readonly ILogger<YamlCapabilityValidator> _logger;
        private static readonly Regex VersionRegex = new("^\\d+\\.\\d+$", RegexOptions.Compiled);

        public YamlCapabilityValidator(ILogger<YamlCapabilityValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates the root YAML configuration object.
        /// </summary>
        public bool Validate(AgentCapabilityYaml config, string filePath = "")
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var errors = new List<string>();
            
            // Validate version
            if (string.IsNullOrEmpty(config.Version))
            {
                errors.Add("Version is required");
            }
            else if (!VersionRegex.IsMatch(config.Version))
            {
                errors.Add("Version must be in format 'major.minor'");
            }

            // Validate agents
            if (config.Agents == null || !config.Agents.Any())
            {
                errors.Add("At least one agent configuration is required");
            }
            else
            {
                foreach (var agent in config.Agents)
                {
                    ValidateAgent(agent.Key, agent.Value, errors);
                }
            }

            // Log and return results
            if (errors.Any())
            {
                var errorMessage = $"Invalid YAML configuration in {filePath}:\n{string.Join("\n", errors)}";
                _logger.LogError(errorMessage);
                throw new ValidationException(errorMessage);
            }

            return true;
        }

        private void ValidateAgent(string agentName, AgentCapabilityYaml.AgentConfig agent, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(agentName))
            {
                errors.Add("Agent name cannot be empty");
                return;
            }

            if (agent.Capabilities == null || !agent.Capabilities.Any())
            {
                errors.Add($"Agent '{agentName}' must have at least one capability");
                return;
            }

            // Validate each capability
            for (var i = 0; i < agent.Capabilities.Count; i++)
            {
                var capability = agent.Capabilities[i];
                var capabilityPath = $"{agentName}.capabilities[{i}]";
                
                if (string.IsNullOrWhiteSpace(capability.Name))
                {
                    errors.Add($"{capabilityPath}.name: Capability name is required");
                }
                else if (!Regex.IsMatch(capability.Name, "^[a-z0-9:-]+$"))
                {
                    errors.Add($"{capabilityPath}.name: Must contain only lowercase letters, numbers, colons, or hyphens");
                }

                if (string.IsNullOrWhiteSpace(capability.Description))
                {
                    errors.Add($"{capabilityPath}.description: Description is required");
                }

                // Validate rate limits if present
                if (agent.RateLimits != null)
                {
                    foreach (var limit in agent.RateLimits)
                    {
                        if (limit.MaxRequests <= 0)
                        {
                            errors.Add($"{agentName}.rate_limits: max_requests must be greater than 0");
                        }

                        if (string.IsNullOrEmpty(limit.Window) || 
                            !(limit.Window.EndsWith('s') || limit.Window.EndsWith('m') || limit.Window.EndsWith('h')))
                        {
                            errors.Add($"{agentName}.rate_limits: window must end with 's' (seconds), 'm' (minutes), or 'h' (hours)");
                        }
                    }
                }
            }
        }
    }
}
