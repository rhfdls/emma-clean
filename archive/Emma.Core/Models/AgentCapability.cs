using System;
using System.Collections.Generic;
using System.Linq;

namespace Emma.Core.Models
{
    /// <summary>
    /// Defines the capabilities and permissions of an agent.
    /// Used to control what actions an agent can perform.
    /// </summary>
    public class AgentCapability
    {
        /// <summary>
        /// Gets or sets the unique name of this capability set.
        /// </summary>
        public string Name { get; set; } = "Default";
        
        /// <summary>
        /// Gets or sets the version of this capability definition.
        /// </summary>
        public string Version { get; set; } = "1.0";
        
        /// <summary>
        /// Gets or sets the maximum number of operations this agent can perform per minute.
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets the maximum number of concurrent operations this agent can perform.
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets whether this agent can access sensitive data.
        /// </summary>
        public bool CanAccessSensitiveData { get; set; }
        
        /// <summary>
        /// Gets or sets the list of allowed actions this agent can perform.
        /// Format: "resource:action" (e.g., "contact:read", "task:create")
        /// </summary>
        public HashSet<string> AllowedActions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Gets or sets the list of allowed tools this agent can use.
        /// Format: "toolName:action" (e.g., "email:send", "calendar:schedule")
        /// </summary>
        public HashSet<string> AllowedTools { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Gets or sets the list of agents this agent can delegate to.
        /// </summary>
        public HashSet<string> AllowedDelegations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Gets or sets the maximum depth of agent delegation chains allowed.
        /// </summary>
        public int MaxDelegationDepth { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets whether this agent requires approval for certain actions.
        /// </summary>
        public bool RequiresApproval { get; set; }
        
        /// <summary>
        /// Gets or sets the list of actions that require explicit approval.
        /// If empty and RequiresApproval is true, all actions require approval.
        /// </summary>
        public HashSet<string> ActionsRequiringApproval { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Gets or sets the list of data scopes this agent can access.
        /// Used for row-level security.
        /// </summary>
        public HashSet<string> DataScopes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Gets or sets the expiration time for cached capabilities.
        /// Null means capabilities don't expire.
        /// </summary>
        public TimeSpan? CacheExpiration { get; set; }
        
        /// <summary>
        /// Gets or sets the time when these capabilities were last updated.
        /// </summary>
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Gets or sets additional metadata about these capabilities.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentCapability"/> class.
        /// </summary>
        public AgentCapability() { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentCapability"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The capability to copy from.</param>
        public AgentCapability(AgentCapability other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            
            Name = other.Name;
            Version = other.Version;
            RateLimitPerMinute = other.RateLimitPerMinute;
            MaxConcurrentOperations = other.MaxConcurrentOperations;
            CanAccessSensitiveData = other.CanAccessSensitiveData;
            AllowedActions = new HashSet<string>(other.AllowedActions, StringComparer.OrdinalIgnoreCase);
            AllowedTools = new HashSet<string>(other.AllowedTools, StringComparer.OrdinalIgnoreCase);
            AllowedDelegations = new HashSet<string>(other.AllowedDelegations, StringComparer.OrdinalIgnoreCase);
            MaxDelegationDepth = other.MaxDelegationDepth;
            RequiresApproval = other.RequiresApproval;
            ActionsRequiringApproval = new HashSet<string>(other.ActionsRequiringApproval, StringComparer.OrdinalIgnoreCase);
            DataScopes = new HashSet<string>(other.DataScopes, StringComparer.OrdinalIgnoreCase);
            CacheExpiration = other.CacheExpiration;
            LastUpdated = other.LastUpdated;
            
            // Deep copy metadata
            foreach (var kvp in other.Metadata)
            {
                Metadata[kvp.Key] = kvp.Value;
            }
        }
        
        /// <summary>
        /// Checks if this agent is allowed to perform the specified action.
        /// </summary>
        /// <param name="action">The action to check, in "resource:action" format.</param>
        /// <returns>True if the action is allowed, false otherwise.</returns>
        public bool CanPerformAction(string action)
        {
            if (string.IsNullOrEmpty(action))
                return false;
                
            // Check if this action is explicitly allowed
            return AllowedActions.Contains(action) || 
                   AllowedActions.Contains($"{action}:*");
        }
        
        /// <summary>
        /// Checks if this agent can use the specified tool.
        /// </summary>
        /// <param name="toolName">The name of the tool.</param>
        /// <param name="action">The action to perform with the tool.</param>
        /// <returns>True if the tool usage is allowed, false otherwise.</returns>
        public bool CanUseTool(string toolName, string action)
        {
            if (string.IsNullOrEmpty(toolName) || string.IsNullOrEmpty(action))
                return false;
                
            var toolAction = $"{toolName}:{action}";
            
            // Check if this tool action is explicitly allowed
            return AllowedTools.Contains(toolAction) || 
                   AllowedTools.Contains($"{toolName}:*");
        }
        
        /// <summary>
        /// Checks if this agent can delegate to the specified agent type.
        /// </summary>
        public bool CanDelegateTo(string agentType)
        {
            if (string.IsNullOrEmpty(agentType))
                return false;
                
            return AllowedDelegations.Contains(agentType) || 
                   AllowedDelegations.Contains("*");
        }
        
        /// <summary>
        /// Checks if the specified action requires approval.
        /// </summary>
        public bool RequiresActionApproval(string action)
        {
            if (!RequiresApproval)
                return false;
                
            // If no specific actions are listed, all actions require approval
            if (ActionsRequiringApproval.Count == 0)
                return true;
                
            // Check if this specific action requires approval
            return ActionsRequiringApproval.Contains(action) || 
                   ActionsRequiringApproval.Contains("*");
        }
        
        /// <summary>
        /// Creates a new capability set with the specified name.
        /// </summary>
        public static AgentCapability Create(string name, string version = "1.0")
        {
            return new AgentCapability
            {
                Name = name,
                Version = version,
                LastUpdated = DateTimeOffset.UtcNow
            };
        }
        
        /// <summary>
        /// Creates a new capability set with the specified allowed actions.
        /// </summary>
        public static AgentCapability WithActions(string name, params string[] actions)
        {
            var capability = Create(name);
            foreach (var action in actions)
            {
                capability.AllowedActions.Add(action);
            }
            return capability;
        }
        
        /// <summary>
        /// Creates a new capability set with the specified allowed tools.
        /// </summary>
        public static AgentCapability WithTools(string name, params string[] tools)
        {
            var capability = Create(name);
            foreach (var tool in tools)
            {
                capability.AllowedTools.Add(tool);
            }
            return capability;
        }
    }
}
