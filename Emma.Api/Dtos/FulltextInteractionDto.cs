using System;
using System.Collections.Generic;

namespace Emma.Api.Dtos
{
    public class FulltextInteractionDto
    {
        public Guid AgentId { get; set; }
        public Guid ContactId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string Type { get; set; } = string.Empty; // transcript, email, sms, etc.
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
