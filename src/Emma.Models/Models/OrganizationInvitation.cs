using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models
{
    [Table("OrganizationInvitations")]
    public class OrganizationInvitation : BaseEntity
    {
        [Required]
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Role { get; set; } = "Member";

        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty;

        public DateTime? ExpiresAt { get; set; }

        public Guid? InvitedByUserId { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [NotMapped]
        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

        [NotMapped]
        public bool IsActive => RevokedAt == null && AcceptedAt == null && !IsExpired;
    }
}
