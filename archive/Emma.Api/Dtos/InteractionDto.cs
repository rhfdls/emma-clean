using System;
using System.Collections.Generic;
using Emma.Models.Models;

namespace Emma.Api.Dtos
{
    /// <summary>
    /// Data Transfer Object for creating or updating an Interaction
    /// </summary>
    public class InteractionDto
    {
        /// <summary>Interaction type (Email, Call, Meeting, etc.)</summary>
        public string Type { get; set; } = "other";
        
        /// <summary>Interaction direction (inbound, outbound, system)</summary>
        public string Direction { get; set; } = "inbound";
        
        /// <summary>Current status of the interaction</summary>
        public string Status { get; set; } = "draft";
        
        /// <summary>Priority level (low, normal, high, urgent)</summary>
        public string? Priority { get; set; }
        
        /// <summary>Interaction subject/title</summary>
        public string? Subject { get; set; }
        
        /// <summary>Main content/body of the interaction</summary>
        public string? Content { get; set; }
        
        /// <summary>AI-generated summary of the interaction</summary>
        public string? Summary { get; set; }
        
        /// <summary>Privacy level (public, internal, private, confidential)</summary>
        public string? PrivacyLevel { get; set; }
        
        /// <summary>Confidentiality level (standard, sensitive, highly_sensitive)</summary>
        public string? Confidentiality { get; set; }
        
        /// <summary>Retention policy ID</summary>
        public string? RetentionPolicy { get; set; }
        
        /// <summary>Whether follow-up is required</summary>
        public bool? FollowUpRequired { get; set; }
        
        /// <summary>Channel used for the interaction</summary>
        public string? Channel { get; set; }
        
        /// <summary>Channel-specific metadata</summary>
        public Dictionary<string, object>? ChannelData { get; set; }
        
        /// <summary>List of participants in the interaction</summary>
        public List<ParticipantDto>? Participants { get; set; }
        
        /// <summary>List of related business entities</summary>
        public List<RelatedEntityDto>? RelatedEntities { get; set; }
        
        /// <summary>List of tags for categorization</summary>
        public List<string>? Tags { get; set; }
        
        /// <summary>Custom fields for extensibility</summary>
        public Dictionary<string, object>? CustomFields { get; set; }
        
        /// <summary>External system IDs</summary>
        public Dictionary<string, string>? ExternalIds { get; set; }
        
        /// <summary>Start time for timed interactions</summary>
        public DateTime? StartedAt { get; set; }
        
        /// <summary>End time for timed interactions</summary>
        public DateTime? EndedAt { get; set; }
        
        /// <summary>Scheduled time for future interactions</summary>
        public DateTime? ScheduledFor { get; set; }
        
        /// <summary>Duration in seconds</summary>
        public int? DurationSeconds { get; set; }
        
        /// <summary>Deadline for follow-up</summary>
        public DateTime? FollowUpBy { get; set; }
        
        /// <summary>ID of the agent assigned to this interaction</summary>
        public Guid? AssignedToId { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for interaction participants
    /// </summary>
    public class ParticipantDto
    {
        /// <summary>Contact ID of the participant</summary>
        public Guid ContactId { get; set; }
        
        /// <summary>Role in the interaction (sender, recipient, cc, bcc, etc.)</summary>
        public string Role { get; set; } = "participant";
        
        /// <summary>Display name of the participant</summary>
        public string? Name { get; set; }
        
        /// <summary>Email address of the participant</summary>
        public string? Email { get; set; }
        
        /// <summary>Phone number of the participant</summary>
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for related entities
    /// </summary>
    public class RelatedEntityDto
    {
        /// <summary>Type of the related entity (e.g., property, deal, task)</summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>ID of the related entity</summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>Role of this relationship</summary>
        public string? Role { get; set; }
        
        /// <summary>Name or title of the related entity</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for interaction search criteria
    /// </summary>
    public class InteractionSearchDto
    {
        /// <summary>Filter by contact ID</summary>
        public Guid? ContactId { get; set; }
        
        /// <summary>Filter by assigned agent ID</summary>
        public Guid? AssignedToId { get; set; }
        
        /// <summary>Filter by interaction type</summary>
        public string? Type { get; set; }
        
        /// <summary>Filter by status</summary>
        public string? Status { get; set; }
        
        /// <summary>Filter by privacy level</summary>
        public string? PrivacyLevel { get; set; }
        
        /// <summary>Filter by tag</summary>
        public string? Tag { get; set; }
        
        /// <summary>Filter by creation date range (start)</summary>
        public DateTime? CreatedAfter { get; set; }
        
        /// <summary>Filter by creation date range (end)</summary>
        public DateTime? CreatedBefore { get; set; }
        
        /// <summary>Search text (searches across content, subject, and participant names)</summary>
        public string? SearchText { get; set; }
        
        /// <summary>Page number for pagination (1-based)</summary>
        public int Page { get; set; } = 1;
        
        /// <summary>Page size for pagination</summary>
        public int PageSize { get; set; } = 20;
        
        /// <summary>Sort field</summary>
        public string? SortBy { get; set; }
        
        /// <summary>Sort direction (asc/desc)</summary>
        public string? SortOrder { get; set; } = "desc";
    }
}
