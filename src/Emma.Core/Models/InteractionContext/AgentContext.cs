using System;
using System.Collections.Generic;

namespace Emma.Core.Models.InteractionContext
{
    /// <summary>
    /// Represents the state and capabilities of an agent within an interaction.
    /// </summary>
    public class AgentContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for the agent instance.
        /// </summary>
        public string AgentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the type/category of the agent (e.g., "nba", "support", "sales").
        /// </summary>
        public string AgentType { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the display name of the agent.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the current state of the agent.
        /// </summary>
        public string State { get; set; } = "Initializing";
        
        /// <summary>
        /// Gets or sets the timestamp when the agent was last active.
        /// </summary>
        public DateTimeOffset LastActiveAt { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Gets or sets the collection of capabilities this agent supports.
        /// </summary>
        public ICollection<string> Capabilities { get; set; } = new HashSet<string>();
        
        /// <summary>
        /// Gets or sets the ID of the interaction this agent is participating in.
        /// </summary>
        public Guid InteractionId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the organization this agent belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }
        
        /// <summary>
        /// Gets or sets the audit ID for tracking purposes.
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the reason for the last state change.
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata for the agent context.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
