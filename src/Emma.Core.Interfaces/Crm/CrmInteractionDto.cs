// SPRINT1: Generic DTO for CRM interaction data
using System;

namespace Emma.Core.Interfaces.Crm
{
    public class CrmInteractionDto
    {
        public string Id { get; set; } = string.Empty;
        public string ContactId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Summary { get; set; }
        public string? ExternalSourceId { get; set; }
        // Add more fields as needed
    }
}
