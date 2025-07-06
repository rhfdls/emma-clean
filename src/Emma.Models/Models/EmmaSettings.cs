// SPRINT1: Settings/config for CRM integrations (per org, per provider)
using System;
using System.ComponentModel.DataAnnotations;

namespace Emma.Models.Models
{
    public class EmmaSettings
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty; // e.g., "fub", "hubspot"

        [MaxLength(200)]
        public string? ApiKey { get; set; }

        public DateTime? LastSync { get; set; }

        // Add more fields as needed for sync options, tokens, etc.
    }
}
