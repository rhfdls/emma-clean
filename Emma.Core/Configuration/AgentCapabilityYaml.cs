using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Root YAML configuration for agent capabilities
    /// </summary>
    public class AgentCapabilityYaml
    {
        /// <summary>
        /// Schema version for compatibility checking
        /// </summary>
        [Required]
        [YamlMember(Alias = "version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Dictionary of agent type to capability configuration
        /// </summary>
        [YamlMember(Alias = "agents")]
        public Dictionary<string, AgentYamlConfig> Agents { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a specific agent type
    /// </summary>
    public class AgentYamlConfig
    {
        /// <summary>
        /// List of capabilities this agent has
        /// </summary>
        [YamlMember(Alias = "capabilities")]
        public List<AgentCapabilityYamlConfig> Capabilities { get; set; } = new();

        /// <summary>
        /// Rate limiting configuration
        /// </summary>
        [YamlMember(Alias = "rate_limits")]
        public List<RateLimitYamlConfig>? RateLimits { get; set; }

        
        /// <summary>
        /// Contexts where this agent can operate
        /// </summary>
        [YamlMember(Alias = "allowed_contexts")]
        public List<string>? AllowedContexts { get; set; }
        
        /// <summary>
        /// Required claims for this agent to operate
        /// </summary>
        [YamlMember(Alias = "required_claims")]
        public List<string>? RequiredClaims { get; set; }
    }

    /// <summary>
    /// Individual capability configuration
    /// </summary>
    public class AgentCapabilityYamlConfig
    {
        /// <summary>
        /// Name of the capability (e.g., "create:task")
        /// </summary>
        [Required]
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the capability
        /// </summary>
        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        /// <summary>
        /// Whether this capability is enabled
        /// </summary>
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Validation rules for this capability
        /// </summary>
        [YamlMember(Alias = "validation_rules")]
        public Dictionary<string, object>? ValidationRules { get; set; }
    }

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public class RateLimitYamlConfig
    {
        /// <summary>
        /// Rate limit window (e.g., "1m" for 1 minute, "1h" for 1 hour)
        /// </summary>
        [Required]
        [YamlMember(Alias = "window")]
        public string Window { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of requests allowed in the window
        /// </summary>
        [YamlMember(Alias = "max_requests")]
        public int MaxRequests { get; set; }

        /// <summary>
        /// Optional scope for the rate limit (e.g., "per_user", "per_agent")
        /// </summary>
        [YamlMember(Alias = "scope")]
        public string? Scope { get; set; }
    }
}
