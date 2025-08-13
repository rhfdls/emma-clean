using System;

namespace Emma.Api.Dtos
{
    public class ContactReadDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? PreferredName { get; set; }
        public string? Title { get; set; }
        public string? JobTitle { get; set; }
        public string? Company { get; set; }
        public string? Department { get; set; }
        public string? Source { get; set; }
        public Guid? OwnerId { get; set; }
        public string? PreferredContactMethod { get; set; }
        public string? PreferredContactTime { get; set; }
        public string? Notes { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime? LastContactedAt { get; set; }
        public DateTime? NextFollowUpAt { get; set; }
        // Add other relevant fields as needed
    }
}
