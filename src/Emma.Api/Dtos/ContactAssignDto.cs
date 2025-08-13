using System;
using System.ComponentModel.DataAnnotations;

namespace Emma.Api.Dtos
{
    public class ContactAssignDto
    {
        [Required]
        public Guid ContactId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid AssignedByAgentId { get; set; }

        public string? SourceContext { get; set; }
        public string? TraceId { get; set; }
    }
}
