using System;
using System.ComponentModel.DataAnnotations;

namespace Emma.Api.Dtos
{
    public class ContactCreateDto
    {
        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [MaxLength(100)]
        public string? PreferredName { get; set; }

        [MaxLength(50)]
        public string? Title { get; set; }

        [MaxLength(200)]
        public string? JobTitle { get; set; }

        [MaxLength(200)]
        public string? Company { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Source { get; set; }

        public Guid? OwnerId { get; set; }

        [MaxLength(50)]
        public string? PreferredContactMethod { get; set; }

        [MaxLength(50)]
        public string? PreferredContactTime { get; set; }

        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }
    }
}
