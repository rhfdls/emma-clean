using System;
using System.ComponentModel.DataAnnotations;
using Emma.Models.Models;

namespace Emma.Api.Dtos
{
    // SPRINT3-API: All fields optional for partial updates via PUT
    public class ContactUpdateDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

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

        // Provider-related optional fields
        public RelationshipState? RelationshipState { get; set; }
        public string? CompanyName { get; set; }
        public string? LicenseNumber { get; set; }
        public bool? IsPreferred { get; set; }
        public string? Website { get; set; }
    }
}
