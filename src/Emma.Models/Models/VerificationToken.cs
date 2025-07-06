// SPRINT1: Model for email verification tokens (optional, for auditable store)
using System;
using System.ComponentModel.DataAnnotations;

namespace Emma.Models.Models
{
    public class VerificationToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
