using System;
using System.ComponentModel.DataAnnotations;
using Emma.Models.Models;

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

        // Preferred plan identifier (e.g., Stripe price/product id)
        public string? PlanId { get; set; }

        [Obsolete("Use PlanId. This will be removed in a future release.")]
        public PlanType? PlanType { get; set; }
        public int? SeatCount { get; set; }
    }
}
