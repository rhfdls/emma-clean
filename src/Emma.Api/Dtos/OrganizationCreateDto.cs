using System;
using System.ComponentModel.DataAnnotations;
using Emma.Models.Enums;

namespace Emma.Api.Dtos
{
    // SPRINT2: Organization create DTO aligned to model requirements
    public class OrganizationCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public Guid OwnerUserId { get; set; }

        public PlanType? PlanType { get; set; }
        public int? SeatCount { get; set; }
    }
}
