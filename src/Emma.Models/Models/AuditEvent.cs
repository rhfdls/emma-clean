using System;

namespace Emma.Models.Models
{
    /// <summary>
    /// Non-PII audit event to record irreversible actions such as contact erasure.
    /// Keep payload free of any personally identifying information.
    /// </summary>
    public class AuditEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public Guid? ActorUserId { get; set; }
        public string Action { get; set; } = string.Empty; // e.g., ContactErased
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public string? TraceId { get; set; }
        public string? DetailsJson { get; set; } // optional, non-PII JSON payload
    }
}
