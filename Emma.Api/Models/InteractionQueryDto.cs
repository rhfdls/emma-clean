using System;

namespace Emma.Api.Models
{
    /// <summary>
    /// DTO for querying agent interactions in CosmosDB and PostgreSQL.
    /// </summary>
    public class InteractionQueryDto
    {
        /// <summary>Contact/lead ID (optional)</summary>
        public Guid? LeadId { get; set; }
        /// <summary>Agent ID (optional)</summary>
        public Guid? AgentId { get; set; }
        /// <summary>Start of date range (optional)</summary>
        public DateTime? Start { get; set; }
        /// <summary>End of date range (optional)</summary>
        public DateTime? End { get; set; }
    }
}
