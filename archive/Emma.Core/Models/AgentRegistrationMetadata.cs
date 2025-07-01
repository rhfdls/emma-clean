using System;
using System.Collections.Generic;

namespace Emma.Core.Models
{
    /// <summary>
    /// Metadata associated with agent registration.
    /// </summary>
    public class AgentRegistrationMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = string.Empty;
        public IList<string> Capabilities { get; set; } = new List<string>();
        public bool IsFactoryCreated { get; set; } = false;
        public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
        public Guid AuditId { get; set; } = Guid.NewGuid();
        public string Reason { get; set; } = "Agent registered via IAgentRegistry";
    }
}
